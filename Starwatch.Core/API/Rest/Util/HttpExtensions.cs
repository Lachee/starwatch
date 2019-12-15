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
using WebSocketSharp.Net;
using Starwatch.API.Util;
using System.Threading.Tasks;

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
