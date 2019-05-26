using Newtonsoft.Json;
using Starwatch.Entities;

namespace Starwatch.API.Rest.Route.Entities
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public struct AccountPatch
    {
        public string Name { get; set; }
        public bool? IsAdmin { get; set; }
        public string Password { get; set; }
        public AccountPatch(Account account)
        {
            Name = account.Name;
            IsAdmin = account.IsAdmin;
            Password = account.Password;
        }

        public Account ToAccount()
        {
            return new Account(Name)
            {
                IsAdmin = IsAdmin.GetValueOrDefault(false),
                Password = Password
            };
        }
    }
}
