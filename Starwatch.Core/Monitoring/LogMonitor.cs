using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;

namespace Starwatch.Monitoring
{
    class LogMonitor : ConfigurableMonitor
    {
        private bool _logChat, _logInfo, _logWarning, _logError;

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

		public override Task<bool> HandleMessage(Message msg)
        {
            //return Task.FromResult(false);
            switch (msg.Level)
            {
                case Message.LogLevel.Chat:
                    if (_logChat) Logger.Log("<{0}> {1}", msg.Author, msg.Content);
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
			return Task.FromResult(false);
		}
    }
}
