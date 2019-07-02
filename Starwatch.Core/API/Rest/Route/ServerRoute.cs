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
            bool reload = query.GetBool("reload", true);
            bool async = query.GetBool(Query.AsyncKey, false);

            if (payload.AllowAnonymousConnections.HasValue) Handler.Starbound.Configurator.AllowAnonymousConnections = payload.AllowAnonymousConnections.Value;
            if (payload.AllowAssetsMismatch.HasValue) Handler.Starbound.Configurator.AllowAssetsMismatch = payload.AllowAssetsMismatch.Value;
            if (payload.MaxPlayers.HasValue) Handler.Starbound.Configurator.MaxPlayers = payload.MaxPlayers.Value;
            if (payload.ServerName != null) Handler.Starbound.Configurator.ServerName = payload.ServerName;

            //Save the settings and reload
            if (reload)
            { 
                var task = Handler.Starbound.SaveConfigurationAsync(true);
                if (async) return RestResponse.Async;
                return new RestResponse(RestStatus.OK, res: task.Result ? GetCulledServerSettings(false) : null);
            }

            return new RestResponse(RestStatus.OK, res: GetCulledServerSettings());
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
            var task = Handler.Starbound.Terminate(query.GetString("reason", "REST Shutdown by " + Authentication.Identity));
            if (async) return RestResponse.Async;

            //Wait for the termination and continue
            task.Wait();
            return new RestResponse(RestStatus.OK);
        }


        public JObject GetCulledServerSettings(bool requireSave = true)
        {

            if (AuthenticationLevel < AuthLevel.Admin)
            {
                //Less than admin, return null;
                return new JObject();
            }
            else if (AuthenticationLevel <= AuthLevel.Bot)
            {
                //Admin, return the basics
                return JObject.FromObject(Handler.Starbound.Configurator);
            }
            else
            {
                //Save the configuration first, just in case
                if (requireSave)
                    Handler.Starbound.Configurator.SaveAsync(false).Wait();

                //Export the settings
                var settings = Handler.Starbound.Configurator.ExportSettingsAsync().Result;

                //Remove the user and bans.
                var jobj = JObject.FromObject(settings);
                jobj.Remove("serverUsers");
                jobj.Remove("bannedIPs");
                jobj.Remove("bannedUuids");
                return jobj;
            }
        }
    }
}
