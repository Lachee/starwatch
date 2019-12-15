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

namespace Starwatch.API.Rest
{
    public class RestResponse
    {
        public static RestResponse OK => new RestResponse(RestStatus.OK);
        public static RestResponse BadMethod => new RestResponse(RestStatus.BadMethod, msg: "Method for the endpoint has not been implemented or doesnt exist.");
        public static RestResponse NotImplemented => new RestResponse(RestStatus.NotImplemented, msg: "Endpoint has not been implemented.");
        public static RestResponse Forbidden => new RestResponse(RestStatus.Forbidden, msg: "Authorization is forbidden from performing the action.");
        public static RestResponse ResourceNotFound => new RestResponse(RestStatus.ResourceNotFound, msg: "The requested resource could not be found.");
        public static RestResponse Async => new RestResponse(RestStatus.Async);

        [JsonProperty("Status")]
        public RestStatus Status { get; }

        [JsonProperty("Route", NullValueHandling = NullValueHandling.Ignore)]
        public string Route { get; set; } = null;

        [JsonProperty("Message")]
        public string Message { get; }

        [JsonProperty("Type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type => Response?.GetType().FullName;

        [JsonProperty("Response")]
        public object Response { get; }

        [JsonProperty("ExecuteTime", NullValueHandling = NullValueHandling.Ignore)]
        public double Time { get; set; } = 0;


        /// <summary>
        /// Creates a new NotImplemented rest response
        /// </summary>
        public RestResponse()
        {
            this.Status = RestStatus.NotImplemented;
            this.Message = "This endpoint has not yet been implemented. Sorry.";
            this.Response = null;
        }

        /// <summary>
        /// Creates a new RestResponse with status and set message. Response will be null.
        /// </summary>
        /// <param name="status">Status of the response.</param>
        /// <param name="message">Message of the response.</param>
        public RestResponse(RestStatus status, string message)
        {
            this.Status = status;
            this.Message = message;
            this.Response = null;
        }

        /// <summary>
        /// Creates a new RestReponse with status and set response. Message will be null.
        /// </summary>
        /// <param name="status">Status of the response.</param>
        /// <param name="response">The response.</param>
        public RestResponse(RestStatus status, object response)
        {
            this.Status = status;
            this.Message = null;
            this.Response = response;
        }

        /// <summary>
        /// Creates a new RestResponse with only a status. Message will be null and response will be a empty string.
        /// </summary>
        /// <param name="status">Status of the response.</param>
        public RestResponse(RestStatus status, string msg = null, object res = null)
        {
            this.Status = status;
            this.Message = msg;
            this.Response = res;
        }
    }
}
