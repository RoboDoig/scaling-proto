using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager singleton;
    private UnityClient drClient;

    public Dictionary<ushort, NetworkEntity> networkPlayers = new Dictionary<ushort, NetworkEntity>();

    // Player prefabs
    public GameObject localPlayerPrefab;
    public GameObject networkPlayerPrefab;

    void Awake() {
        if (singleton != null) {
            Destroy(gameObject);
            return;
        }

        singleton = this;

        drClient = GetComponent<UnityClient>();
        drClient.MessageReceived += MessageReceived;
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage() as Message) {
            if (message.Tag == Tags.PlayerConnectTag) {
                PlayerConnect(sender, e);
            } else if (message.Tag == Tags.PlayerDisconnectTag) {
                PlayerDisconnect(sender, e);
            } else if (message.Tag == Tags.PlayerMoveTag) {
                PlayerMove(sender, e);
            }else if (message.Tag == Tags.PlayerInformationTag) {
                PlayerInformation(sender, e);
            } else if (message.Tag == Tags.StartGameTag) {
                StartGame(sender, e);
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

                Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                Color32 color = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 255);

                // Player / Network Player Spawn
                GameObject obj;
                if (ID == drClient.ID) {
                    // If this ID corresponds to this client, spawn the controllable player prefab
                    obj = Instantiate(localPlayerPrefab, position, Quaternion.identity) as GameObject;
                } else {
                    // Else we spawn a network prefab, non-controllable
                    obj = Instantiate(networkPlayerPrefab, position, Quaternion.identity) as GameObject;
                }

                // Set the color
                Renderer renderer = obj.GetComponent<MeshRenderer>();
                renderer.material.color = color;

                // Get network entity data of prefab and add to network players store
                networkPlayers.Add(ID, obj.GetComponent<NetworkEntity>());

                // Update player name
                networkPlayers[ID].SetPlayerName(playerName);
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

    void PlayerMove(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                PlayerMoveMessage playerMoveMessage = reader.ReadSerializable<PlayerMoveMessage>();

                networkPlayers[playerMoveMessage.ID].transform.position = playerMoveMessage.position;
            }
        }
    }

    void PlayerInformation(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                PlayerInformationMessage playerInformationMessage = reader.ReadSerializable<PlayerInformationMessage>();

                networkPlayers[playerInformationMessage.id].SetPlayerName(playerInformationMessage.playerName);
            }
        }
    }

    void StartGame(object sender, MessageReceivedEventArgs e) {
        UIManager.singleton.CloseUI();

        // Set the local player to be controllable
        foreach (KeyValuePair<ushort, NetworkEntity> networkPlayer in networkPlayers) {
            Player player = networkPlayer.Value.GetComponent<Player>();
            if (player != null) {
                player.controllable = true;
            }
        }
    }
    
    // Messages - maybe there should be a separate namespace for these so that both server and client can use them
    // Message for updating player information
    private class PlayerInformationMessage : IDarkRiftSerializable {
        public ushort id {get; set;}
        public string playerName {get; set;}

        public PlayerInformationMessage() {

        }

        public PlayerInformationMessage(ushort _id, string _playerName) {
            id = _id;
            playerName = _playerName;
        }

        public void Deserialize(DeserializeEvent e) {
            id  = e.Reader.ReadUInt16();
            playerName = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e) {
            e.Writer.Write(playerName);
        }
    }

    public void SendPlayerInformationMessage(string playerName) {
        using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
            writer.Write(new PlayerInformationMessage(drClient.ID, playerName));
            using (Message message = Message.Create(Tags.PlayerInformationTag, writer)) {
                drClient.SendMessage(message, SendMode.Reliable);
            }
        }
    }

    // Message for telling the server a player is ready - don't think ID needs to be in constructor
    private class PlayerReadyMessage : IDarkRiftSerializable {
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

    // Message for updating movement
    private class PlayerMoveMessage : IDarkRiftSerializable {
        public ushort ID {get; set;}
        public Vector3 position {get; set;}

        public PlayerMoveMessage() {

        }

        public PlayerMoveMessage(Vector3 _postion) {
            position = _postion;
        }

        public void Deserialize(DeserializeEvent e) {
            ID = e.Reader.ReadUInt16();
            position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        }

        public void Serialize(SerializeEvent e) {
            e.Writer.Write(position.x);
            e.Writer.Write(position.y);
            e.Writer.Write(position.z);
        }
    }

    public void SendPlayerMoveMessage(Vector3 position) {
        using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
            writer.Write(new PlayerMoveMessage(position));
            using (Message message = Message.Create(Tags.PlayerMoveTag, writer)) {
                drClient.SendMessage(message, SendMode.Unreliable);
            }
        }
    }
}
