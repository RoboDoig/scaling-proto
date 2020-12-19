using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using Amazon.GameLift;

public class ClientNetworkManager : MonoBehaviour
{
    [SerializeField]
    UnityClient client;
    AmazonGameLiftClient gameLiftClient;

    public Dictionary<ushort, SimpleNetworkEntity> networkPlayers = new Dictionary<ushort, SimpleNetworkEntity>();

    void Awake() {
        gameLiftClient = new AmazonGameLiftClient();
        client.MessageReceived += MessageReceived;
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e) {
        Debug.Log("MessageReceived");
        using (Message message = e.GetMessage() as Message) {
            if (message.Tag == Tags.PlayerConnectTag) {
                PlayerConnect(sender, e);
            } else if (message.Tag == Tags.PlayerDisconnectTag) {
                PlayerDisconnect(sender, e);
            }
        }

        ClientUIManager.singleton.PopulateConnectedPlayers(networkPlayers);
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
}
