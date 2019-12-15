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
namespace Starwatch.API.Rest
{
    public enum RestStatus
    {
        /// <summary>
        /// Everything executed fine.
        /// </summary>
        OK = 0,

        /// <summary>
        /// The request was successful, but was asyncronous and will return nothing useful.
        /// </summary>
        Async = 1,

        /// <summary>
        /// There was an issue with the request, such as a bad payload parameter or missing queries.
        /// </summary>
        BadRequest = 4000,

        /// <summary>
        /// Authentication does not have permission to this route
        /// </summary>
        Forbidden = 4030,

        /// <summary>
        /// The HTTP Method did not exist. For example, attempting to call POST on a GET endpoint.
        /// </summary>
        BadMethod = 4050,

        /// <summary>
        /// The route was not found
        /// </summary>
        RouteNotFound = 4040,

        /// <summary>
        /// The resource was not found.
        /// </summary>
        ResourceNotFound = 4041,

        /// <summary>
        /// The account has made to many requests and the current one has been rejected.
        /// </summary>
        TooManyRequests = 4290,

        /// <summary>
        /// An internal error has occured while processing the request.
        /// </summary>
        InternalError = 5000,
        
        /// <summary>
        /// The route exists, but hasn't been implemented yet.
        /// </summary>
        NotImplemented = 5010,

        /// <summary>
        /// The connection was terminated during the request. This could be because of internal errors or security issues.
        /// </summary>
        Terminated = 5030,
    }
}
