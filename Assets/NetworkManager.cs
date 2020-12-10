using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkManager : MonoBehaviour
{
    [SerializeField]
    UnityClient client;

    [SerializeField]
    [Tooltip("The controllable player prefab")]
    GameObject controllablePrefab;

    [SerializeField]
    [Tooltip("The network controllable player prefab")]
    GameObject networkPrefab;

    public Dictionary<ushort, NetworkEntity> networkPlayers = new Dictionary<ushort, NetworkEntity>();

    void Awake() {
        if (client == null) {
            Debug.LogError("Client unassigned in NetworkPlayerManager");
            Application.Quit();
        }

        if (controllablePrefab == null) {
            Debug.LogError("Player prefab unassigned in NetworkPlayerManager");
            Application.Quit();
        }

        if (networkPrefab == null) {
            Debug.LogError("Network player unassigned in NetworkPlayerManager");
            Application.Quit();
        }

        client.MessageReceived += MessageReceived;
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage() as Message) {
            if (message.Tag == Tags.SpawnPlayerTag) {
                SpawnPlayer(sender, e);
            } else if (message.Tag == Tags.DespawnPlayerTag) {
                DespawnPlayer(sender, e);
            } else if (message.Tag == Tags.MovePlayerTag) {
                MovePlayer(sender, e);
            }
        }
    }

    void SpawnPlayer(object sender, MessageReceivedEventArgs e) {
        Debug.Log("Spawn Player");
        using (Message message = e.GetMessage())
        using (DarkRiftReader reader = message.GetReader()) {
            PlayerMessage playerMessage = reader.ReadSerializable<PlayerMessage>();

            Vector3 position = new Vector3(playerMessage.x, playerMessage.y, playerMessage.z);
            Color32 color = new Color32(playerMessage.colorR, playerMessage.colorG, playerMessage.colorB, 255);
            GameObject obj;

            if (playerMessage.id == client.ID) {
                obj = Instantiate(controllablePrefab, position, Quaternion.identity);
            } else {
                obj = Instantiate(networkPrefab, position, Quaternion.identity);
            }

            obj.GetComponent<Renderer>().material.color = color;
            NetworkEntity networkEntity = obj.GetComponent<NetworkEntity>();
            networkEntity.networkID = playerMessage.id;
            networkPlayers.Add(playerMessage.id, obj.GetComponent<NetworkEntity>());
        }
    }

    void DespawnPlayer(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage())
        using (DarkRiftReader reader = message.GetReader()) {
            DestroyPlayer(reader.ReadUInt16());
        }
    }

    void DestroyPlayer(ushort id) {
        Destroy(networkPlayers[id].gameObject);
        networkPlayers.Remove(id);
    }

    void MovePlayer(object sender, MessageReceivedEventArgs e) {
        Debug.Log("MovePlayer");
    }

    public class PlayerMessage : IDarkRiftSerializable {
        public ushort id;
        public float x {get; set;}
        public float y {get; set;}
        public float z {get; set;}
        public byte colorR {get; set;}
        public byte colorG {get; set;}
        public byte colorB {get; set;}

        public void Deserialize(DeserializeEvent e) {
            id = e.Reader.ReadUInt16();
            x = e.Reader.ReadSingle();
            y = e.Reader.ReadSingle();
            z = e.Reader.ReadSingle();
            colorR = e.Reader.ReadByte();
            colorG = e.Reader.ReadByte();
            colorB = e.Reader.ReadByte();
        }

        public void Serialize(SerializeEvent e) {
            throw new System.NotImplementedException();
        }
    }
}
