using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Entities.Tests
{
    [TestClass]
    public class MessageTests
    {
        [TestMethod]
        public void ParseTest ()
        {
            MalformedMessageTest();
        }

        private class MessageComparison
        {
            public string Content { get; set; } = "";
            public string? Author { get; set; } = "";
            public bool IsChat { get; set; } = false;
            public string Level { get; set; } = "";

            public bool CompareToMessage (Message message)
            {
                if (message is null)
                {
                    return false;
                }

                if (!Content.Equals(message.Content))
                {
                    return false;
                }

                if (
                     (Author is null && message.Author is not null) ||
                     (Author is not null && message.Author is null)
                   )
                {
                    return false;
                }
                else if (Author is null && message.Author is null)
                {
                    return true;
                }
                
                if (Author is not null && !Author.Equals(message.Author))
                {
                    return false;
                }

                if (IsChat != message.IsChat)
                {
                    return false;
                }    

                if (!Level.Equals(message.Level.ToString()))
                {
                    return false;
                }

                return true;
            }
        }

        private void MalformedMessageTest ()
        {
            // This is an example of a group of messages sent which are confirmed
            // to cause Starwatch to crash the server.

            string[] lumiMessages = new string[]
            {
                "[Info] Chat: <^pink;Lumi^reset;> ^white;",
                "return math.factor(749)",
                "--------",
                "7 * 107"
            };

            Message[] parsed = new Message[4];
            MessageComparison[] expected = new MessageComparison[]
            {
                new MessageComparison
                {
                    Content = "^white;",
                    Author = "^pink;Lumi^reset;",
                    IsChat = true,
                    Level = "Chat"
                },

                new MessageComparison
                {
                    Content = "return math.factor(749)",
                    Author = null,
                    IsChat = false,
                    Level = "Error"
                },

                new MessageComparison
                {
                    Content = "--------",
                    Author = null,
                    IsChat = false,
                    Level = "Error"
                },

                new MessageComparison
                {
                    Content = "7 * 107",
                    Author = null,
                    IsChat = false,
                    Level = "Error"
                }
            };

            for (int i = 0; i < lumiMessages.Length; i++)
            {
                parsed[i] = Message.Parse(null, lumiMessages[i]);

                Assert.IsTrue(
                    expected[i].CompareToMessage(parsed[i]),
                    $@"
Message #{i+1} was not parsed expectedly:

Original:
{lumiMessages[i]}

Got:
  Content: '{parsed[i].Content}'
  Author: '{parsed[i].Author}'
  IsChat: {parsed[i].IsChat.ToString().ToLowerInvariant()}
  Level: '{parsed[i].Level}'

Expected:
  Content: '{expected[i].Content}'
  Author: '{expected[i].Author}'
  IsChat: {expected[i].IsChat.ToString().ToLowerInvariant()}
  Level: '{expected[i].Level}'"
                );
            }

            
        }
    }
}
