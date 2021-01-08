using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DarkRift;
using DarkRift.Server;

using Microsoft.Playfab.Gaming.GSDK.CSharp;

namespace ScalingPlugins
{
    public class NetworkManager : Plugin
    {
        public override bool ThreadSafe => false;
        public override Version Version => new Version(1, 0, 0);
        Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();
        Dictionary<IClient, ConnectedPlayer> pfPlayers = new Dictionary<IClient, ConnectedPlayer>();
        DateTime startDateTime;
        bool sessionIdAssigned;

        public NetworkManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            GameserverSDK.Start();

            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;

            GameserverSDK.RegisterShutdownCallback(OnShutdown);
            GameserverSDK.RegisterHealthCallback(OnHealthCheck);
            sessionIdAssigned = false;

            if (GameserverSDK.ReadyForPlayers())
            {
                // returns true on allocation call, player about to connect
            } else
            {
                // returns false when server is being terminated
            }
        }

        void OnShutdown()
        {
            Environment.Exit(1); // Is this the best way to do this?
        }

        bool OnHealthCheck()
        {
            // How long has server been active in seconds?
            float awakeTime;

            if (!sessionIdAssigned)
            {
                awakeTime = 0f;
            } else
            {
                awakeTime = (float)(DateTime.Now - startDateTime).TotalSeconds;
            }

            // Get server info
            // If session ID has been assigned, server is active
            string sessionIdCheck = null;
            IDictionary<string, string> config = GameserverSDK.getConfigSettings();
            if (config.TryGetValue(GameserverSDK.SessionIdKey, out string sessionId))
            {
                sessionIdCheck = sessionId;
                
                // If this is the first session assignment, start the activated timer
                if (!sessionIdAssigned)
                {
                    startDateTime = DateTime.Now;
                    sessionIdAssigned = true;
                }
            }

            Console.WriteLine(sessionIdAssigned);
            Console.WriteLine(awakeTime);

            // If server has been awake for over 10 mins, and no players connected, and the PlayFab server is not in standby (no session id assigned): begin shutdown
            if (awakeTime > 600f && players.Count <= 0 && sessionIdAssigned)
            {
                OnShutdown();
                return false;
            }

            return true;
        }

        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            // When client connects, generate new player data
            Player newPlayer = new Player(e.Client.ID, RandomString(6, false));
            players.Add(e.Client, newPlayer);

            // Reset server clock
            startDateTime = DateTime.Now;

            // Write player data and tell other connected clients about this player
            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer);

                using (Message newPlayerMessage = Message.Create(Tags.PlayerConnectTag, newPlayerWriter))
                {
                    foreach (IClient client in ClientManager.GetAllClients().Where(x => x != e.Client))
                    {
                        client.SendMessage(newPlayerMessage, SendMode.Reliable);
                    }
                }
            }

            // Tell the client player about all connected players
            foreach (Player player in players.Values)
            {
                Message playerMessage = Message.Create(Tags.PlayerConnectTag, player);
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
            }

            // Tell PlayFab about this player - TODO this is repeated in client disconnected
            pfPlayers.Add(e.Client, new ConnectedPlayer(newPlayer.playerName));

            List<ConnectedPlayer> listPfPlayers = new List<ConnectedPlayer>();
            foreach (KeyValuePair<IClient, ConnectedPlayer> pfPlayer in pfPlayers)
            {
                listPfPlayers.Add(pfPlayer.Value);
            }
            GameserverSDK.UpdateConnectedPlayers(listPfPlayers);

            e.Client.MessageReceived += OnPlayerReadyMessage;
            e.Client.MessageReceived += OnPlayerInformationMessage;
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            players.Remove(e.Client);
            pfPlayers.Remove(e.Client);

            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(e.Client.ID);

                using (Message message = Message.Create(Tags.PlayerDisconnectTag, writer))
                {
                    foreach (IClient client in ClientManager.GetAllClients())
                    {
                        client.SendMessage(message, SendMode.Reliable);
                    }
                }
            }

            // Update playfabs player list - TODO this is repeated in client connected
            List<ConnectedPlayer> listPfPlayers = new List<ConnectedPlayer>();
            foreach (KeyValuePair<IClient, ConnectedPlayer> pfPlayer in pfPlayers)
            {
                listPfPlayers.Add(pfPlayer.Value);
            }

            GameserverSDK.UpdateConnectedPlayers(listPfPlayers);
        }

        // Basically the same as OnPlayerInformationMessage - TODO
        void OnPlayerReadyMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.PlayerSetReadyTag)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        ushort clientID = reader.ReadUInt16();
                        bool isReady = reader.ReadBoolean();

                        // Update player ready status and check if all players are ready
                        players[ClientManager.GetClient(clientID)].isReady = isReady;
                        CheckAllReady();

                    }
                }
            }
        }

        void OnPlayerInformationMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.PlayerInformationTag)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        ushort clientID = reader.ReadUInt16();
                        string playerName = reader.ReadString();

                        // Update player information
                        players[ClientManager.GetClient(clientID)].playerName = playerName;

                        // Update all players
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(clientID);
                            writer.Write(playerName);

                            message.Serialize(writer);
                        }

                        foreach(IClient client in ClientManager.GetAllClients())
                        {
                            client.SendMessage(message, e.SendMode);
                        }
                    }
                }
            }
        }

        void CheckAllReady()
        {
            // Check all clients, if any not ready, then return
            foreach (IClient client in ClientManager.GetAllClients())
            {
                if (!players[client].isReady)
                {
                    return;
                }
            }

            // If all are ready, broadcast start game to all clients
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                using (Message message = Message.Create(Tags.StartGameTag, writer))
                {
                    foreach (IClient client in ClientManager.GetAllClients())
                    {
                        client.SendMessage(message, SendMode.Reliable);
                    }
                }
            }
        }

        string RandomString(int size, bool lowerCase)
        {
            Random r = new Random();
            StringBuilder randString = new StringBuilder(size);

            int start = (lowerCase) ? 97 : 65;

            for (int i = 0; i < size; i++)
            {
                randString.Append((char)(26 * r.NextDouble() + start));
            }

            return randString.ToString();
        }

        void OnMessage(object sender, MessageReceivedEventArgs e)
        {

        }
    }
}

