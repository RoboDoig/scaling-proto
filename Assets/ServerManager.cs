using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance;
    private XmlUnityServer xmlServer;
    private DarkRiftServer server;

    Dictionary<IClient, NetworkPlayer> networkPlayers = new Dictionary<IClient, NetworkPlayer>();

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    void Start() {
    }

    public void StartServer() {
        xmlServer = GetComponent<XmlUnityServer>();
        xmlServer.Create();
        server = xmlServer.Server;

        server.ClientManager.ClientConnected += OnClientConnected;
        server.ClientManager.ClientDisconnected += OnClientDisconnected;       

        // If we host the server, we need to join as client as well
        ClientNetwork.Instance.JoinServer(true);
        UIManager.Instance.SetButtonsInteractable(false);
    }

    void OnClientConnected(object sender, ClientConnectedEventArgs e) {
        // Create a new network player
        System.Random r = new System.Random();
        NetworkPlayer newPlayer = new NetworkPlayer(
            e.Client.ID,
            "default",
            false,
            (float)r.NextDouble() * 10f,
            1f,
            (float)r.NextDouble() * 10f,
            (byte)r.Next(0, 200),
            (byte)r.Next(0, 200),
            (byte)r.Next(0, 200)
        );

        // Write player data and tell other connected clients about this client player
        using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create()) {
            newPlayerWriter.Write(newPlayer);

            using (Message newPlayerMessage = Message.Create(Tags.PlayerConnectTag, newPlayerWriter)) {
                foreach (IClient client in server.ClientManager.GetAllClients().Where(x => x != e.Client)) {
                    client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }

        // Add new player to player dict
        networkPlayers.Add(e.Client, newPlayer);

        // Tell client about all connected players
        foreach (NetworkPlayer player in networkPlayers.Values) {
            using (Message playerMessage = Message.Create(Tags.PlayerConnectTag, player)) {
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
            }
        }

        e.Client.MessageReceived += OnMessage;
    }

    void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
        networkPlayers.Remove(e.Client);

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(e.Client.ID);

            using (Message message = Message.Create(Tags.PlayerDisconnectTag, writer))
            {
                foreach (IClient client in server.ClientManager.GetAllClients())
                {
                    client.SendMessage(message, SendMode.Reliable);
                }
            }
        }

        e.Client.MessageReceived -= OnMessage;
    }

    void OnDestroy() {
        server.ClientManager.ClientConnected -= OnClientConnected;
        server.ClientManager.ClientDisconnected -= OnClientDisconnected;
    }

    void OnMessage(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage() as Message) {
            using (DarkRiftReader reader = message.GetReader()) {
                if (message.Tag == Tags.PlayerInfoTag) {
                    ushort id = reader.ReadUInt16();
                    string playerName = reader.ReadString();
                    bool isHost = reader.ReadBoolean();

                    networkPlayers[server.ClientManager.GetClient(id)].playerName = playerName;
                    networkPlayers[server.ClientManager.GetClient(id)].isHost = isHost;
                }
            }
        }

        // Send player info back to all clients
        foreach (NetworkPlayer player in networkPlayers.Values) {
            using (Message playerMessage = Message.Create(Tags.PlayerInfoTag, player)) {
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
            }
        }

        UIManager.Instance.PopulateConnectedPlayers(networkPlayers);
    }
}
