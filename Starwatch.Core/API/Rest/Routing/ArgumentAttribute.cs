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
