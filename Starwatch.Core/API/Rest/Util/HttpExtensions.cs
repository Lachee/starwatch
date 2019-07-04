using WebSocketSharp.Net;
using Starwatch.API.Util;

namespace Starwatch.API.Rest.Util
{
    static class RestExtensions
    {
        /// <summary>
        /// Writes an error message
        /// </summary>
        /// <param name="response"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        public static void WriteRest(this HttpListenerResponse response, RestStatus status, string message)
        {
            response.WriteRest(new RestResponse(status, msg: message));
        }

        /// <summary>
        /// Writes a rest response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="rest"></param>
        public static void WriteRest(this HttpListenerResponse response, RestResponse rest)
        {
            switch(rest.Status)
            {
                default:
                case RestStatus.Async:
                case RestStatus.OK:
                    response.StatusCode = (int)HttpStatusCode.OK;
                    break;

                case RestStatus.Forbidden:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    break;

                case RestStatus.RouteNotFound:
                case RestStatus.ResourceNotFound:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case RestStatus.Terminated:
                    response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    break;

                case RestStatus.TooManyRequests:
                    response.StatusCode = 429; //TooManyRequests in Http2
                    break;

                case RestStatus.NotImplemented:
                    response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    break;

                case RestStatus.InternalError:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;

                case RestStatus.BadRequest:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case RestStatus.BadMethod:
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    break;
                    
            }

            response.WriteJson(rest);
        }
    }
}
