using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using Starwatch.API.Gateway.Log.Models;
using Starwatch.API.Gateway.Log.Payload;
using Starwatch.Entities;
using System.Threading;

namespace Starwatch.API.Gateway
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GatewayConnection : WebSocketBehavior
    {
        public static long ConnectionCount { get; private set; }
        private static long GetNextConnectionID() { return ConnectionCount++; }

        /// <summary>
        /// The unique ID of the connection
        /// </summary>
        [JsonProperty]
        public long ConnectionID { get; }

        /// <summary>
        /// Text representation of the connection. Will use the Authentication name if available, otherwise the user's endpoint.
        /// </summary>
        [JsonProperty]
        public string Identifier => (Authentication != null ? Authentication.ToString() : Context.UserEndPoint.Address.ToString()) + $"#{ConnectionID}";

        /// <summary>
        /// The authentication of the gateway connection
        /// </summary>
        public Authentication Authentication
        {
            get
            {
                if (_authentication == null)
                    ValidateAuthentication();

                return _authentication;
            }
        }
        private Authentication _authentication;

        /// <summary>
        /// Has the connection terminated?
        /// </summary>
        [JsonProperty]
        public bool HasTerminated { get; private set; }


        /// <summary>
        /// The logger
        /// </summary>
        public Logging.Logger Logger { get; private set; }

        /// <summary>
        /// The API 
        /// </summary>
        public ApiHandler API { get; private set; }

        /// <summary>
        /// Creates a new gateway connection
        /// </summary>
        public GatewayConnection() : base()
        {
            ConnectionID = GetNextConnectionID();
            _authentication = null;
            HasTerminated = false;
        }

        /// <summary>
        /// Initializes the handler
        /// </summary>
        /// <param name="api"></param>
        public void Initialize(ApiHandler api)
        {
            API = api;
            Logger = new Logging.Logger("WS#" + ConnectionID, api.Logger);
        }

        /// <summary>
        /// Validates and sets the current authentication, closing the connection if its invalid with PolicyViolation.
        /// </summary>
        /// <returns></returns>
        protected bool ValidateAuthentication()
        {
            _authentication = API.CreateAuthentication(this.Context.User.Identity);

            //Reject Anonymous
            if (Authentication == null)
            {
                Logger.LogError("Unauthorized anonymous connection");
                Terminate(CloseStatusCode.PolicyViolation, "Anonymous forbidden from gateway.");
                return false;
            }

            //Reject bad levels
            if (Authentication.AuthLevel < API.Configuration.GetObject("websocket_minimum_auth", AuthLevel.Admin))
            {
                Logger.LogError("Unauthorized connection: " + Authentication);
                Terminate(CloseStatusCode.PolicyViolation, "Authentication forbidden from gateway.");
                return false;
            }

            //We are all good
            return true;
        }

        

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
                Logger.Log("An error has occured within the websocket of connection {0}: {1}", this, e.Message);

            if (e.Exception != null)
                Logger.Log("An exception has occured within the websocket of connection {0}: {1}", this, e.Exception.Message);

            //Error means closed connection
            Terminate(CloseStatusCode.ServerError, e.Message);
        }

        protected override void OnClose(WebSocketSharp.CloseEventArgs e)
        {
            Logger.Log("{0} has closed: ({1}) {2}", this, e.Code, e.Reason);
            if (e.WasClean) Logger.Log("The closure was not clean!");
            HasTerminated = true;
        }

        public void Terminate(CloseStatusCode status = CloseStatusCode.Normal, string reason = "Regular Termination")
        {
            if (HasTerminated) return;

            HasTerminated = true;
            this.Sessions.CloseSession(this.ID, status, reason);
        }        

        public override string ToString() => $"Gateway({Identifier})";
    }
}
