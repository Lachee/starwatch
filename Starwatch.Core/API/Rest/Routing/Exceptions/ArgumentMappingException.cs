using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Rest.Routing.Exceptions
{
    public class ArgumentMappingException : Exception
    {
        public ArgumentMappingException(string message) : base(message) { }
    }
}
