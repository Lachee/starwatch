using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Rest.Routing
{
    /// <summary>
    /// Tells the router about new routes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RouteAttribute : Attribute
    {
        public char ArgumentPrefix { get; set; } = ':';
        public string Route { get; }
        public AuthLevel Permission { get; }
        public RouteAttribute(string route, AuthLevel permission = AuthLevel.User)
        {
            this.Route = route;
            this.Permission = permission;
        }
        
        public string[] GetSegments() => Route.Trim('/').Split('/');
    }
}
