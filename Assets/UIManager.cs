using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager singleton;

    public InputField nameInputField;
    public Button startSessionButton;
    public Button localTestButton;
    public VerticalLayoutGroup connectedPlayersGroup;
    public GameObject connectedPlayerIndicatorPrefab;
    public Button readyButton;

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
            obj.transform.SetParent(connectedPlayersGroup.transform);
            obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName;
        }
    }

    public void ClearConnectedPlayers() {
        foreach (Transform child in connectedPlayersGroup.transform) {
            Destroy(child.gameObject);
        }
    }
}
