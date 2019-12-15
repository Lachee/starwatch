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
using Starwatch.API.Rest.Serialization;

namespace Starwatch.API.Rest.Routing
{
    /// <summary>
    /// Tells the router that the property is an argument it should set
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    class ArgumentAttribute : Attribute
    {
        /// <summary>
        /// The name of the argument
        /// </summary>
        public string ArgumentName { get; }

        /// <summary>
        /// The converter of the argument. Null for default converters.
        /// </summary>
        public Type Converter
        {
            get => _converter;
            set
            {
                if (value != null && value.IsAssignableFrom(typeof(IArgumentConverter)))
                    throw new ArgumentException("The type must be a IArgumentConverter or null.", "converter");

                _converter = value;
            }
        }
        private Type _converter { get; set; } = null;

        /// <summary>
        /// What will the parser do if the value comes up as null?
        /// </summary>
        public NullBehaviour NullValueBehaviour { get; set; } = NullBehaviour.ReturnMissingResource;
        public enum NullBehaviour
        {
            /// <summary>
            /// Sets the value to be null
            /// </summary>
            Allow,

            /// <summary>
            /// Ignores the value and skips setting anything
            /// </summary>
            Ignore,

            /// <summary>
            /// Returns a 404 Missing Resource
            /// </summary>
            ReturnMissingResource
        }

        public ArgumentAttribute(string name)
        {
            ArgumentName = name;
        }
    }
}
