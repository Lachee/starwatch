using Starwatch.API.Gateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Rest.Route
{
    internal interface IGatewayRoute
    {
        /// <summary>
        /// Updates the route with the gateway properties. The Authentication and RestHandler route are set here.
        /// </summary>
        /// <param name="jsonConnection"></param>
        void SetGateway(GatewayJsonConnection jsonConnection);

        /// <summary>
        /// Gets the route name
        /// </summary>
        /// <returns></returns>
        string GetRouteName();
    }
}
