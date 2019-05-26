using Starwatch.API.Gateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Gateway.Event
{
    internal interface IGatewayRoute
    {
        /// <summary>
        /// Updates the route with the gateway properties. The Authentication and RestHandler route are set here.
        /// </summary>
        /// <param name="gateway"></param>
        void SetGateway(EventConnection gateway);

        /// <summary>
        /// Gets the route name
        /// </summary>
        /// <returns></returns>
        string GetRouteName();
    }
}
