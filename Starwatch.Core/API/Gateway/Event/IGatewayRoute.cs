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
