using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Starbound.Rcon
{
    /// <summary>
    /// Response from the RCON client.
    /// </summary>
    public struct RconResponse
    {
        public string Command { get; set; }

        /// <summary>
        /// The response from the rcon command (or the error message if <see cref="Success"/> is  false)
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Did the rcon command succeed and get sent through?
        /// </summary>
        public bool Success { get; set; }
    }
}
