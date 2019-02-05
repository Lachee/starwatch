using System;
using System.Collections.Generic;
using System.Text;

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
