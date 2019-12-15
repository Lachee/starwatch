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
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    class LogMonitor : ConfigurableMonitor
    {
        private bool _logChat, _logInfo, _logWarning, _logError;

        public override int Priority => 10;

        public LogMonitor(Server server) : base(server, "Game")
        {
            Logger.Colourise = true;
            string logLevel = Configuration.GetObject("level", "CIWE");
            Configuration.Save();

            _logChat = logLevel.Contains("C");
            _logInfo = logLevel.Contains("I");
            _logWarning = logLevel.Contains("W");
            _logError = logLevel.Contains("E");

            server.Connections.OnPlayerConnect += (player) =>
            {
                Logger.Log("A player has connected!");
            };

            server.Connections.OnPlayerUpdate += (player) =>
            {
                Logger.Log("A player has updated!");
                Logger.Log("UUID: " + player.UUID);
                Logger.Log("Location: " + player.Location);
            };

            server.Connections.OnPlayerDisconnect += (player, reason) =>
            {
                Logger.Log("A player has disconnected because " + reason);
            };
        }

		public override async Task<bool> HandleMessage(Message msg)
        {
            //return Task.FromResult(false);
            switch (msg.Level)
            {
                case Message.LogLevel.Chat:
                    if (_logChat) Logger.Log("<{0}> {1}", msg.Author, msg.Content);

                    //Save the chat log
                    var cl = new ChatLog(msg);
                    await cl.SaveAsync(Server.DbContext);

                    break;

                case Message.LogLevel.Info:
                    if (_logInfo) Logger.Log(msg.Content);
                    break;

                case Message.LogLevel.Warning:
                    if (_logWarning) Logger.LogWarning(msg.Content);
                    break;

                case Message.LogLevel.Error:
                    if (_logError) Logger.LogError(msg.Content);
                    break;

            }

            return false;
		}
    }
}
