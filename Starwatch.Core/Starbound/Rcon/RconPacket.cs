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
using System.IO;
using System.Threading.Tasks;

namespace Starwatch.Starbound.Rcon
{
    public class RconPacket
    {
        public int ID { get; set; }
        public string Body { get; set;  }
        public PacketType Type { get; set; }
        public enum PacketType
        {
            ServerdataAuth = 3,
            ServerdataAuthResponse = 2,
            ServerdataExecuteCommand = 2,
            ServerdataResponseValue = 0
        }

        public static async Task<RconPacket> FromStream(Stream stream, int count)
        {
            int id, type;
            string body;

            byte[] intbuff = new byte[sizeof(int)];
            byte[] stringbuff = new byte[count - 10];
            byte[] nullbuff = new byte[2];

            await stream.ReadAsync(intbuff, 0, intbuff.Length);
            id = BitConverter.ToInt32(intbuff, 0);

            await stream.ReadAsync(intbuff, 0, intbuff.Length);
            type = BitConverter.ToInt32(intbuff, 0);

            await stream.ReadAsync(stringbuff, 0, stringbuff.Length);
            body = System.Text.Encoding.ASCII.GetString(stringbuff);

            await stream.ReadAsync(nullbuff, 0, nullbuff.Length);
            return new RconPacket()
            {
                ID = id,
                Type = type == 2 ? PacketType.ServerdataAuthResponse : (PacketType)type,
                Body = body
            };
        }

        public async Task ToStream(Stream stream)
        {
            byte[] bodyBuffer = System.Text.Encoding.ASCII.GetBytes(Body);
            int size = bodyBuffer.Length + 10;

            await stream.WriteAsync(BitConverter.GetBytes(size), 0, sizeof(int));
            await stream.WriteAsync(BitConverter.GetBytes(ID), 0, sizeof(int));
            await stream.WriteAsync(BitConverter.GetBytes((int) Type), 0, sizeof(int));                
            await stream.WriteAsync(bodyBuffer, 0, bodyBuffer.Length);
            await stream.WriteAsync(new byte[2], 0, 2);
            
        }
    }
}
