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
        List<ConnectedPlayer> pfPlayers = new List<ConnectedPlayer>();

        public NetworkManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            GameserverSDK.Start();

            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;

            GameserverSDK.RegisterShutdownCallback(OnShutdown);
            //ReadyForPlayers();

            if (GameserverSDK.ReadyForPlayers())
            {
                // returns true on allocation call, player about to connect
            } else
            {
                // returns false when server is being terminated
            }
        }

        async void ReadyForPlayers()
        {
            await Task.Delay(1);
            GameserverSDK.ReadyForPlayers();
        }

        void OnShutdown()
        {
            Environment.Exit(1); // Is this the best way to do this?
        }

        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            // When client connects, generate new player data
            Player newPlayer = new Player(e.Client.ID, RandomString(6, false));
            players.Add(e.Client, newPlayer);

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

            // Tell PlayFab about this player
            pfPlayers.Add(new ConnectedPlayer(newPlayer.playerName));
            GameserverSDK.UpdateConnectedPlayers(pfPlayers);

            e.Client.MessageReceived += OnMessage;
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            players.Remove(e.Client);

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

