using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Starwatch.Util
{
    public class VPNValidator
    {
        private IPRange[] _ranges;
        public struct IPRange
        {
            public uint NetworkAddress;
            public byte CIDR;
        }

        /// <summary>
        /// Loads a series of IPv4 addresses to block
        /// </summary>
        /// <param name="filepath"></param>
        public void Load(string filepath)
        {
            string[] lines = File.ReadAllLines(filepath);
            List<IPRange> ranges = new List<IPRange>(lines.Length);
            foreach(var l in lines)
            {
                //Skip invalid lines. A IPv4 MUST have at least 7 characters: 0.0.0.0
                if (l.Length < 7 || string.IsNullOrWhiteSpace(l) || l.StartsWith("#"))
                    continue;

                //Parse the line
                ranges.Add(Parse(l.Trim()));
            }

            //Store the ranges into the list
            _ranges = ranges.ToArray();
        }

        /// <summary>
        /// Checks if the IP address is in any range
        /// </summary>
        /// <param name="address">The IPv4 address to check</param>
        /// <returns></returns>
        public bool Check(string address)
        {
            var range = Parse(address);
            for(int i = 0; i < _ranges.Length; i++)
            {
                uint mask = uint.MaxValue << (32 - _ranges[i].CIDR);
                if ((mask & range.NetworkAddress) == _ranges[i].NetworkAddress)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the range the IP address is in.
        /// </summary>
        /// <param name="address">The IPv4 address to check</param>
        /// <returns></returns>
        public IPRange GetRange(string address)
        {
            var range = Parse(address);
            for (int i = 0; i < _ranges.Length; i++)
            {
                uint mask = uint.MaxValue << (32 - _ranges[i].CIDR);
                if ((mask & range.NetworkAddress) == _ranges[i].NetworkAddress)
                    return _ranges[i];
            }

            return range;
        }

        /// <summary>
        /// Parses a IPv4 address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPRange Parse(string address)
        {
            var parts = address.Split('.');
            var ends = parts[parts.Length - 1].Split('/');
            
            uint bytes = 0;
            bytes |= uint.Parse(parts[0]) << 24;
            bytes |= uint.Parse(parts[1]) << 16;
            bytes |= uint.Parse(parts[2]) << 8;
            bytes |= uint.Parse(ends[0]);

            return new IPRange()
            {
                NetworkAddress = bytes,
                CIDR = ends.Length != 2 ? byte.MinValue : byte.Parse(ends[1])
            };
        }

        /// <summary>
        /// Checks if the address is a IPv4
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsIPv4(string address)
        {
            if (System.Net.IPAddress.TryParse(address, out var addr))
                return addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            return false;
        }

        /// <summary>
        /// Checks if the address is a IPv6
        /// </summary>
        public static bool IsIPv6(string address)
        {
            if (System.Net.IPAddress.TryParse(address, out var addr))
                return addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
            return false;
        }
    }
}
