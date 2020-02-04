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
using Starwatch.Starbound;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

namespace Starwatch.Monitoring
{
    class UdpMonitor : Monitor
    {
        private Timer _checkTimer;
        public UdpMonitor(Server server) : base(server, "UdpMonitor") { }

        public override Task Initialize()
        {
            //Make sure we are not in debug mode
            //if (Server.SamplePath != null)
            //{
            //    Logger.Log("Cannot run  UDP checks because a sample set is being used.");
            //    return Task.CompletedTask;
            //}

            //Setup the automated timer.
            _checkTimer = new Timer(60 * 1000) { AutoReset = true };
            _checkTimer.Elapsed += async (sender, evnt) =>
            {
                //Skip null settings
                if (Server.Configurator == null)
                    return;

                //Skip the server that isnt running
                if (!Server.IsRunning)
                    return;

                //Make sure we are runnign the query server
                if (Server.Configurator.QueryServerPort == 0)
                {
                    Logger.LogWarning("Cannot run UDP checks because the server has disabled its query server.");
                    _checkTimer.Enabled = false;
                    return;
                }

                //Prepare the address 
                string address = Server.Configurator.QueryServerBind.Trim();
                if (string.IsNullOrWhiteSpace(address) || address.Equals("*") || address.Equals("localhost")) address = "127.0.0.1";

                //Check the UDP with the given address and port.
                if (!await CheckServerUDP(address, Server.Configurator.QueryServerPort))
                {
                    Logger.Log("Server requires UDP restart!");
                    Server.ApiHandler.BroadcastRoute(new API.Rest.Route.ServerStatisticsRoute(), "OnUdpCrash");
                    await Server.Terminate("UDP Failed to respond");
                }
            };
            _checkTimer.Start();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            if (_checkTimer != null)
            {
                _checkTimer.Dispose();
                _checkTimer = null;
            }
        }

        private async Task<bool> CheckServerUDP(string address, int port)
        {
            //Create the UDP client and set its timeouts
            UdpClient udpClient = new UdpClient();
            udpClient.Client.SendTimeout = 1000;
            udpClient.Client.ReceiveTimeout = 1000;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

            //We need to catch any exceptions that might be thrown
            try
            {
                //Prepare the endpoint
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(address), port);

                //Connect to the server
                Logger.Log("Connecting to server {0}:{1}", address, port);
                udpClient.Connect(endPoint);

                //This is the persumed handshake starbound expects
                byte[] handshake = new byte[]
                {
                    255, 255, 255, 255,
                    84, 83, 111, 117,
                    114, 99, 101, 32,
                    69, 110, 103, 105,
                    110, 101, 32, 81,
                    117, 101, 114, 121, 0
                };

                //Send it away
                Logger.Log("Sending Handshake...");
                await udpClient.SendAsync(handshake, handshake.Length);
                await Task.Delay(150);

                //Wait for a response from the same endpoint
                Logger.Log("Waiting for response...");
                var result = await udpClient.ReceiveAsync();

                //We where successfull!
                Logger.Log("Successfull handshake performed! Recieved {0} bytes", result.Buffer.Length);
                return true;
            }
            catch (Exception e)
            {
                //A error has occured while checking the server
                Logger.LogWarning("An exception has occured while trying to check the server: " + e.Message);
                return false;
            }
            finally
            {
                //Finally close the client, disposing of its uselesness
                udpClient.Close();
            }
        }
    }
}
