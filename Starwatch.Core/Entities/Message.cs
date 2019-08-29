using Newtonsoft.Json;
using Starwatch.Starbound;
using System.Linq;
using System.Text.RegularExpressions;

namespace Starwatch.Entities
{
    public class Message
	{
        public static readonly Regex MESSAGE_REGEX = new Regex(@"\[(I|W|E)[nfoarn]+\]\s(Chat:\s<(.+)>\s)?(.*)", RegexOptions.Compiled);
        public static readonly Regex CHAT_REGEX = new Regex(@"(Chat:\s<(.+)>\s)?(.*)", RegexOptions.Compiled);

        public enum LogLevel
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Chat = 3
        }

        [JsonIgnore]
        public Server Server { get; }

        [JsonIgnore]
        public bool IsChat => Author != null;

        public string Content { get; private set; }
        public string Author { get; private set; }
        public LogLevel Level { get; private set; }
        
        protected Message(Server server)
        {
            Server = server;
        }

        public override string ToString() =>  $"[{Level}] "+ (IsChat ? $"<{Author}> " : " ") + $"{Content}";

        public static Message Parse(Server server, string line)
        {
            //Prepare the line
            var contents = line.Trim();

            //Invalid message, so send a error with its content
            if (contents.Length < 6)
            {
                return new Message(server)
                {
                    Content = line,
                    Level = LogLevel.Error,
                    Author = "UNKOWN"
                };
            }

            //Parse the level
            var level = LogLevel.Info;
            switch (contents[1])
            {
                default:
                case 'E':
                    level = LogLevel.Error;
                    contents = contents.Substring(8);
                    break;

                case 'W':
                    level = LogLevel.Warning;
                    contents = contents.Substring(7);
                    break;

                case 'I':
                    level = LogLevel.Info;
                    contents = contents.Substring(7);
                    break;
            }

            //If we are info, then we should get the message and author.
            if (level == LogLevel.Info && contents.StartsWith("Chat:"))
            {
                int indexNameStart = 7;
                int indexNameEnd = contents.IndexOf("> ");

                string chat = contents.Substring(indexNameEnd + 2);
                string author = contents.Substring(indexNameStart, indexNameEnd - indexNameStart);

                return new Message(server)
                {
                    Level = LogLevel.Chat,
                    Content = chat,
                    Author = author,
                };
            }
            else
            {
                //We are not a special chat condition, so just return what we have
                return new Message(server)
                {
                    Level = level,
                    Content = contents,
                    Author = null
                };
            }
        }

        /// <summary>
        /// Gets the session linked to the chat message.
        /// </summary>
        /// <returns></returns>
        public Session GetSession()
        {
            //Only chat messages have sessions
            if (!IsChat)
                return null;

            //Get the player linked with the author name. If it doesnt exist, return null.
            var player = Server.Connections.GetPlayersEnumerable().FirstOrDefault(p => p.Username.Equals(this.Author));
            if (player == null) return null;
            
            //Get the session linked with the account name.
            return Server.Connections.GetSession(player.Connection);
        }

        [System.Obsolete("Regex Based Parsing is now obsolete")]
        public static Message ParseRegex(Server server, string line)
        {
            var match = MESSAGE_REGEX.Match(line);
            if (!match.Success) return null;

            //Calculate the level
            var level = LogLevel.Info;
            switch(match.Groups[1].Value)
            {
                default:
                case "I":
                    level = LogLevel.Info;
                    break;

                case "W":
                    level = LogLevel.Warning;
                    break;

                case "E":
                    level = LogLevel.Error;
                    break;
            }

            //Check if its chat
            bool ischat = level == LogLevel.Info && match.Groups[3].Success;
            
            return new Message(server)
            {
                Level = ischat ? LogLevel.Chat : level,
                Content = match.Groups[4].Value,
                Author = ischat ? match.Groups[3].Value : null,
            };
        }
	}
    
}
