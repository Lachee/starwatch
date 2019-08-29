using Starwatch.API.Rest;
using Starwatch.API.Rest.Util;
using Starwatch.API.Util;
using Starwatch.Entities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Starwatch.API.Web
{
    public class WorldHandler : IRequestHandler
    {
        /// <summary>
        /// The magick bytes for a World File Format. Used for quick validation that the world is off the correct type.
        /// </summary>
        private readonly byte[] WORLD_MAGICK = new byte[] { 0x42, 0x54, 0x72, 0x65, 0x65, 0x44, 0x42, 0x35, 0x00, 0x00, 0x08, 0x00, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x34 };
        
        /// <summary>
        /// The API handler.
        /// </summary>
        public ApiHandler API { get; }

        public bool AllowUploads { get; }
        public bool AllowDownloads { get; }
        public bool AllowDeletion { get; }

        public WorldHandler(ApiHandler apiHandler)
        {
            this.API = apiHandler;
            var config = apiHandler.Configuration.GetConfiguration("world_handler");
            AllowDownloads  = config.GetBool("allow_download", true);
            AllowUploads    = config.GetBool("allow_upload", true);
            AllowDeletion   = config.GetBool("allow_delete", false);
        }

        public bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //This isnt a auth request
            if (args.Request.Url.Segments.Length < 3) return false;
            if (args.Request.Url.Segments[1] != "universe/") return false;
            if (args.Request.Url.Segments[2] != "world/") return false;

            //Admin users only. Bots are forbidden.
            if (auth.AuthLevel < AuthLevel.Admin)
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.Close();
                return true;
            }

            //Get the world part
            string whereami = args.Request.Url.Segments[3];

            //Strip the extension
            string extension = Path.GetExtension(whereami);
            if (extension.Equals(".json")) whereami = Path.GetFileNameWithoutExtension(whereami);

            //Get the world
            var world = World.Parse(whereami);
            if (world == null)
            {
                args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "World is invalid format."));
                return true;
            }

            //Make sure it has a file
            if (string.IsNullOrWhiteSpace(world.Filename))
            {
                args.Response.WriteRest(new RestResponse(RestStatus.BadRequest, msg: "World does nto have a file. It is actually a " + world.GetType().FullName));
                return true;
            }

            //Switch the extension
            switch(extension)
            {
                default:
                case ".json":
                    return HandleWorldJson(method, args, auth, world);

                case ".system":
                case ".world":
                    return HandleWorldFile(method, args, auth, world);
            }
        }

        private bool HandleWorldJson(RequestMethod method, HttpRequestEventArgs args, Authentication auth, World world)
        {
            //Make sure it has a file
            if (string.IsNullOrWhiteSpace(world.Filename))
            {
                args.Response.WriteRest(RestStatus.BadRequest, "World does not have a file. It is actually a " + world.GetType().FullName);
                return true;
            }

            //Wait for it to finish
            args.Response.KeepAlive = true;

            //Try to load the file
            var task = Task.Run(async () =>
            {
                try
                {
                    //Trigger a auth request
                    auth.RecordAction("world:json:download");

                    //Export the json data. We won't allow overwrites so it wont even attempt to generate a new one.
                    string filepath = await world.ExportJsonDataAsync(API.Starwatch.Server, overwrite: false);

                    //Write the file that we get back
                    args.Response.WriteFile(filepath);

                }
                catch (FileNotFoundException)
                {
                    //We failed to find a file
                    args.Response.WriteRest(RestStatus.ResourceNotFound, "The world hasn't generated a save file yet.");
                }
            });

            //Wait for the task to finish
            task.Wait();
            return true;
        }

        private bool HandleWorldFile(RequestMethod method, HttpRequestEventArgs args, Authentication auth, World world)
        {
            //If it is a get, then return the file
            if (AllowDownloads && method == RequestMethod.Get)
            {
                //Trigger a auth request
                auth.RecordAction("world:download");

                args.Response.WriteFile(world.GetAbsolutePath(API.Starwatch.Server));
                return true;
            }

            //If it is a post, then upload the file (if allowed)
            if (AllowUploads && method == RequestMethod.Post)
            {
                //Make sure we have permission
                if (auth.AuthLevel < AuthLevel.SuperBot)
                {
                    args.Response.WriteRest(RestStatus.Forbidden, "Only super-bot or super-users may upload world files.");
                    return true;
                }

                //Make sure we have a body
                if (!args.Request.HasEntityBody)
                {
                    args.Response.WriteRest(RestStatus.BadRequest, "Request has no body.");
                    return true;
                }
                
                //Trigger a auth request
                auth.RecordAction("world:upload");

                //Make sure its a valid multipart
                var multipart = args.Request.ReadMultipart();
                if (!multipart.Success)
                {
                    args.Response.WriteRest(RestStatus.BadRequest, "Uploaded multi-part data was invalid.");
                    return true;
                }

                //Make sure we start with the header
                if (!multipart.Content.Take(WORLD_MAGICK.Length).SequenceEqual(WORLD_MAGICK))
                {
                    args.Response.WriteRest(RestStatus.BadRequest, "Uploaded file is not a world file.");
                    return true;
                }

                //Make sure the world isn't loaded
                if (API.Starwatch.Server.IsRunning && API.Starwatch.Server.Connections.GetCopiedPlayersEnumerable().Any(p => (p.Location?.Equals(world)).GetValueOrDefault(false)))
                {
                    args.Response.WriteRest(RestStatus.BadRequest, "World is in use.");
                    return true;
                }

                //World is loaded, its passed all our checks... I guess finally upload it
                File.WriteAllBytes(world.GetAbsolutePath(API.Starwatch.Server), multipart.Content);

                //Return the json, fully loaded
                return HandleWorldJson(RequestMethod.Get, args, auth, world);
            }
            
            if (AllowDeletion && method == RequestMethod.Delete)
            {
                //Make sure we have permission
                if (auth.AuthLevel < AuthLevel.SuperBot)
                {
                    args.Response.WriteRest(RestStatus.Forbidden, "Only super-bot or super-users may delete world files.");
                    return true;
                }

                //Make sure the world isn't loaded
                if (API.Starwatch.Server.IsRunning && API.Starwatch.Server.Connections.GetCopiedPlayersEnumerable().Any(p => (p.Location?.Equals(world)).GetValueOrDefault(false)))
                {
                    args.Response.WriteRest(RestStatus.BadRequest, "World is in use.");
                    return true;
                }

                //Trigger a auth request
                auth.RecordAction("world:delete");

                //Delete the world
                File.Delete(world.GetAbsolutePath(API.Starwatch.Server));

                //Make sure the json exists, if it does delete that too
                if (File.Exists(world.GetAbsoluteJsonPath(API.Starwatch.Server)))
                    File.Delete(world.GetAbsoluteJsonPath(API.Starwatch.Server));
            }

            //Bad Method
            args.Response.WriteRest(RestStatus.BadMethod, "Bad Method");
            return true;
        }
    }
}
