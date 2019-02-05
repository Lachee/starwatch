using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Rest.Serialization
{
    public interface IArgumentConverter
    {
        bool TryConvertArgument(RestHandler context, string value, out object result);
    }
}
