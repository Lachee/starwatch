using Starwatch.API.Rest;
using Starwatch.API.Rest.Util;
using Starwatch.API.Util;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Starwatch.API.Web
{
    public class AuthHandler : IRequestHandler
    {
        private const int BUFFER_SIZE = 512;
        private const string ROOT_URL = "oauth/";
        private static readonly Random RNG = new Random();

        public ApiHandler API { get; }

        private object _pendingTokenLock = new object();
        private Dictionary<string, Authentication.AuthToken> _pendingTokens = new Dictionary<string, Authentication.AuthToken>(8);

        public AuthHandler(ApiHandler apiHandler)
        {
            API = apiHandler;          
        }

        public void ClearPendingTokens()
        {
            //Clearing pending tokens
            lock (_pendingTokenLock)
                _pendingTokens.Clear();
        }

        public bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //This isnt a auth request
            if (args.Request.Url.Segments.Length < 3) return false;
            if (args.Request.Url.Segments[1] != ROOT_URL) return false;

            if (!API.IsSecure)
            {
                //Only secure connections can establish auth
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.WriteText("Forbidden\nCannot accept any authorization while the connection is not secure.");
                return true;
            }

            if (args.Request.Url.Segments[2] == "authorize")
                HandleAuthorize(method, args, auth);

            if (args.Request.Url.Segments[2] == "token") 
                HandleToken(method, args, auth);

            return true;
        }

        private void HandleToken(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //Register the action
            auth.RecordAction("auth:token");

            //Make sure its a valid method
            if (method != RequestMethod.Post)
            {
                args.Response.WriteRest(new Rest.RestResponse(Rest.RestStatus.BadMethod, msg: "Post was expected"));
                return;
            }

            //Validate the payload
            if (args.Request.ContentType != ContentType.FormUrlEncoded || !args.Request.HasEntityBody)
            {
                args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "The payload must be x-www-form-urlencoded"));
                return;
            }

            //Only bots can use this endpoint
            if (!auth.IsBot)
            {
                args.Response.WriteRest(new RestResponse(RestStatus.Forbidden, msg: "Only bots are allowed to use this endpoint"));
                return;
            }

            //Read the body
            byte[] buffer = new byte[BUFFER_SIZE];
            int len = args.Request.InputStream.Read(buffer, 0, BUFFER_SIZE);
            string content = args.Request.ContentEncoding.GetString(buffer, 0, len);

            //Parse into a NVK then into a query
            NameValueCollection nvk = System.Web.HttpUtility.ParseQueryString(content);
            Query query = new Query(nvk);

            //Validate the body
            string grantType, code;

            //============ Validate the code
            if (!query.TryGetString("code", out code))
            {
                args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "Invalid Code. Missing."));
                return;
            }

            //============== Validate the grant
            if (!query.TryGetString("grant_type", out grantType) && !(grantType == "authorization_code" || grantType == "refresh_token"))
            {
                args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "Invalid Grant"));
                return;
            }

            
            if (grantType == "refresh_token")
            {
                //We are going to refresh this users token.
                Authentication user = API.GetAuthenticationFromRefreshToken(code);
                if (user == null)
                {
                    args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "Invalid Code"));
                    return;
                }

                //refresh the users token
                user.Token = GenerateToken(TimeSpan.FromHours(24), user.Name);
                args.Response.WriteRest(new RestResponse(RestStatus.OK, res: user.Token.Value));
                return;
            }

            if (grantType == "authorization_code")
            {
                //We are going to return a new token from the code.
                //Look for the pending token and remove it from the listing.
                bool foundToken = false;
                Authentication.AuthToken token;
                lock (_pendingTokenLock)
                {
                    if (_pendingTokens.TryGetValue(code, out token))
                    {
                        foundToken = true;
                        _pendingTokens.Remove(code);
                    }
                }

                //Make sure we actually found it
                if (!foundToken)
                {
                    args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "Invalid Code"));
                    return;
                }

                //Return the new stuffs
                args.Response.WriteRest(new RestResponse(RestStatus.OK, res: token));
                return;
            }
        }
            

        private void HandleAuthorize(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //Register the action
            auth.RecordAction("auth:authorize");

            //Make sure its a valid method
            if (method != RequestMethod.Get)
            {
                args.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                args.Response.Close();
                return;
            }

            //Only users may use this endpoing
            if (!auth.IsUser)
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.Close();
                return;
            }

            //Prepare the query params
            Query query = new Query(args.Request);
            string clientID, redirectURL, responseType, state;
            BotAccount botAccount = null;

            //Get the state
            state = query.GetString("state", "");

            //Validate the clientID
            if (!query.TryGetString("client_id", out clientID) || !API.ScanBotAccounts(clientID, out botAccount))
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.WriteText("Invalid client_id. Please do not ever use this service again, it maybe trying to steal your credentials!");
                return;
            }
            
            //Validate the redirect
            if (!query.TryGetString("redirect_uri", out redirectURL) || !redirectURL.Equals(botAccount.AuthRedirect))
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.WriteText("redirect_uri not supplied from auth application or does not match bot account.");
                return;
            }

            //Validate the response tpye
            if (!query.TryGetString("response_type", out responseType) || !(responseType == "code" || responseType == "token"))
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.WriteText("response_type not supplied from auth application or is invalid.");
                return;
            }

            //If its a token lets return the token asap
            if (responseType == "token")
            {
                //Only admin bots can get tokens
                if (!botAccount.IsAdmin)
                {
                    args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    args.Response.WriteText("response_type not supplied from auth application or is invalid.");
                    return;
                }

                //Generate some values
                auth.Token = GenerateToken(TimeSpan.FromHours(1), auth.Name);

                //Return with the token
                Redirect(args, $"{redirectURL}?access_token={auth.Token.Value.AccessToken}&expires_in={auth.Token.Value.Expiry.ToUnixEpoch()}&state={state}");
                return;
            }

            //Its a simple code one.
            if (responseType == "code")
            {
                //Generate some values;
                string code = GenerateRandomString(RNG, 16);
                auth.Token = GenerateToken(TimeSpan.FromHours(24), auth.Name);

                //Add to the list
                lock (_pendingTokenLock) _pendingTokens.Add(code, auth.Token.Value);
                
                //Redirect
                Redirect(args, $"{redirectURL}?code={code}&state={state}");
                return;
            }
        }

        private void Redirect(HttpRequestEventArgs args, string url)
        {
            var res = args.Response;
            res.AddHeader("location", url);
            res.StatusCode = (int)HttpStatusCode.Redirect;
            res.Close();
        }

        private Authentication.AuthToken GenerateToken(TimeSpan expiry, string username)
        {
            return new Authentication.AuthToken()
            {
                Account = username,
                AccessToken = GenerateRandomString(RNG),
                RefreshToken = GenerateRandomString(RNG),
                Expiry = DateTime.UtcNow + expiry
            };
        }

        /// <summary>
        /// Generates a random string that can be used as a token. Not cryptographically secure.
        /// </summary>
        /// <param name="length">Number of characters in the string</param>
        /// <returns></returns>
        private string GenerateRandomString(Random random, int length = 64)
        {
            string map = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
            StringBuilder builder = new StringBuilder(length);
            for (int i = 0; i < length; i++) builder.Append(map[random.Next(map.Length)]);
            return builder.ToString();
        }
    }
}
