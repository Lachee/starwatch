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
using System;

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
