using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkManager : MonoBehaviour
{
    private UnityClient drClient;
    private UIManager uiManager;

    public Dictionary<ushort, SimpleNetworkEntity> networkPlayers = new Dictionary<ushort, SimpleNetworkEntity>();

    void Awake() {
        drClient = GetComponent<UnityClient>();
        drClient.MessageReceived += MessageReceived;
    }

    void Start() {
        uiManager = UIManager.singleton;
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage() as Message) {
            if (message.Tag == Tags.PlayerConnectTag) {
                PlayerConnect(sender, e);
            } else if (message.Tag == Tags.PlayerDisconnectTag) {
                PlayerDisconnect(sender, e);
            } else if (message.Tag == Tags.StartGameTag) {
                Debug.Log("START GAME!!!");
            }
        }

        // Update the UI with connected players
        UIManager.singleton.PopulateConnectedPlayers(networkPlayers);
    }

    void PlayerConnect(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                ushort ID = reader.ReadUInt16();
                string playerName = reader.ReadString();

                networkPlayers.Add(ID, new SimpleNetworkEntity(ID, playerName));
            }
        }
    }

    void PlayerDisconnect(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                networkPlayers.Remove(reader.ReadUInt16());
            }
        }
    }

    
    // Messages - maybe there should be a separate namespace for these so that both server and client can use them
    public class PlayerReadyMessage : IDarkRiftSerializable {
        public ushort id {get; set;}
        public bool isReady {get; set;}

        public PlayerReadyMessage() {

        }

        public PlayerReadyMessage(ushort _id, bool _isReady) {
            id = _id;
            isReady = _isReady;
        }

        public void Deserialize(DeserializeEvent e) {
            id = e.Reader.ReadUInt16();
            isReady = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e) {
            e.Writer.Write(id);
            e.Writer.Write(isReady);
        }
    }

    public void SendPlayerReadyMessage(bool isReady) {
        using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
            writer.Write(new PlayerReadyMessage(drClient.ID, isReady));
            using (Message message = Message.Create(Tags.PlayerSetReadyTag, writer)) {
                drClient.SendMessage(message, SendMode.Reliable);
            }
        }
    }
}
