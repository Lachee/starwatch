using Newtonsoft.Json;
using Starwatch.API.Gateway.Log.Models;
using Starwatch.API.Gateway.Log.Payload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Starwatch.API.Gateway
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class OldConnection : GatewayConnection
    {
        public override string ToString() => $"GatewayLogs({Identifier})";

        /// <summary>
        /// The current sequence of the connection
        /// </summary>
        [JsonProperty]
        public ushort Sequence { get; private set; }

        /// <summary>
        /// Does the current connection require a catchup?
        /// </summary>
        [JsonProperty]
        public bool NeedCatchup { get; private set; }

        /// <summary>
        /// The client that this connection was established with.
        /// </summary>
        [JsonProperty]
        public string Agent { get; private set; }

        /// <summary>Is the connection ready to send events</summary>
        [JsonProperty]
        public bool IsReady { get; private set; }

        /// <summary>
        /// The current event filter
        /// </summary>
        [JsonProperty]
        public Filter Filter { get; private set; }

        public OldConnection()
        {
            Sequence = 0;
            IsReady = false;
            Filter = new Filter();
        }

        protected override async void OnMessage(MessageEventArgs msg)
        {
            //Make sure we are not terminated
            if (HasTerminated)
            {
                Logger.Log("Received a message from a terminated connection.");
                return;
            }

            //Validate the authentication
            if (Authentication == null && !ValidateAuthentication())
                return;

            //Make sure its binary
            if (!msg.IsBinary)
            {
                Logger.Log("Tried to get data but it wasnt sent in binary");
                Terminate(CloseStatusCode.UnsupportedData, "Binary only");
                return;
            }

            try
            {
                Frame frame = new Frame();
                if (!frame.Read(msg.RawData))
                {
                    Terminate(CloseStatusCode.ProtocolError, "Invalid version");
                    return;
                }

                //Make sure we are within sequence
                if (!ValidateSequence(frame.Sequence))
                {
                    Terminate(CloseStatusCode.InvalidData, "Invalid sequence");
                    return;
                }

                switch (frame.OpCode)
                {
                    default:
                        Logger.Log("Unkown Opcode: {0}", frame.OpCode);
                        break;

                    case OpCode.Hello:
                        Logger.Log("Hello Received!");

                        //Make sure hello is valid.
                        Hello hello = JsonConvert.DeserializeObject<Hello>(frame.Content);
                        if (string.IsNullOrWhiteSpace(hello.Agent))
                        {
                            Terminate(CloseStatusCode.ProtocolError, "Invalid Agent");
                            return;
                        }

                        //Update our settings
                        Agent = hello.Agent;
                        IsReady = true;

                        //Return hello
                        await SendPayload(new Log.Payload.Welcome() { Connection = ConnectionID, ID = Identifier, Agent = Agent });
                        break;

                    case OpCode.Heartbeat:
                        Logger.Log("Heartbeat Payload.");
                        break;

                    case OpCode.Close:
                        Terminate(CloseStatusCode.Away, "Client requested close");
                        break;

                    case OpCode.Filter:


                        /*
                            * ENBL = Enable all events
                            * DISB = Disable all events
                            * BOTS = Enable bottable events (server and logs)
                            * 
                            * PLYR = Toggle player events
                            * SERV = Toggle server events
                            * LOGS = Toggle log events
                            * 
                            * SYNC = Refresh all players
                        */

                        if (!IsReady)
                        {
                            Terminate(CloseStatusCode.ProtocolError, "Connection not ready");
                            return;
                        }

                        Logger.Log("Filter Payload: " + frame.Identifier);
                        switch (frame.Identifier)
                        {
                            default:
                                Terminate(CloseStatusCode.ProtocolError, "Invalid identifier");
                                break;

                            case "ENBL":
                                Filter.LogEvents = Filter.ServerEvents = Filter.PlayerEvents = true;
                                break;

                            case "DISB":
                                Filter.LogEvents = Filter.ServerEvents = Filter.PlayerEvents = false;
                                break;

                            case "BOTS":
                                Filter.ServerEvents = Filter.PlayerEvents = true;
                                break;

                            case "PLYR":
                                Filter.PlayerEvents = !Filter.PlayerEvents;
                                break;

                            case "SERV":
                                Filter.ServerEvents = !Filter.ServerEvents;
                                break;

                            case "LOGS":
                                Filter.LogEvents = !Filter.LogEvents;
                                break;

                            case "SYNC":
                                await SynchronizePlayers();
                                break;

                        }
                        await SendPayload(new FilterAck() { Summary = this.Filter });
                        break;

                    case OpCode.FilterAck:
                    case OpCode.HeartbeatAck:
                    case OpCode.Welcome:
                    case OpCode.LogEvent:
                    case OpCode.PlayerEvent:
                    case OpCode.ServerEvent:
                        Logger.Log("Invalid Opcode: {0}", frame.OpCode);
                        Terminate(CloseStatusCode.UnsupportedData, "server opcode from client!");
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("Exception occured while recieving a message from " + ConnectionID + ": " + e.Message);
                Logger.Log("-- Message Dump --");
                Logger.Log(msg.Data);
                Logger.Log(Convert.ToBase64String(msg.RawData));
                Logger.Log("------------------");

                Terminate(CloseStatusCode.ServerError, e.Message);
            }
        }


        /// <summary>
        /// Sends every player currently connected (individually) as SYNC events.
        /// </summary>
        /// <returns></returns>
        public async Task SynchronizePlayers()
        {
            await SendPayload(new PlayerSyncEvent()
            {
                Players = API.Starwatch.Server.Connections.GetPlayers()
            });
        }

        /// <summary>
        /// Sends a payload
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<bool> SendPayload(IPayload payload)
        {
            try
            {
                //Serialize the payload into json and a frame.
                string json = JsonConvert.SerializeObject(payload.Data);
                Frame frame = new Frame()
                {
                    OpCode = payload.OpCode,
                    Identifier = payload.Identifier,
                    Sequence = NextSequence(),
                    Content = json
                };

                //Convert the frame into a byte array
                byte[] data;
                using (MemoryStream stream = new MemoryStream(2048))
                {
                    await frame.WriteAsync(stream);
                    data = stream.ToArray();
                }

                //Just send the data, we dont care to wait for the result.
                _ = Task.Run(() => Send(data));
                return true;
            }
            catch (Exception e)
            {
                Logger.Log("Error occured with {0} while trying to send payload {1}: {2}", this, payload, e.Message);
                Terminate(CloseStatusCode.ServerError, "Error while sending payload.");
                return false;
            }
        }

        /// <summary>
        /// Gets the next sequence to send
        /// </summary>
        /// <returns></returns>
        private ushort NextSequence()
        {
            if (Sequence >= ushort.MaxValue - 1)
                Sequence = ushort.MinValue;

            return ++Sequence;
        }

        /// <summary>
        /// Validates the sequence, disconnect if its too slow or fast.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        protected bool ValidateSequence(ushort sequence)
        {
            int maxdiff = 25;

            //Calculate the actual difference, looping if nessary
            int realseq = sequence;
            if (realseq > Sequence) realseq = sequence - (ushort.MaxValue - 1);
            int difference = Sequence - sequence;

            //The difference is to great and we need to disconnect them
            if (difference >= maxdiff)
            {
                Logger.Log("Heartbeat sequence was smaller than expected for auth '{0}'. Was expecting '{1}, but got '{2}'", Identifier, Sequence, sequence);
                return false;
            }

            //Do we need catchup?
            if (difference >= (maxdiff / 2))
            {
                Logger.Log("{0} is running to slow and is {1} sequences behind!", this, difference);
                NeedCatchup = true;
            }

            //Have we finished catching up?
            if (sequence == Sequence && NeedCatchup)
            {
                NeedCatchup = false;
                Logger.Log("{0} has caught up to sequences.", Identifier);
            }

            return true;
        }

    }
}
