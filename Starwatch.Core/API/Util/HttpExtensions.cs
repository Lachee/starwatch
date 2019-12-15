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
using System;
using System.Text;
using WebSocketSharp.Net;
using WebSocketSharp;
using Newtonsoft.Json;
using System.IO;

namespace Starwatch.API.Util
{
    static class ContentType
    {
        public const string JSON = "application/json";
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string JavaScript = "application/javascript";
        public const string Zip = "application/zip";

        public const string Text = "text/plain";
        public const string HTML = "text/html";
        public const string CSS = "text/css";


        public const string PNG = "image/png";
        public const string JPEG = "image/jpeg";
        public const string GIF = "image/gif";
        public const string Icon = "image/x-icon";



        public static string FromFileExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                default:
                case ".log":
                    return Text;
                case ".zip":
                    return Zip;
                case ".js":
                    return JavaScript;
                case ".css":
                    return CSS;
                case ".json":
                    return JSON;
                case ".html":
                    return HTML;
                case ".png":
                    return PNG;
                case ".jpg":
                case ".jpeg":
                    return JPEG;
                case ".gif":
                    return GIF;
                case ".ico":
                    return Icon;
            }
        }

    }

    static class HttpExtensions
    {
        private static int CHUNK_SIZE = 2048;

        public static Multipart ReadMultipart(this HttpListenerRequest request)
        {
            return new Multipart(request.InputStream, request.ContentEncoding);
        }

        /// <summary>
        /// Reads a file and writes it to the response.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="path"></param>
        public static void WriteFile(this HttpListenerResponse response, string path)
        {
            if (!File.Exists(path))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.WriteText($"The file '{path}' could not be found.");
                return;
            }

            //Get exetension
            var ext = Path.GetExtension(path);
            response.ContentType = ContentType.FromFileExtension(ext);

            //Get the data and write it
            var buff = File.ReadAllBytes(path);
            response.WriteContent(buff);
        }
 
        /// <summary>
        /// Reads a file with the specified FileShare and writes it to the response.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="path"></param>
        /// <param name="share"></param>
        public static void WriteFile(this HttpListenerResponse response, string path, FileShare share)
        {
            //Make sure file exists
            if (!File.Exists(path))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.WriteText($"The file '{path}' could not be found. Are you sure this is the correct url?");
                return;
            }

            //Get exetension
            var ext = Path.GetExtension(path);
            response.ContentType = ContentType.FromFileExtension(ext);
            
            //Read the file stream and write it
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, share))
                response.WriteStream(stream);
        }

        [System.Obsolete("Chunking is only effective if asyncronous")]
        public static void WriteFileChunked(this HttpListenerResponse response, string path)
        {
            if (!File.Exists(path))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.WriteText($"The file '{path}' could not be found. Are you sure this is the correct url?");
                return;
            }

            //Get exetension
            var ext = Path.GetExtension(path);
            response.ContentType = ContentType.FromFileExtension(ext);
            response.ContentEncoding = Encoding.UTF8;

            //Send in chunks
            response.SendChunked = true;

            //Prepare read values
            int bytesRead;
            byte[] chunk = new byte[CHUNK_SIZE];

            //Do the reading
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        using (Stream outputStream = response.OutputStream)
                        {
                            while ((bytesRead = reader.Read(chunk, 0, chunk.Length)) > 0)
                                outputStream.Write(chunk, 0, bytesRead);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                response.Abort();
            }
        }

        /// <summary>
        /// Writes text to the response with UTF8 encoding.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="content"></param>
        /// <param name="contentType"></param>
        public static void WriteText(this HttpListenerResponse response, string content, string contentType = "text/plain") => response.WriteText(content, Encoding.UTF8, contentType);

        /// <summary>
        /// Writes text to the response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="contentType"></param>
        public static void WriteText(this HttpListenerResponse response, string content, Encoding encoding, string contentType = "text/plain")
        {
            response.ContentType = contentType;
            response.ContentEncoding = encoding;
            byte[] buff = encoding.GetBytes(content);
            response.WriteContent(buff);
        }
        
        /// <summary>
        /// Serializes the object into JSON formatting and writes it to the response.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="obj"></param>
        public static void WriteJson(this HttpListenerResponse response, object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            response.WriteText(json, Encoding.UTF8, ContentType.JSON);
        }

        /// <summary>
        /// Reads the stream into a buffer and sends the buffer to the response.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="stream"></param>
        public static void WriteStream(this HttpListenerResponse response, Stream stream)
        {
            //Prepare read values
            int bytesRead;
            byte[] chunk = new byte[CHUNK_SIZE];

            //Do the reading
            using (MemoryStream memory = new MemoryStream())
            {
                //Read all the content from the stream
                while ((bytesRead = stream.Read(chunk, 0, chunk.Length)) > 0)
                    memory.Write(chunk, 0, bytesRead);

                //Write the data to the output
                response.WriteContent(memory.ToArray());
            }
        }

        /// <summary>
        /// Strips HTML tags
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string StripTags(this string str) => str.Replace("<", "&lt;").Replace(">", "&gt;");
        
    }
}
