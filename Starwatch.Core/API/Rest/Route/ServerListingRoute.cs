using Newtonsoft.Json.Linq;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/server/statistics/listing", AuthLevel.Bot)]
    class ServerListingRoute : RestRoute
    {
        public override bool Silent => true;

        public ServerListingRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }


        public override RestResponse OnGet(Query query)
        {
            var stat = new CompoundStatistic();
            stat.Statistics =  Handler.Starbound.GetStatisticsAsync().Result;
            stat.Configurator = Handler.Starbound.Configurator;

            return new RestResponse(RestStatus.OK, res: stat);
        }

        class CompoundStatistic
        {
            public Starbound.Statistics Statistics { get; set; }
            public Starbound.Configurator Configurator { get; set; }
        }


    }
}
