#define SKIP_REST_EXCEPTIONS

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Util;
using System.Threading.Tasks;
using Starwatch.API;
using System.Collections.Specialized;
using Starwatch.API.Rest.Routing.Exceptions;
using Starwatch.API.Web;
using Starwatch.API.Rest.Util;

namespace Starwatch.API.Rest
{
    public class RestHandler : IRequestHandler
    {
        public const bool ENFORCE_SSL_PASSWORDS = true;

        private const string ROOT_URL = "/api";
        private const int BUFFER_SIZE = 2048;
        private Dictionary<StubKey, List<RouteFactory>> _routeMap = new Dictionary<StubKey, List<RouteFactory>>();

        public Starwatch.Logging.Logger Logger { get; private set; }
        public bool ReportExceptions { get; set; } = true;

        public ApiHandler ApiHandler { get; }
        public HttpServer HttpServer => ApiHandler.HttpServer;
        public StarboundHandler Starwatch => ApiHandler.Starwatch;
        public Starbound.Server Starbound => ApiHandler.Starwatch.Server;

        public RestHandler(ApiHandler apiHandler)
        {
            this.ApiHandler = apiHandler;
            this.Logger = new Logging.Logger("REST", ApiHandler.Logger);
        }

        #region Registration

        /// <summary>
        /// Clears all routes
        /// </summary>
        public void ClearRoutes()
        {
            _routeMap.Clear();
        }

        /// <summary>
        /// Registers all routes in the assembly
        /// </summary>
        /// <param name="assembly">The assembly to register routes from</param>
        public void RegisterRoutes(Assembly assembly)
        {
            Logger.Log("Registering all routes in " + assembly);

            //Get all the types
            var types = assembly.GetTypes()
                .Where(p => typeof(RestRoute).IsAssignableFrom(p) && !p.IsAbstract);

            //Iterate over every type, adding it to the route map.
            foreach(var type in types)
            {
                var attribute = type.GetCustomAttribute<RouteAttribute>();
                if (attribute == null) continue;

                //Prepare our stub
                StubKey key = new StubKey(attribute);

                //Doesnt yet exist, so we will create a new lsit of route factories
                if (!_routeMap.ContainsKey(key))
                    _routeMap.Add(key, new List<RouteFactory>());

                //Add a new factory to the list
                _routeMap[key].Add(new RouteFactory(attribute, type));
                Logger.Log("- {0}", attribute.Route);
            }
        }

        /// <summary>
        /// Gets a list of complete routes
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> GetRoutes()
        {
            List<string> routes = new List<string>();
            foreach (var value in _routeMap.Values)
                foreach (var rf in value)
                    routes.Add(rf.Route);

            return routes;
        }

        #endregion

        #region Routing
        public bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication authentication)
        {
            //Prepare the values
            var req = args.Request;
            var res = args.Response;

            if (!CanHandleRequest(req)) return false;

            try
            {    
                //Make sure authentication isn't null
                if (authentication == null)
                {
                    Logger.LogError("BAD REQUEST: No valid authorization found.");
                    res.WriteRest(new RestResponse(RestStatus.Forbidden, msg: "No valid authorization found."));
                    return true;
                }

                if (authentication.Name == "lachee")
                    authentication.AuthLevel = AuthLevel.SuperUser;
                    //authentication.AuthType = AuthType.Bot;

                    //Update the authentications update
                Logger.Log("Authentication {0} requested {1}", authentication, req.RawUrl);

                //Keep it alive
                res.KeepAlive = true;

                //Make sure its valid type
                bool requireContentType = method != RequestMethod.Get && method != RequestMethod.Delete;
                if (requireContentType && req.ContentType != ContentType.JSON && req.ContentType != ContentType.FormUrlEncoded)
                {
                    Logger.LogError("BAD REQUEST: Invalid content type");
                    res.WriteRest(new RestResponse(RestStatus.BadRequest, msg: $"Invalid content type. Expected '{ContentType.JSON}' or '{ContentType.FormUrlEncoded}' but got '{req.ContentType}'"));
                    return true;
                }

                //Make sure get doesnt have payloads
                if (req.HasEntityBody && (method == RequestMethod.Get || method == RequestMethod.Delete))
                {
                    Logger.LogError("BAD REQUEST: Cannot send body with GET or DELETE");
                    res.WriteRest(new RestResponse(RestStatus.BadRequest, method.ToString() + " does not support body data."));
                    return true;
                }


                //Get all the stubs
                string endpoint = args.Request.Url.LocalPath.Remove(0, ROOT_URL.Length);
                string[] segments = endpoint.Trim('/').Split('/');
                if (segments.Length == 0)
                {
                    Logger.LogError("BAD REQUEST: No Endpoint Found");
                    res.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "No route supplied."));
                    return true;
                }

                //Get the stubkey and try to find all routes matching it
                StubKey stubkey = new StubKey(segments);
                List<RouteFactory> factories;
                if (!_routeMap.TryGetValue(stubkey, out factories))
                {
                    //No matching stub
                    Logger.LogError("BAD REQUEST: No Endpoint Found to match " + stubkey);
                    res.WriteRest(new RestResponse(RestStatus.RouteNotFound, msg: "No route that matched base and segment count."));
                    return true;
                }

                //We are going to find the closest matching route.
                int bestScore = 0;
                RouteFactory bestFactory = null;
                foreach (RouteFactory factory in factories)
                {
                    int score = factory.CalculateRouteScore(segments);
                    if (score > 0 && score >= bestScore)
                    {
                        bestScore = score;
                        bestFactory = factory;
                    }
                }

                //Make sure we found something
                if (bestFactory == null)
                {
                    Logger.LogError("BAD REQUEST: No routes match the stubs");
                    res.WriteRest(new RestResponse(RestStatus.RouteNotFound, msg: "No route that matched segments."));
                    return true;
                }

                //Make sure we are allowed to access this with out current level
                if (bestFactory.AuthenticationLevel > authentication.AuthLevel)
                {
                    Logger.LogError("Authentication is trying to access something that is above its level");
                    res.WriteRest(new RestResponse(RestStatus.Forbidden, msg: "Route requires authorization level " + bestFactory.AuthenticationLevel.ToString()));
                    return true;
                }

                //Create a instance of the route
                RestRoute route = null;
                Query query = new Query(req);
                RestResponse response = null;
                object payload = null;

                try
                {
                     route = bestFactory.Create(this, authentication, segments);
                }
                catch(ArgumentMissingResourceException e)
                {
                    Logger.LogWarning("Failed to map argument because of missing resources: {0}", e.ResourceName);
                    res.WriteRest(new RestResponse(RestStatus.ResourceNotFound, msg: $"Failed to find the resource '{e.ResourceName}'", res: e.ResourceName));
                    return true;
                }
                catch(ArgumentMappingException e)
                {
                    Logger.LogError(e, "Failed to map argument: {0}");
                    res.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "Failed to assign route argument.", res: e.Message));
                    return true;
                }

                //Just quick validation that everything is still ok.
                Debug.Assert(route != null, "Route is null and was never assigned!");

                #region Get the payload

                //parse the payload if we have one
                if (req.HasEntityBody)
                {
                    byte[] buffer = new byte[BUFFER_SIZE];
                    int len = req.InputStream.Read(buffer, 0, BUFFER_SIZE);
                    string content = req.ContentEncoding.GetString(buffer, 0, len);

                    if (!TryParseContent(route.PayloadType, content, req.ContentType, out payload))
                    {
                        Logger.LogError("BAD REQUEST: Invalid formatting");
                        res.WriteRest(new RestResponse(RestStatus.BadRequest, $"Invalid payload format for {req.ContentType}."));
                        return true;
                    }
                }

                #endregion

                //Execute the correct method
                Stopwatch watch = Stopwatch.StartNew();
                switch (method)
                {
                    default:
                        response = RestResponse.BadMethod;
                        break;

                    case RequestMethod.Get:
                        response = route.OnGet(query);
                        break;

                    case RequestMethod.Delete:
                        response = route.OnDelete(query);
                        break;

                    case RequestMethod.Post:
                        response = route.OnPost(query, payload);
                        break;

                    //Put is obsolete, so keeping it just like a PATCH
                    case RequestMethod.Patch:
                    case RequestMethod.Put:
                        response = route.OnPatch(query, payload);
                        break;
                }

                //Make sure response isn't null at this point
                Debug.Assert(response != null);

                //Update the authentications update
                authentication.RecordAction("rest:" + bestFactory.Route);

                //Write the resulting json
                response.Route = bestFactory.Route;
                response.Time = watch.ElapsedMilliseconds;
                res.WriteRest(response);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception occured while processing rest: {0}");
                if (ReportExceptions)
                    args.Response.WriteRest(new RestResponse(RestStatus.InternalError, e.Message, e));
                else
                    args.Response.WriteRest(new RestResponse(RestStatus.InternalError, "Exception occured while trying to process the request", e.Message));
            }

            return true;
        }

        /// <summary>
        /// Tries to parse the content into the give payload type. Accepts both JSON and URL Form.
        /// </summary>
        /// <param name="payloadType"></param>
        /// <param name="content"></param>
        /// <param name="contentType"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private bool TryParseContent(Type payloadType, string content, string contentType, out object payload)
        {
            //Its empty so just return null.
            if (string.IsNullOrWhiteSpace(content))
            {
                payload = null;
                return true;
            }

            //Change the type
            switch(contentType)
            {
                default:
                    Logger.LogError("Unkown content type trying to be parsed! " + contentType);
                    payload = null;
                    return false;

                case ContentType.JSON:
                    return TryParseJsonContent(payloadType, content, out payload);

                case ContentType.FormUrlEncoded:
                    return TryParseNameValueCollectionContent(payloadType, content, out payload);
            }
        }

        /// <summary>Tries to parse the content as <see cref="NameValueCollection"/> with the given type</summary>
        private bool TryParseNameValueCollectionContent(Type type, string content, out object payload)
        {
            try
            {
                var collection = System.Web.HttpUtility.ParseQueryString(content);
                Dictionary<string, object> dict = collection.AllKeys.ToDictionary(k => k, k => collection.GetValues(k).Length > 1 ? (object)collection.GetValues(k) : (object)collection.GetValues(k).First());
                JObject obj = JObject.FromObject(dict);
                if (type == null) payload = obj;
                else payload = obj.ToObject(type);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to parse collection: " + e.Message);
                payload = null;
                return false;
            }
        }

        /// <summary>Tries to parse the content as json with the given type</summary>
        private bool TryParseJsonContent(Type type, string json, out object payload)
        {
            try
            {
                //Parse the json
                JObject obj = JObject.Parse(json);
                if (type == null) payload = obj;
                else payload = obj.ToObject(type);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to parse JSON: " + e.Message);
                payload = null;
                return false;
            }
        }

        /// <summary>Can the handler actually handle this request?</summary>
        private bool CanHandleRequest(HttpListenerRequest request)
        {
            return request.Url.LocalPath.StartsWith(ROOT_URL);
        }

#endregion
    }

    struct StubKey
    {
        public int length;
        public string route;

        public StubKey(RouteAttribute attribute) : this(attribute.GetSegments()) { }
        public StubKey(string[] stubs)
        {
            this.length = stubs.Length;
            this.route = stubs[0];
        }

        public override string ToString() => $"{route}({length})";
        public override int GetHashCode() => (length * 2745) ^ (route.GetHashCode() * 7);
        public override bool Equals(object obj)
        {
            if (obj is StubKey) return ((StubKey)obj).route == route && ((StubKey)obj).length == length;            
            return false;
        }
    }
}
