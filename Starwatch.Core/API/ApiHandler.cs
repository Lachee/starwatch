using Newtonsoft.Json;
using Starwatch.API.Gateway;
using Starwatch.API.Rest;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Rest.Util;
using Starwatch.API.Web;
using Starwatch.Logging;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using Starwatch.API.Gateway.Event;

namespace Starwatch.API
{
    public class ApiHandler
    {
        private const string BOT_ACCOUNT_FILE = "Starwatch-Bots.json";
        private const int DEFAULT_PORT = 8000;

        public const string GATEWAY_EVENT_SERVICE = "/events";
        public const string GATEWAY_LOG_SERVICE = "/log";

        public Logger Logger { get; }
        public Configuration Configuration { get; }
        public Starwatch.StarboundHandler Starwatch { get; }

        private List<IRequestHandler> requestHandlers = new List<IRequestHandler>();
        public BlocklistHandler BlocklistHandler { get; }
        public AuthHandler AuthHandler { get; }
        public RestHandler RestHandler { get; }

        public HttpServer HttpServer { get; private set; }

        private Timer _authenticationTimer;
        private Dictionary<string, Authentication> _authentications = new Dictionary<string, Authentication>();

        private string CertificateFilename => Configuration.GetString("cert_file", "cert.pfx");
        private string CertificatePassword => Configuration.GetString("cert_pass", "pass123");
        public bool IsSecure { get => Configuration.GetBool("secured", false); set => Configuration.SetKey("secured", value); }
        public int Port => Configuration.GetInt("port", DEFAULT_PORT);


        public ApiHandler(StarboundHandler starboundHandler, Configuration configuration)
        {
            //Setup basic logging and stuff
            this.Starwatch = starboundHandler;
            this.Logger = new Logger("API", starboundHandler.Logger);
            this.Configuration = configuration;

            //We create the blocklist handler. This is not added to the list as it is ALWAYS the first element
            this.BlocklistHandler = new BlocklistHandler();
            this.BlocklistHandler.BlockAddresses = configuration.GetObject("blocklist", new HashSet<string>());

            //Create the handlers
            //Register the auth handler
            if (configuration.GetBool("enable_rest", true))
            {
                requestHandlers.Add(this.RestHandler = new Rest.RestHandler(this)
                {
                    MinimumAuthentication = configuration.GetObject("rest_minimum_auth", AuthLevel.Admin)
                });
                RestHandler.RegisterRoutes(Assembly.GetAssembly(typeof(Rest.Route.MetaEndpointsRoute)));
            }

            //Register the auth handler
            if (configuration.GetBool("enable_auth", true))
                requestHandlers.Add(this.AuthHandler = new Web.AuthHandler(this));

            //Register the log handler
            if (configuration.GetBool("enable_log", true))
                requestHandlers.Add(new LogHandler(this));

            //Register the web handler
            if (configuration.GetBool("enable_web", true))
                requestHandlers.Add(new WebHandler(this) { ContentRoot = Configuration.GetString("web_root", "Content/") });
            

            //Setup the timer
            _authenticationTimer = new Timer(60 * 1000) { AutoReset = true };
            _authenticationTimer.Elapsed += (sender, args) =>
            {
                //Clear the pending auths
                AuthHandler.ClearPendingTokens();

                //Clear all authentications that have time up
                DateTime mintime = DateTime.UtcNow - TimeSpan.FromDays(7);
                var removals = _authentications.Where(kp => kp.Value.LastActionTime < mintime).Select(kp => kp.Key).ToList();
                foreach(var r in removals) _authentications.Remove(r);
            };
            _authenticationTimer.Start();
        }

        private void HandleRequest(RequestMethod method, HttpRequestEventArgs args)
        {
            //Prepare the authentication
            var identity = args.User.Identity;
            var authentication = CreateAuthentication(identity);

            //Make sure the auth is valid
            if (authentication == null)
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.Close();
                return;
            }

            //First make sure we are allowed
            if (BlocklistHandler != null && BlocklistHandler.HandleRequest(method, args, authentication))
                return;

            //Double check teh rate limits. Every hit counts as one. Additional hits maybe include.
            authentication.RecordAction($"http:{method}");
            authentication.IncrementRateLimit();

            double limit = authentication.CheckRateLimit();
            
            if (limit >= 3)         //If we are 150% we will add them to the blocklist
            {
                //Block connection
                Logger.LogError("Client " + authentication + " has exceeded their rate limit and is being blocked!");
                BlockRequests(args.Request.UserHostAddress);
                BlockRequests(authentication.Name, true);

                //Respond with the message saying they are blocked
                args.Response.WriteRest(new RestResponse(RestStatus.Forbidden, msg: "You have been banned for too many requests.", res: new RateLimitResponse()
                {
                    Limit = 0,
                    Remaining = 0,
                    RetryAfter = null
                }));
                return;
            }
            else if (limit >= 1)    //If we are 100% over, reject them saying they are over.
            {
                Logger.LogWarning("Client " + authentication + " has exceeded their rate limit and is being ignored.");
                
                //426 is "Too Many Requests" 
                //https://softwareengineering.stackexchange.com/questions/128512/suggested-http-rest-status-code-for-request-limit-reached
                args.Response.AddHeader("Retry-After", authentication.RateLimitResetTime.ToString("r"));
                args.Response.AddHeader("X-RateLimit-Reset", authentication.RateLimitResetTime.ToString("r"));
                args.Response.AddHeader("X-RateLimit-Limit", authentication.GetRateLimit().ToString());
                args.Response.AddHeader("X-RateLimit-Remaining", authentication.GetRateLimitRemaining().ToString());
                args.Response.WriteRest(new RestResponse(RestStatus.TooManyRequests, msg: "You are being ratelimited.", res: new RateLimitResponse()
                {
                    Limit = authentication.GetRateLimit(),
                    Remaining = authentication.GetRateLimitRemaining(),
                    RetryAfter = authentication.RateLimitResetTime
                }));
                return;
            }

            //Lets just always add these headers anyways cause why not.
            args.Response.AddHeader("X-RateLimit-Reset", authentication.RateLimitResetTime.ToString("r"));
            args.Response.AddHeader("X-RateLimit-Limit", authentication.GetRateLimit().ToString());
            args.Response.AddHeader("X-RateLimit-Remaining", authentication.GetRateLimitRemaining().ToString());

            //Iterate over every handler and abort when we get the first valid one.
            for (int i = 0; i < requestHandlers.Count; i++)
                if (requestHandlers[i].HandleRequest(method, args, authentication))
                    return;

            //We didnt find anything so return a 404
            args.Response.StatusCode = (int)HttpStatusCode.NotFound;
            args.Response.Close();
        }

        #region start / stop
        public Task Start()
        {
            if (IsSecure)
            {
                //First load and validate the certificate
                Logger.Log("Setting up certificate for secure connection....");
                var certfactory = new CertificateFactory(CertificateFilename, CertificatePassword, Logger);
                var certificate = certfactory.Load();

                if (certificate == null)
                {
                    //Its invalid to remove the secure status and let it fall through to the next if statement
                    Logger.LogError("Failed to establish a valid certificate for the secure connection! Reverting the setting...");
                    IsSecure = false;
                }
                else
                {
                    //Its valid, so lets configure the server
                    HttpServer = new HttpServer(Port, true);
                    HttpServer.SslConfiguration.ServerCertificate = certificate;
                    HttpServer.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    HttpServer.SslConfiguration.CheckCertificateRevocation = false;
                }
            }

            //If we are not secure then setup the server normally.
            // We are doing it as a seperate if statement because the secure check may fail.
            if (!IsSecure)
            {
                Logger.LogWarning("The connection is not secured. Authentication functionality will be disabled!");
                HttpServer = new HttpServer(Port, false);
            }

            //Establish the common settings
            this.HttpServer.Log.Level = WebSocketSharp.LogLevel.Warn;
            this.HttpServer.Log.Output = (data, file) => {
                Logger.Log("[HTTP] " + $"({data.Level}) {data.Message}");
            };

            //Authentication handling
            this.HttpServer.AuthenticationSchemes = WebSocketSharp.Net.AuthenticationSchemes.Basic;
            this.HttpServer.UserCredentialsFinder += (identity) =>
            {
                var auth = CreateAuthentication(identity);
                return auth != null ? auth.GetNetworkCredentials(identity) : null;
            };

            //Events
            this.HttpServer.OnGet += (s, a) => HandleRequest(RequestMethod.Get, a);
            this.HttpServer.OnDelete += (s, a) => HandleRequest(RequestMethod.Delete, a);
            this.HttpServer.OnPost += (s, a) => HandleRequest(RequestMethod.Post, a);
            this.HttpServer.OnPatch += (s, a) => HandleRequest(RequestMethod.Patch, a);
            this.HttpServer.OnPut += (s, a) => HandleRequest(RequestMethod.Put, a);

            //Service
            //HttpServer.AddWebSocketService("/", () => new Gateway.GatewayConnection(this));
            HttpServer.AddWebSocketService<Gateway.EventConnection>(GATEWAY_EVENT_SERVICE, gc => gc.Initialize(this));
            HttpServer.AddWebSocketService<Gateway.LogConnection>(GATEWAY_LOG_SERVICE, gc => gc.Initialize(this));

            //Start the actual server
            Logger.Log("Starting HTTP Server on port {0}, secured: {1}", Port, IsSecure);
            HttpServer.Start();
            return Task.CompletedTask;
        }
        public Task Stop()
        {
            //Stop the server
            HttpServer.Stop();

            //Unregister the listeners
            RestHandler.ClearRoutes();

            return Task.CompletedTask;
        }       
        #endregion

        /// <summary>
        /// Gets all the gateway connections
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetGatewayConnections<T>(string service) where T : GatewayConnection
        {
            var serviceHost = HttpServer.WebSocketServices[service];
            return serviceHost.Sessions.Sessions.Select(s => s as T).Where(s => s != null);
        }

        /// <summary>
        /// Blocks all requests from this address
        /// </summary>
        /// <param name="address"></param>
        public void BlockRequests(string identifier, bool save = false)
        {
            if (BlocklistHandler == null) return;
            BlocklistHandler.BlockAddresses.Add(identifier);
            Configuration.SetKey("blocklist", BlocklistHandler.BlockAddresses, save: save);
        }

        #region credentials

        /// <summary>
        /// Finds the first authentication with matching refresh token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public Authentication GetAuthenticationFromRefreshToken(string refreshToken)
        {
            return _authentications.Values.Where(a => a.Token.HasValue && a.Token.Value.RefreshToken.Equals(refreshToken)).FirstOrDefault();
        }

        /// <summary>
        /// Clears all authentications
        /// </summary>
        public void ClearAuthentications()
        {
            //Clears all authentications, forcing them to be recached.
            _authentications.Clear();
        }

        /// <summary>
        /// Disconnects an authentication from all websocket connections and authentication cache
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="closeStatusCode"></param>
        /// <param name="reason"></param>
        public void DisconnectAuthentication(Authentication auth, WebSocketSharp.CloseStatusCode closeStatusCode = WebSocketSharp.CloseStatusCode.Normal, string reason = "Normal Termination")
        {
            //Clear all the services
            var serviceHost = HttpServer.WebSocketServices[GATEWAY_EVENT_SERVICE];
            foreach (var con in serviceHost.Sessions.Sessions.Select(s => s as GatewayConnection))
            {
                if (con.Authentication == auth)
                    con.Terminate(closeStatusCode, reason);
            }

            //Clear identification
            _authentications.Remove(auth.Identity.Name);
        }

        /// <summary>
        /// Gets a list of all authentications. Used soley for maintainence.
        /// </summary>
        /// <returns></returns>
        public string[] GetAuthenticationNames()
        {
            return _authentications.Keys.ToArray();
        }

        /// <summary>
        /// Attempts to get the authentication. If it does not exist, null will be returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Authentication GetAuthentication(string name)
        {
            Authentication auth = null;
            if (_authentications.TryGetValue(name, out auth))
                return auth;

            return null;
        }

        /// <summary>
        /// Attemps to get the authentication. If it does not exist, it will be created using the identity.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public Authentication CreateAuthentication(IIdentity identity)
        {
            var auth = GetAuthentication(identity.Name);
            if (auth != null) return auth;

            //We are a bot user, so get the bot accounts
            if (identity.Name.StartsWith("bot_"))
            {
                //Find the bot
                Logger.Log("Authorizing bot account {0}", identity.Name);

                //Scan for the account and create the identity if we found it.
                auth = CreateBotAuthentication(identity.Name, identity);
                
            }
            else if(identity.Name.StartsWith("token_"))
            {
                Logger.Log("Authorizing token {0}", identity.Name);

                //Prepare username
                string username = identity.Name.Substring(6);

                //Get the users pervious authentication. If it does not exist then create it.
                Authentication user = GetAuthentication(username);
                if (user == null) user = CreateUserAuthentication(username, identity);

                //Check if the user allows tokens.
                if (user.Token.HasValue) return user;
            }
            else
            {
                //Get the user authentication
                Logger.Log("Authorizating user account {0}", identity.Name);
                auth = CreateUserAuthentication(identity.Name, identity);
            }

            //Add to the cache if its not null then return teh result.
            if (auth != null)  _authentications.Add(auth.Name, auth);
            return auth;
        }

        /// <summary>Creates a bot authentication</summary>
        private Authentication CreateBotAuthentication(string name, IIdentity identity = null)
        {
            BotAccount account;
            if (ScanBotAccounts(name, out account)) return new Authentication(account) { Identity = identity };
            return null;
        }

        /// <summary>Creates a user authentication</summary>
        private Authentication CreateUserAuthentication(string name, IIdentity identity = null)
        {
            var account = Starwatch.Server.Settings.Accounts.GetAccount(identity.Name);
            if (account != null) return new Authentication(account) { Identity = identity };
            return null;
        }


        /// <summary>
        /// Tries to get a bot account, loading the json data and scanning through all accounts.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool ScanBotAccounts(string name, out BotAccount account)
        {
            //Get the actual bot account.
            Logger.Log("Loading bot account {0}", name);
            if (System.IO.File.Exists(BOT_ACCOUNT_FILE))
            {
                string botjson = System.IO.File.ReadAllText(BOT_ACCOUNT_FILE);
                var accounts = JsonConvert.DeserializeObject<BotAccount[]>(botjson);
                for (int i = 0; i < accounts.Length; i++)
                {
                    //Make sure it matches
                    account = accounts[i];
                    if (account.Name.Equals(name)) return true;
                }
            }
            else
            {
                string json = JsonConvert.SerializeObject(
                    new BotAccount[] 
                    {
                        new BotAccount("bot_example")
                        {
                            AuthRedirect = "https://example.com/",
                            IsAdmin = false,
                            Password = "postman"
                        }
                    }, Formatting.Indented);

                System.IO.File.WriteAllText(BOT_ACCOUNT_FILE, json);
            }

            //Nothing matches, abort
            account = default(BotAccount);
            return false;
        }
        #endregion

        #region Gateway Helpers

        /// <summary>
        /// Sends a route event over all the gateway connections that are listening
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="route"></param>
        /// <param name="evt">The name of the event</param>
        /// <param name="reason">The reason for the event</param>
        /// <param name="query"></param>
        internal void BroadcastRoute<T>(T route, string evt, string reason = "", Query query = null) where T : RestRoute, IGatewayRoute
        {
            var serviceHost = HttpServer.WebSocketServices[GATEWAY_EVENT_SERVICE];
            foreach (var con in serviceHost.Sessions.Sessions.Select(s => s as EventConnection))
            {
                try
                {
                    con.SendRoute<T>(route, evt, reason, query);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to send event to connection "+con+": {0}");
                    con.Terminate(WebSocketSharp.CloseStatusCode.ServerError, "event send error");
                }
            }
        }

        /// <summary>
        /// Sends a route event over the gateway but uses a callback to generate the results.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="evt"></param>
        /// <param name="reason"></param>
        /// <param name="query"></param>
        internal void BroadcastRoute(GatewayEventPayloadCallback callback, string evt, string reason = "")
        {
            var serviceHost = HttpServer.WebSocketServices[GATEWAY_EVENT_SERVICE];
            foreach (var con in serviceHost.Sessions.Sessions.Select(s => s as EventConnection))
            {
                try
                {
                    con.SendRoute(callback, evt, reason);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to send event to connection " + con + ": {0}");
                    con.Terminate(WebSocketSharp.CloseStatusCode.ServerError, "event send error");
                }
            }
        }
        
        /// <summary>
        /// Broadcasts a log to all those who are listening
        /// </summary>
        /// <param name="log"></param>
        internal void BroadcastLog(string log)
        {
            var serviceHost = HttpServer.WebSocketServices[GATEWAY_LOG_SERVICE];
            foreach (var con in serviceHost.Sessions.Sessions.Select(s => s as LogConnection))
            {
                try
                {
                    con.SendLog(log);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to send log to connection " + con + ": {0}");
                    con.Terminate(WebSocketSharp.CloseStatusCode.ServerError, "log send error");
                }
            }
        }

       #endregion
    }
}
