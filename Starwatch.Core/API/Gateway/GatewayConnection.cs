/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Starwatch.API.Gateway
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GatewayConnection : WebSocketBehavior
    {
        public static long ConnectionCount { get; private set; }
        private static long GetNextConnectionID() => ConnectionCount++;

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
