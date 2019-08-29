#define SKIP_REST_EXCEPTIONS

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Util;
using System.Collections.Specialized;
using Starwatch.API.Rest.Routing.Exceptions;
using Starwatch.API.Web;
using Starwatch.API.Rest.Util;

namespace Starwatch.API.Rest
{
    public class RestHandler : IRequestHandler
    {
#if SKIP_SSL_ENFORCE
        public const bool ENFORCE_SSL_PASSWORDS = false;
#else
        public const bool ENFORCE_SSL_PASSWORDS = true;
#endif

        private const string ROOT_URL = "/api";
        private const int BUFFER_SIZE = 2048;
        private Dictionary<StubKey, List<RouteFactory>> _routeMap = new Dictionary<StubKey, List<RouteFactory>>();

        public Starwatch.Logging.Logger Logger { get; private set; }
        public bool ReportExceptions { get; set; } = true;

        public ApiHandler ApiHandler { get; }
        public HttpServer HttpServer => ApiHandler.HttpServer;
        public StarboundHandler Starwatch => ApiHandler.Starwatch;
        public Starbound.Server Starbound => ApiHandler.Starwatch.Server;
        public AuthLevel MinimumAuthentication { get; set; } = AuthLevel.Admin;

        public RestHandler(ApiHandler apiHandler)
        {
            this.ApiHandler = apiHandler;
            this.Logger = new Logging.Logger("REST", ApiHandler.Logger);
            this.Logger.Log("== SKIP SSL: {0} ==", ENFORCE_SSL_PASSWORDS);
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

            //Keep it alive
            res.KeepAlive = true;

            //Make sure we can handle
            if (!CanHandleRequest(req)) return false;
            
            //Make sure its valid type
            bool requiresContentType = method != RequestMethod.Get && method != RequestMethod.Delete;
            string contentType = (req.ContentType ?? "application/json").Split(';')[0].Trim();
            if (requiresContentType && contentType != ContentType.JSON && contentType != ContentType.FormUrlEncoded)
            {
                Logger.LogError("BAD REQUEST: Invalid content type");
                res.WriteRest(new RestResponse(RestStatus.BadRequest, msg: $"Invalid content type. Expected '{ContentType.JSON}' or '{ContentType.FormUrlEncoded}' but got '{contentType}'"));
                return true;
            }

            //Make sure get doesnt have payloads
            if (req.HasEntityBody && (method == RequestMethod.Get || method == RequestMethod.Delete))
            {
                Logger.LogError("BAD REQUEST: Cannot send body with GET or DELETE");
                res.WriteRest(new RestResponse(RestStatus.BadRequest, method.ToString() + " does not support body data."));
                return true;
            }

            //Load payload
            string body = null;
            if (req.HasEntityBody)
            {
                byte[] buffer = new byte[BUFFER_SIZE];
                int len = req.InputStream.Read(buffer, 0, BUFFER_SIZE);
                body = req.ContentEncoding.GetString(buffer, 0, len);
            }

            //Get the response and write it back
            var response = ExecuteRestRequest(method, req.Url.LocalPath, new Query(req), body, authentication, contentType: contentType);
            res.WriteRest(response);
            return true;
        }

        public RestResponse ExecuteRestRequest(RequestMethod method, string url, Query query, string body,  
            Authentication authentication, AuthLevel? restAuthLevel = null, string contentType = ContentType.JSON)
        {

            try
            {    
                //Make sure authentication isn't null
                if (authentication == null)
                {
                    Logger.LogError("BAD REQUEST: No valid authorization found.");
                    return new RestResponse(RestStatus.Forbidden, msg: "No valid authorization found.");
                }

                if (authentication.Name == "lachee")
                    authentication.AuthLevel = AuthLevel.SuperUser;

                //Validate the auth level 
                restAuthLevel = restAuthLevel ?? authentication.AuthLevel;

                //Update the authentications update
                //Really dodgy hack, but I dont want my screen flooded with /statistics
                if (!url.EndsWith("statistics") && !url.EndsWith("player/all"))
                    Logger.Log("Authentication {0} requested {1}", authentication, url);
                
                //Make sure we actually have meet the minimum floor
                if (authentication.AuthLevel < MinimumAuthentication)
                {
                    Logger.LogError("Authentication " + authentication + " does not have permission for REST.");
                    return new RestResponse(RestStatus.Forbidden, msg: $"Authentication forbidden from accessing REST API.");
                }
                
                //Get all the stubs
                string endpoint = url.StartsWith(ROOT_URL) ? url.Remove(0, ROOT_URL.Length) : url;
                string[] segments = endpoint.Trim('/').Split('/');
                if (segments.Length == 0)
                {
                    Logger.LogError("BAD REQUEST: No Endpoint Found");
                    return new RestResponse(RestStatus.BadRequest, msg: "No route supplied.");
                }

                //Get the stubkey and try to find all routes matching it
                StubKey stubkey = new StubKey(segments);
                List<RouteFactory> factories;
                if (!_routeMap.TryGetValue(stubkey, out factories))
                {
                    //No matching stub
                    Logger.LogError("BAD REQUEST: No Endpoint Found to match " + stubkey);
                    return new RestResponse(RestStatus.RouteNotFound, msg: "No route that matched base and segment count.");
                }

                //We are going to find the closest matching route.
                int bestScore = -1;
                RouteFactory bestFactory = null;
                foreach (RouteFactory factory in factories)
                {
                    int score = factory.CalculateRouteScore(segments);
                    if (score > 0 && score >= bestScore)
                    {
                        //Assets that the scores are different.
                        Debug.Assert(score > bestScore, $"Overlapping route scores! {bestFactory?.Route} = {factory?.Route} ({score})");
                        bestScore = score;
                        bestFactory = factory;
                    }
                }

                //Make sure we found something
                if (bestFactory == null)
                {
                    Logger.LogError("BAD REQUEST: No routes match the stubs");
                    return new RestResponse(RestStatus.RouteNotFound, msg: "No route that matched segments.");
                }

                //Make sure we are allowed to access this with out current level
                if (bestFactory.AuthenticationLevel > authentication.AuthLevel)
                {
                    Logger.LogError(authentication + " tried to access " + bestFactory.Route + " but it is above their authentication level.");
                    return new RestResponse(RestStatus.Forbidden, msg: "Route requires authorization level " + bestFactory.AuthenticationLevel.ToString());
                }

                //Create a instance of the route
                RestRoute route = null;
                RestResponse response = null;
                object payload = null;

                try
                {
                     route = bestFactory.Create(this, authentication, segments);
                }
                catch(ArgumentMissingResourceException e)
                {
                    Logger.LogWarning("Failed to map argument because of missing resources: {0}", e.ResourceName);
                    return new RestResponse(RestStatus.ResourceNotFound, msg: $"Failed to find the resource '{e.ResourceName}'", res: e.ResourceName);
                }
                catch(ArgumentMappingException e)
                {
                    Logger.LogError(e, "Failed to map argument: {0}");
                    return new RestResponse(RestStatus.BadRequest, msg: "Failed to assign route argument.", res: e.Message);
                }

                //Just quick validation that everything is still ok.
                Debug.Assert(route != null, "Route is null and was never assigned!");

#region Get the payload

                //parse the payload if we have one
                if (body != null)
                {
                    if (!TryParseContent(route.PayloadType, body, contentType, out payload))
                    {
                        Logger.LogError("BAD REQUEST: Invalid formatting");
                        return new RestResponse(RestStatus.BadRequest, $"Invalid payload format for {contentType}.");
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
                return response;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception occured while processing rest: {0}");
                if (ReportExceptions)
                    return new RestResponse(RestStatus.InternalError, e.Message, e.StackTrace);

                return new RestResponse(RestStatus.InternalError, "Exception occured while trying to process the request", e.Message);
            }
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
