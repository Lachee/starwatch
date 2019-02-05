using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
