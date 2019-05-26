using Newtonsoft.Json.Linq;
using Starwatch.API.Rest.Routing;
using Starwatch.Starbound;
using System;
using Starwatch.API.Web;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;

namespace Starwatch.API.Rest.Route
{
    [Route("/server")]
    class ServerRoute : RestRoute, IGatewayRoute
    {
        public override Type PayloadType => typeof(Payload);
        struct Payload
        {
            public bool? AllowAnonymousConnections { get; set; }
            public bool? AllowAssetsMismatch { get; set; }
            public int? MaxPlayers { get; set; }
            public string ServerName { get; set; }
        }

        public ServerRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public ServerRoute() : base(null, null) { }

        public string GetRouteName() => "/server";
        public void SetGateway(EventConnection gateway)
        {
            this.Handler = gateway.API.RestHandler;
            this.Authentication = gateway.Authentication;
        }

        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            //Only bots can do this
            if (AuthenticationLevel < AuthLevel.Admin) return RestResponse.Forbidden;

            //Prepare values
            Payload payload = (Payload)payloadObject;
            Settings settings = Handler.Starbound.Settings;
            bool reload = query.GetBool("reload", true);
            bool async = query.GetBool(Query.AsyncKey, false);

            if (payload.AllowAnonymousConnections.HasValue) settings.AllowAnonymousConnections    = payload.AllowAnonymousConnections.Value;
            if (payload.AllowAssetsMismatch.HasValue)       settings.AllowAssetsMismatch          = payload.AllowAssetsMismatch.Value;
            if (payload.MaxPlayers.HasValue)                settings.MaxPlayers                   = payload.MaxPlayers.Value;
            if (payload.ServerName != null)                 settings.ServerName                   = payload.ServerName;

            //Save the settings
            var task = Handler.Starbound.SaveSettings(settings, reload);
            if (async) return RestResponse.Async;
            
            return new RestResponse(RestStatus.OK, res: task.Result ? GetCulledServerSettings() : null);
        }

        public override RestResponse OnGet(Query query)
        {
            var settings = GetCulledServerSettings();
            return new RestResponse(RestStatus.OK, res: settings);
        }
        public override RestResponse OnDelete(Query query)
        {
            //The authentication is forbidden from accessing DELETE /server
            if (AuthenticationLevel < AuthLevel.Admin)
                return RestResponse.Forbidden;


            //Delete the server and wait for it.
            bool async = query.GetBool(Query.AsyncKey, false);
            var task = Handler.Starbound.Terminate();
            if (async) return RestResponse.Async;

            //Wait for the termination and continue
            task.Wait();
            return new RestResponse(RestStatus.OK);
        }


        public JObject GetCulledServerSettings()
        {
            JObject settings = JObject.FromObject(Handler.Starbound.Settings);
            if (AuthenticationLevel < AuthLevel.SuperUser)
            {
                settings.Remove("serverUsers");

                settings.Remove("bannedIPs");
                settings.Remove("bannedUuids");

                settings.Remove("rconServerBind");
                settings.Remove("rconServerPort");
                settings.Remove("rconServerPassword");
                settings.Remove("rconServerTimeout");
                settings.Remove("runRconServer");
            }

            if (AuthenticationLevel < AuthLevel.SuperBot)
            {
                settings.Remove("safeScripts");
                settings.Remove("scriptInstructionLimit");
                settings.Remove("scriptInstructionMeasureInterval");
                settings.Remove("scriptProfilingEnabled");
                settings.Remove("scriptRecursionLimit");

                settings.Remove("serverFidelity");
                settings.Remove("serverOverrideAssetsDigest");

                settings.Remove("clientP2PJoinable");
                settings.Remove("clientIPJoinable");
                settings.Remove("clearUniverseFiles");
                settings.Remove("clearPlayerFiles");
                settings.Remove("checkAssetsDigest");

                settings.Remove("bannedTicket");
            }

            if (AuthenticationLevel < AuthLevel.Bot)
            {

            }

            if (AuthenticationLevel < AuthLevel.Admin)
            {
                settings.Remove("queryServerBind");
                settings.Remove("queryServerPort");
                settings.Remove("runQueryServer");

                settings.Remove("anonymousConnectionsAreAdmin");
                settings.Remove("allowAssetsMismatch");
                settings.Remove("allowAnonymousConnections");
                settings.Remove("allowAdminCommandsFromAnyone");
                settings.Remove("allowAdminCommands");
            }

            return settings;
        }
    }
}
