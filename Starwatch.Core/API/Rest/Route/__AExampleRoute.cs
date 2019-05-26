using Starwatch.API.Web;

namespace Starwatch.API.Rest.Routing
{
    //[Route("/example/:id/backup")]
    class __AExampleRoute : RestRoute
    {
        [Argument("id")]
        public string Identifier { get; set; }

        public __AExampleRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnGet(Query query)
        {
            //System.Threading.Thread.Sleep(3000);
            return new RestResponse(RestStatus.OK, res: Identifier.Substring(10000));
        }
    }
}
