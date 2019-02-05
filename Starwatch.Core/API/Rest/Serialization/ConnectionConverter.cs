using Starwatch.API.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Rest.Serialization
{
    class ConnectionConverter : IArgumentConverter
    {
        public bool TryConvertArgument(RestHandler context, string value, out object result)
        {
            //Make sure the connection ID is valid
            int cid;
            if (!int.TryParse(value, out cid))
            {
                result = null;
                return false;
            }

            //Get the player.
            result = context.Starbound.Connections.GetPlayer(cid);
            return true;
        }
    }
}
