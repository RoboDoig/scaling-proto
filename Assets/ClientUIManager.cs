using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientUIManager : MonoBehaviour
{
    public GameObject connectedPlayersPanel;
    public GameObject connectedPlayerIndicatorPrefab;

    public static ClientUIManager singleton;

    void Awake() {
        if (singleton != null) {
            Destroy(gameObject);
            return;
        }

        singleton = this;
    }

    public void PopulateConnectedPlayers(Dictionary<ushort, SimpleNetworkEntity> connectedPlayers) {
        ClearConnectedPlayers();
        foreach (KeyValuePair<ushort, SimpleNetworkEntity> connectedPlayer in connectedPlayers) {
            GameObject obj = Instantiate(connectedPlayerIndicatorPrefab);
            obj.transform.SetParent(connectedPlayersPanel.transform);
            obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName;
        }

    }

    public void ClearConnectedPlayers() {
        foreach (Transform child in connectedPlayersPanel.transform) {
            Destroy(child.gameObject);
        }
    }
}
