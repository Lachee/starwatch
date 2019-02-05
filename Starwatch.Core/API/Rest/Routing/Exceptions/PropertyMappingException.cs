using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Starwatch.API.Rest.Routing.Exceptions
{
    public class PropertyMappingException : Exception
    {
        public PropertyInfo Property { get; }
        public string ArgumentName { get; }

        public PropertyMappingException(PropertyInfo property, string argumentName, string message) : base (message)
        {
            Property = property;
            ArgumentName = argumentName;
        }
    }
}
