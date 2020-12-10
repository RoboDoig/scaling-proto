using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;

public class UIManager : MonoBehaviour
{
    public InputField playerNameInput;
    public InputField gameNameInput;
    public InputField serverIPInput;
    public InputField portInput;
    public Button hostButton;
    public Button joinButton;
    public GameObject connectedPlayersPanel;
    public GameObject connectedPlayerIndicatorPrefab;

    public static UIManager Instance;

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PopulateConnectedPlayers(Dictionary<ushort, NetworkPlayer> connectedPlayers) {
        ClearConnectedPlayers();
        foreach (KeyValuePair<ushort, NetworkPlayer> connectedPlayer in connectedPlayers) {
            GameObject obj = Instantiate(connectedPlayerIndicatorPrefab);
            obj.transform.SetParent(connectedPlayersPanel.transform);
            if (connectedPlayer.Value.isHost) {
                obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName + " (host)";
            } else {
                obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName;
            }
        }
    }

    public void PopulateConnectedPlayers(Dictionary<IClient, NetworkPlayer> connectedPlayers) {
        ClearConnectedPlayers();
        foreach (KeyValuePair<IClient, NetworkPlayer> connectedPlayer in connectedPlayers) {
            GameObject obj = Instantiate(connectedPlayerIndicatorPrefab);
            obj.transform.SetParent(connectedPlayersPanel.transform);
            if (connectedPlayer.Value.isHost) {
                obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName + " (host)";
            } else {
                obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName;
            }
        }
    }

    public void ClearConnectedPlayers() {
        foreach (Transform child in connectedPlayersPanel.transform) {
            Destroy(child.gameObject);
        }
    }

    public void SetButtonsInteractable(bool interactable) {
        hostButton.interactable = interactable;
        joinButton.interactable = interactable;
    }
}
