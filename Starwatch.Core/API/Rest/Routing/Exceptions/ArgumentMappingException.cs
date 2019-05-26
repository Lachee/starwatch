using System;

namespace Starwatch.API.Rest.Routing.Exceptions
{
    public class ArgumentMappingException : Exception
    {
        public ArgumentMappingException(string message) : base(message) { }
    }
}
