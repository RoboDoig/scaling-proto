using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Net;

public class ClientNetwork : MonoBehaviour
{
    private UnityClient client;
    public static ClientNetwork Instance;
    public Dictionary<ushort, NetworkPlayer> networkPlayers = new Dictionary<ushort, NetworkPlayer>();

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    void Start() {
        client = GetComponent<UnityClient>();
        client.MessageReceived += MessageReceived;
    }

    public void JoinServer(bool asHost) {
        client.Host = "127.0.0.1";
        client.Port = 4296;
        client.Connect(client.Host, client.Port, true);

        // Give the server the player name
        using (DarkRiftWriter nameWriter = DarkRiftWriter.Create()) {
            nameWriter.Write(client.ID);
            nameWriter.Write(UIManager.Instance.playerNameInput.text);
            nameWriter.Write(asHost);

            using (Message newNameMessage = Message.Create(Tags.PlayerInfoTag, nameWriter)) {
                client.SendMessage(newNameMessage, SendMode.Reliable);
            }
        }

        UIManager.Instance.SetButtonsInteractable(false);
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage() as Message) {
            if (message.Tag == Tags.PlayerConnectTag) {
                PlayerConnect(sender, e);
            } else if (message.Tag == Tags.PlayerInfoTag) {
                PlayerInfo(sender, e);
            } else if (message.Tag == Tags.PlayerDisconnectTag) {
                PlayerDisconnect(sender, e);
            }
        }
    }

    void PlayerConnect(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                NetworkPlayer networkPlayer = reader.ReadSerializable<NetworkPlayer>();

                networkPlayers.Add(networkPlayer.id, networkPlayer);
            }
        }

        UIManager.Instance.PopulateConnectedPlayers(networkPlayers);
    }

    void PlayerDisconnect(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                ushort playerID = reader.ReadUInt16();
                networkPlayers.Remove(playerID);
            }
        }

        UIManager.Instance.PopulateConnectedPlayers(networkPlayers);
    }

    void PlayerInfo(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                NetworkPlayer networkPlayer = reader.ReadSerializable<NetworkPlayer>();

                networkPlayers[networkPlayer.id].playerName = networkPlayer.playerName;
                networkPlayers[networkPlayer.id].isHost = networkPlayer.isHost;
            }
        }

        UIManager.Instance.PopulateConnectedPlayers(networkPlayers);
    }
}
