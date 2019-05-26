using Starwatch.API.Rest.Routing;
using System.Collections.Generic;
using Starwatch.API.Web;
using System.IO;

namespace Starwatch.API.Rest.Route
{
    [Route("/log", permission: AuthLevel.Admin)]
    class LogRoute : RestRoute
    {
        private const string BASE_LOG = "starbound_server.log";

        public LogRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            List<string> logs = new List<string>(10);
            foreach(var file in Directory.EnumerateFiles(Starbound.StorageDirectory, BASE_LOG + "*"))
            {
                string extension = Path.GetExtension(file);
                string number = (extension.Equals(".log") ? "0" : extension.Remove(0, 1));
                logs.Add(number);
            }
            

            return new RestResponse(RestStatus.OK, logs);
        }
        
    }
}
