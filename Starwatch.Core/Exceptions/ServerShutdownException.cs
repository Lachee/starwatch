using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Exceptions
{
    class ServerShutdownException : Exception
    {
        public ServerShutdownException(string reason) : base(reason) { }
    }
}
