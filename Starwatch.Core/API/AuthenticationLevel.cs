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
namespace Starwatch.API
{
    /// <summary>
    /// The type of authentication, ordered in terms of permission level, where SUperUser is the highest.
    /// </summary>
    public enum AuthLevel
    {
        /// <summary>
        /// Anonymous users have a level of 0 and are never allowed to access the API.
        /// </summary>
        Anonymous = 0,

        /// <summary>
        /// The most basic authentication. A user is someone with a starbound account
        /// </summary>
        User = 1,

        /// <summary>
        /// The next step up from <see cref="User"/>, a Admin is someone with a starbound account that is admin. This will allow access to most parts of the API
        /// </summary>
        Admin = 2,

        /// <summary>
        /// A bot is an account specifically designed and made to access the API. They get access to more advance things within the api.
        /// </summary>
        Bot = 3,
        
        /// <summary>
        /// A super bot is a bot made by someone who is also a <see cref="SuperUser"/> or is otherwise trusted.
        /// </summary>
        SuperBot = 4,

        /// <summary>
        /// A super user is a root user, specifically designated to be allowed to do anything.
        /// </summary>
        SuperUser = 10

    }
}
