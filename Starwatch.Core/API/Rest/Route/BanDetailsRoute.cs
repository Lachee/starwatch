using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/ban/:ticket", AuthLevel.Admin)]
    class BanDetailsRoute : RestRoute
    {
        [Argument("ticket", Converter = typeof(BanConverter))]
        public Ban Ban { get; set; }

        public override Type PayloadType => typeof(Ban);

        public BanDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets a ban
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            return new RestResponse(RestStatus.OK, res: Ban);
        }

        /// <summary>
        /// Deletes a ban
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {
            //Failed to remove the ban
            if (!Starbound.Settings.RemoveBan(Ban))
                return new RestResponse(RestStatus.OK, res: false);

            //Save the settings and reload
            var task = Starbound.SaveSettings(true);
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return new RestResponse(RestStatus.OK, res: task.Result);
        }

    }
}
