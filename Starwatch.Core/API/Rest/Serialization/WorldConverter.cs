using Starwatch.Entities;

namespace Starwatch.API.Rest.Serialization
{
    class WorldConverter : IArgumentConverter
    {
        public bool TryConvertArgument(RestHandler context, string value, out object result)
        {
            //Make sure the ticket is valid
            result = World.Parse(value);
            return result != null;
        }
    }
}
