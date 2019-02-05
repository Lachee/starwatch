using Starwatch.API.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Rest.Serialization
{
    class AccountConverter : IArgumentConverter
    {
        public bool TryConvertArgument(RestHandler context, string value, out object result)
        {
            //Get the account
            result = context.Starbound.Settings.Accounts.GetAccount(value);
            return true;
        }
    }
}
