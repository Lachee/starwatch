using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.API.Gateway.Models
{
    struct Frame
    {
        public byte Version => 4;
        public ushort Sequence { get; set; }
        public OpCode OpCode { get; set; }
        public string Identifier { get; set; }
        public string Content { get; set; }

        public bool Read(byte[] data)
        {
            int index = 0;

            //Version
            if (data[index] != this.Version) return false;
            index += 1;

            // == Sequence
            Sequence = BitConverter.ToUInt16(data, index);
            index += 2;

            // == Opcode
            OpCode = (OpCode)data[index];
            index += 1;

            // == Identifier
            Identifier = Encoding.ASCII.GetString(data, index, 4);
            index += 4;

            // == Content
            int length = BitConverter.ToInt32(data, index);
            index += 4;

            if (length > 0)
            {
                Content = Encoding.UTF8.GetString(data, index, data.Length - index);
                Content = Content.Trim();
            }

            return true;
        }

        public async Task<int> WriteAsync(Stream stream)
        {
            await stream.WriteAsync(new byte[1] { Version }, 0, 1);
            await stream.WriteAsync(BitConverter.GetBytes(Sequence), 0, 2);
            await stream.WriteAsync(new byte[] { (byte) OpCode }, 0, 1);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(Identifier), 0, 4);

            byte[] contentBytes = Encoding.UTF8.GetBytes(Content);
            await stream.WriteAsync(BitConverter.GetBytes(contentBytes.Length), 0, 4);
            await stream.WriteAsync(contentBytes, 0, contentBytes.Length);

            //Just 4 bytes padding to be safe
            await stream.WriteAsync(new byte[4], 0, 4);
            return contentBytes.Length + 16;
        }
    }
}
