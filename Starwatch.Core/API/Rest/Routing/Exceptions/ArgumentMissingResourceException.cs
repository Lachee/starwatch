using System;

namespace Starwatch.API.Rest.Routing.Exceptions
{
    public class ArgumentMissingResourceException : Exception
    {
        public string ResourceName { get; }
        public ArgumentMissingResourceException(string resourceName) : base($"{resourceName} is missing its resource")
        {
            ResourceName = resourceName;
        }
    }
}
