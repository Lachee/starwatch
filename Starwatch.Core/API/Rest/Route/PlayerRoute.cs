using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/player")]
    class PlayerRoute : RestRoute
    {
        public PlayerRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            var enumerator = Handler.Starbound.Connections.GetCopiedPlayersEnumerable();

            string username, nickname, accountname, uuid, location;
            bool admin;

            //Filter the result
            if (query.TryGetString("uuid", out uuid)) enumerator = enumerator.Where(p => p.UUID != null && p.UUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("username", out username)) enumerator = enumerator.Where(p => p.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("nickname", out nickname)) enumerator = enumerator.Where(p => p.Nickname.Equals(nickname, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("location", out location)) enumerator = enumerator.Where(p => p.Location.Whereami.Equals(location, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("account", out accountname))
            {
                enumerator = enumerator.Where(p => 
                        accountname.Equals("anonymous", StringComparison.InvariantCultureIgnoreCase) ? 
                            p.AccountName == null : 
                            p.AccountName.Equals(accountname, StringComparison.InvariantCultureIgnoreCase)
                    );
            }

            if (query.TryGetBool("admin", out admin))
                enumerator = enumerator.Where(p => p.AccountName != null && (p.GetAccountAsync().Result?.IsAdmin).GetValueOrDefault(false));

            //Prepare the array
            Dictionary<int, string> players = new Dictionary<int, string>();
            foreach (var p in enumerator) players.Add(p.Connection, p.Username);

            //Return the array
            return new RestResponse(RestStatus.OK, players);
        }
        
    }
}
