using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/meta/authentication/:account", AuthLevel.Bot)]
    class MetaAuthenticationDetailsRoute : RestRoute
    {
        [Argument("account", NullValueBehaviour = ArgumentAttribute.NullBehaviour.Ignore)]
        public string AccountName { get; set; }

        public MetaAuthenticationDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnGet(Query query)
        {
            Authentication authentication;
            if (AccountName.Equals("@me")) 
            {
                authentication = Authentication;
            }
            else
            {
                if (AuthenticationLevel < AuthLevel.SuperBot) return RestResponse.Forbidden;
                authentication = Handler.ApiHandler.GetAuthentication(AccountName);
            }

            //We could not find the authentication
            if (authentication == null)
                return RestResponse.ResourceNotFound;

            //Return the authentication
            return new RestResponse(RestStatus.OK, authentication);
        }
    }
}
