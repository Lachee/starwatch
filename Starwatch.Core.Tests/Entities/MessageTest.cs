using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.Entities;
using System.Diagnostics;

namespace Starwatch.Core.Tests.Entities
{
    [TestClass]
    public class MessageTest
    {
        [TestMethod]
        public void TestLumiMessage ()
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

            foreach (string msg in lumiMessages)
            {
                Message m = Message.Parse(null, msg);
                Console.WriteLine(
                    $"Message Original: \"{msg}\"\n" +
                    $"-- .Content       \"{m.Content}\"\n" +
                    $"-- .Author        \"{m.Author}\"\n" +
                    $"-- .IsChat        \"{m.IsChat}\"\n" +
                    $"-- .Level         \"{m.Level}\""
                );
            }
        }
    }
}
