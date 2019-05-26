using System;

namespace Starwatch.Exceptions
{
    class ServerShutdownException : Exception
    {
        public ServerShutdownException(string reason) : base(reason) { }
    }
}
