using Newtonsoft.Json;
using Starwatch.API.Rest.Route;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Starwatch.API.Gateway
{
    class GatewayJsonConnection : GatewayConnection
    {
        public override string ToString() => $"GatewayEvent({Identifier})";

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
            //Set the gateway
            route.SetGateway(this);

            //Get the resposne and set its endpoint
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

        public class EventResponse : Rest.RestResponse
        {
            [JsonProperty]
            public string Event { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Reason { get; set; }

            public EventResponse(Rest.RestResponse response) 
                : base(response.Status, response.Message, response.Response)
            {

            }
        }
    }

}
