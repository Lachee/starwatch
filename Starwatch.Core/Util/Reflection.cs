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

namespace Starwatch.Util
{
    public static class Reflection
    {
        /// <summary>
        /// Alias of <see cref="Type.GetConstructor(Type[])"/> but using params.
        /// </summary>
        /// <param name="type">self type</param>
        /// <param name="parameters">The parameters in the constructor</param>
        /// <returns></returns>
        public static System.Reflection.ConstructorInfo GetConstructor(this Type type, params Type[] parameters) => type.GetConstructor(parameters);

        /// <summary>
        /// Alias of <see cref="System.Reflection.ConstructorInfo.Invoke(object[])"/> but using params.
        /// </summary>
        /// <param name="constructorInfo"></param>
        /// <param name="parameters">The parameters in the constructor</param>
        /// <returns></returns>
        public static object Invoke(this System.Reflection.ConstructorInfo constructorInfo, params object[] parameters) => constructorInfo.Invoke(parameters);
        
    }
}
