using Newtonsoft.Json;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using System;
using System.Threading.Tasks;
using WebSocketSharp;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Rest;
using Starwatch.API.Util;

namespace Starwatch.API.Gateway
{
    delegate object GatewayEventPayloadCallback(EventConnection g);
    class EventConnection : GatewayConnection
    {
        public AuthLevel AuthLevel => Authentication.AuthLevel;
        public override string ToString() => $"GatewayEvent({Identifier})";

        public class ClientRequest
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Method { get; set; } = "GET";
            public string Endpoint { get; set; }
            public Query Query { get; set; }
            public string Payload { get; set; }
        }

        protected override void OnMessage(MessageEventArgs msg)
        {
            //Make sure we are not terminated
            if (HasTerminated)
            {
                Logger.Log("Received a message from a terminated connection.");
                return;
            }

            //Validate the authentication
            if (Authentication == null && !ValidateAuthentication())
                return;

            //Make sure its binary
            if (!msg.IsText)
            {
                Logger.Log("Tried to get data but it wasnt sent in text");
                Terminate(CloseStatusCode.UnsupportedData, "Text only");
                return;
            }

            //Try to parse
            try
            {
                //Parse the request and the method
                var req = JsonConvert.DeserializeObject<ClientRequest>(msg.Data);
                RequestMethod method = (RequestMethod)Enum.Parse(typeof(RequestMethod), req.Method, true);

                //Manual overrides for some authentication levels
                AuthLevel authLevel = Authentication.AuthLevel;
                switch(req.Endpoint)
                {
                    case "/player/all":
                        authLevel = AuthLevel.SuperBot;
                        break;
                }
                
                //Execute the response and return it in a event
                RestResponse response = API.RestHandler.ExecuteRestRequest(method, req.Endpoint, req.Query, req.Payload, Authentication, authLevel, ContentType.JSON);
                SendRoute((gateway) => response, "OnCallback", req.Endpoint);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while parsing: {0}");
                Terminate(CloseStatusCode.ServerError, e.Message);
            }
        }

        /// <summary>
        /// Sends a specified route with query
        /// </summary>
        /// <param name="route"></param>
        /// <param name="evt">The name of the event</param>
        /// <param name="query"></param>
        /// <param name="reason">The reason for the event</param>
        public void SendRoute<T>(T route, string evt, string reason = "", Query query = null) where T : RestRoute, IGatewayRoute
        {
            //Validate the authentication
            if (Authentication == null && !ValidateAuthentication())
                return;

            //Set the gateway
            route.SetGateway(this);

            //Get the response and set its endpoint
            var response = new EventResponse(route.OnGet(query ?? new Query()))
            {
                Route = route.GetRouteName(),
                Event = evt,
                Reason = reason,
            };

            //Invoke the OnGet asn serialize the response
            string json = JsonConvert.SerializeObject(response);
            _ = Task.Run(() => Send(json));
        }

        /// <summary>
        /// Sends a callback route with query
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="evt"></param>
        /// <param name="reason"></param>
        /// <param name="query"></param>
        public void SendRoute(GatewayEventPayloadCallback callback, string evt, string reason = "")
        { 
            
            //Validate the authentication
            if (Authentication == null && !ValidateAuthentication())
                return;

            //Prepare rest response
            object data = null;

            try
            {
                //Execute the callback and make sure we have a real result
                data = callback(this);
            }
            catch (Exception e)
            {
                //If any error occurs, skip!
                Logger.LogError("Failed to send connection {0} event because callback error: {1}", this.ToString(), e.Message);
                return;
            }

            //Make sure data isnt null
            if (data == null) return;

            //Create a new event response
            EventResponse eventResponse = data is RestResponse ? new EventResponse(data as RestResponse) : new EventResponse(data);
            eventResponse.Event = evt;
            eventResponse.Reason = reason;
            
            //Send if off to the socket
            string json = JsonConvert.SerializeObject(eventResponse);
            _ = Task.Run(() => Send(json));
        }

        public class EventResponse : RestResponse
        {
            [JsonProperty]
            public string Event { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Reason { get; set; }

            public EventResponse(object response) : base(RestStatus.OK, res: response)
            {
            }

            public EventResponse(Rest.RestResponse restResponse) 
                : base(restResponse.Status, restResponse.Message, restResponse.Response)
            {
                Route = restResponse.Route;
                Time = restResponse.Time;
            }
        }
    }

}
