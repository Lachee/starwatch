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
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Starwatch.Starbound.Rcon
{
    public class SourceRconClient
    {
        public delegate void RconEventHandler(object sender, RconResponse response);
        public event RconEventHandler OnExecuteSuccess;
        public event RconEventHandler OnExecuteFailure;

        public string Address { get; protected set; }
        public int Port { get; protected set; }
        private string _password;

        public SourceRconClient(string address, int port, string password = null)
        {
            Address = address;
            Port = port;
            _password = password;
        }        

        /// <summary>
        /// Executes a RCON command and listens for its response. Will fail safely with a invalid rcon response being returned if any errors occured.
        /// </summary>
        /// <param name="command">The rcon command to execute</param>
        /// <returns>Returns a rcon resposne.</returns>
        public virtual async Task<RconResponse> ExecuteAsync(string command)
        {
            try
            {
                int sequence = 10;
                using (var tcp = new TcpClient())
                {
                    //Connect to the TCP and prepare the stream
                    await tcp.ConnectAsync(Address, Port);
                    using (var stream = tcp.GetStream())
                    {
                        //Write the authentication packet
                        int authID = sequence++;
                        await new RconPacket()
                        {
                            ID = authID,
                            Type = RconPacket.PacketType.ServerdataAuth,
                            Body = _password
                        }.ToStream(stream);

                        //Make sure its all valid
                        bool receivedAuthPacket = false;
                        bool isAuthenticated = false;

                        //Prepare a buffer and read all the authorization packets.
                        byte[] sizebuff = new byte[sizeof(int)];
                        while (!receivedAuthPacket)
                        {
                            await stream.ReadAsync(sizebuff, 0, sizebuff.Length);
                            var authResponsePacket = await RconPacket.FromStream(stream, BitConverter.ToInt32(sizebuff, 0));
                            if (authResponsePacket.Type == RconPacket.PacketType.ServerdataAuthResponse)
                            {
                                receivedAuthPacket = true;
                                isAuthenticated = authResponsePacket.ID == authID;
                            }
                        }

                        //Make sure we are authorized
                        if (!isAuthenticated)
                        {
                            var res = new RconResponse()
                            {
                                Command = command,
                                Message = "Invalid Authorization",
                                Success = false
                            };

                            OnExecuteFailure?.Invoke(this, res);
                            return res;
                        }

                        //Write the command packet
                        await new RconPacket()
                        {
                            ID = sequence++,
                            Type = RconPacket.PacketType.ServerdataExecuteCommand,
                            Body = command
                        }.ToStream(stream);

                        //Read its response.
                        await stream.ReadAsync(sizebuff, 0, sizebuff.Length);
                        var responsePacket = await RconPacket.FromStream(stream, BitConverter.ToInt32(sizebuff, 0));
                        var response = new RconResponse()
                        {
                            Command = command,
                            Message = responsePacket.Body,
                            Success = true
                        };

                        OnExecuteSuccess?.Invoke(this, response);
                        return response;
                    }
                }
            }
            catch (Exception e)
            {
                var res = new RconResponse()
                {
                    Command = command,
                    Message = "Exception Occured: " + e.Message,
                    Success = false
                };

                OnExecuteFailure?.Invoke(this, res);
                return res;
            }
        }
    }
    

}
