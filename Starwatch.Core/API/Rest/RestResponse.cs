using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

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

        [JsonProperty("Message")]
        public string Message { get; }

        [JsonProperty("Route", NullValueHandling = NullValueHandling.Ignore)]
        public string Route { get; set; } = null;

        [JsonProperty("Time", NullValueHandling = NullValueHandling.Ignore)]
        public double Time { get; set; } = 0;

        [JsonProperty("Response")]
        public object Response { get; }

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
