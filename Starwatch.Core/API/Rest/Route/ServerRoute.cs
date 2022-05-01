/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
using Newtonsoft.Json.Linq;
using Starwatch.API.Rest.Routing;
using Starwatch.Starbound;
using System;
using Starwatch.API.Web;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;
using System.Threading.Tasks;
using Starwatch.Extensions;

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
            Payload payload = new Payload
            {
                AllowAnonymousConnections = null,
                AllowAssetsMismatch = null,
                MaxPlayers = null,
                ServerName = null
            };

            try
            {
                payload = (Payload)payloadObject;
            }
            catch { }

            bool reload = query.GetBool("reload", true);
            bool async = query.GetBool(Query.AsyncKey, false);
            bool includeAll = query.GetBool("full", false);
            bool dirty = false;

            if (payload.AllowAnonymousConnections.HasValue)
            {
                Handler.Starbound.Configurator.AllowAnonymousConnections = payload.AllowAnonymousConnections.Value;
                dirty = true;
            }

            if (payload.AllowAssetsMismatch.HasValue)
            {
                Handler.Starbound.Configurator.AllowAssetsMismatch = payload.AllowAssetsMismatch.Value;
                dirty = true;
            }

            if (payload.MaxPlayers.HasValue)
            {
                Handler.Starbound.Configurator.MaxPlayers = payload.MaxPlayers.Value;
                dirty = true;
            }

            if (payload.ServerName != null)
            {
                Handler.Starbound.Configurator.ServerName = payload.ServerName;
                dirty = true;
            }

            Task genericTask;

            #region Saving server config if changes were made
            if (dirty)
            {
                if (async)
                {
                    genericTask = Handler.Starbound.Configurator.SaveAsync(reload)
                    .CallAsyncWithLog(
                        Starbound.Logger,
                        "Exception saving config: {0}"
                    );
                }
                else
                {
                    Handler.Starbound.Configurator.SaveAsync(reload).RunSynchronously();
                }
            }
            #endregion

            #region Reloading server if no changes were made
            if (reload && !dirty)
            {
                if (async)
                {
                    genericTask = Starbound.Rcon.ReloadServerAsync()
                    .CallAsyncWithLog(
                        Starbound.Logger,
                        "Exception reloading server config: {0}"
                    );
                }
                else
                {
                    var resp = Starbound.Rcon.ReloadServerAsync().Result;
                    var serverSettings = GetCulledServerSettings(false, includeAll: includeAll);

                    if (!resp.Success)
                        return new RestResponse(RestStatus.InternalError, "Could not reload the server", res: serverSettings);

                    else
                        return new RestResponse(RestStatus.OK, res: serverSettings);
                }
            }
            #endregion

            if (async)
                return new RestResponse(RestStatus.Async);

            return new RestResponse(RestStatus.OK, res: GetCulledServerSettings(false, includeAll: includeAll));
        }

        public override RestResponse OnGet(Query query)
        {
            bool includeAll = query.GetBool("full", false);
            var settings = GetCulledServerSettings(false, includeAll: includeAll);
            return new RestResponse(RestStatus.OK, res: settings);
        }
        public override RestResponse OnDelete(Query query)
        {
            //The authentication is forbidden from accessing DELETE /server
            if (AuthenticationLevel < AuthLevel.Admin)
                return RestResponse.Forbidden;


            //Delete the server and wait for it.
            bool async = query.GetBool(Query.AsyncKey, false);
            var task = Handler.Starbound.Terminate(query.GetString("reason", "REST Shutdown by " + Authentication.ToString()));
            if (async) return RestResponse.Async;
            
            //Wait for the termination and continue
            task.Wait();
            return new RestResponse(RestStatus.OK);
        }


        public JObject GetCulledServerSettings(bool requireSave = true, bool includeAll = false)
        {

            if (AuthenticationLevel < AuthLevel.Admin)
            {
                //Less than admin, return null;
                return new JObject();
            }
            else if (AuthenticationLevel > AuthLevel.Bot && includeAll)
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

            //Admin, return the basics
            return JObject.FromObject(Handler.Starbound.Configurator);
        }
    }
}
