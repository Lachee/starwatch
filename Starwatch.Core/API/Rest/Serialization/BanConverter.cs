namespace Starwatch.API.Rest.Serialization
{
    class BanConverter : IArgumentConverter
    {
        public bool TryConvertArgument(RestHandler context, string value, out object result)
        {
            //Make sure the ticket is valid
            long ticket;
            if (!long.TryParse(value, out ticket))
            {
                result = null;
                return false;
            }

            //Get the ban
            result = context.Starbound.Configurator.GetBanAsync(ticket).Result;
            return true;
        }
    }
}
