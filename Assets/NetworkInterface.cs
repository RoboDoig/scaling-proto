using System.Collections;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkInterface : MonoBehaviour
{
    public static NetworkInterface singleton;
    private UnityClient drClient;
    private UIManager uiManager;
    private NetworkManager networkManager;

    // PlayFab settings
    public string titleID; // The playfab title ID
    public string region; // The region where we will try to connect
    public string matchmakingQueue; // The name of the matchmaking queue we'll use
    public int matchmakingTimeout; // How long to attempt matchmaking before resetting
    public string playfabTCPPortName; // Playfab's name for the TCP port mapping
    public string playfabUDPPortName; // Playfab's name for the UDP port mapping

    void Awake() {
        if (singleton != null) {
            Destroy(gameObject);
            return;
        }

        singleton = this;
    }

    void Start() {
        // Cache components
        drClient = GetComponent<UnityClient>();
        uiManager = UIManager.singleton;
        networkManager = GetComponent<NetworkManager>();
    }

    // General Functions //
    public void SetPlayerReady() {
        // Tell the server this player is ready to start game
        networkManager.SendPlayerReadyMessage(true);

        // Update UI
        uiManager.SetLobbyInteractable(false);
    }

    // Connect with local test server //
    public void StartLocalSession() {
        // Connect to local network
        drClient.ConnectInBackground(IPAddress.Parse("127.0.0.1"), 7777, 7777, true, delegate {OnLocalSessionCallback();} );

        // Update UI
        uiManager.SetInputInteractable(false);
    }

    public void OnLocalSessionCallback() {
        if (drClient.ConnectionState == ConnectionState.Connected) {
            // If connection successful, send any additional player info
            networkManager.SendPlayerInformationMessage(uiManager.nameInputField.text);

            // Set lobby controls to interactable
            uiManager.SetLobbyInteractable(true);
        } else {
            // Else reset the input UI
            uiManager.SetInputInteractable(true);
        }
    }

    // PlayFab Connection //
    public void StartSession(string clientName) {
        // Attempt to login to PlayFab
        var request = new LoginWithCustomIDRequest { CustomId = clientName, CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnPlayFabError);

        // Disable input panel
        uiManager.SetInputInteractable(false);
    }

    private void OnLoginSuccess(LoginResult result) {
        // If login is a success, attempt to start matchmaking with the client's entity key values
        StartMatchmakingRequest(result.EntityToken.Entity.Id, result.EntityToken.Entity.Type);
    }

    private void StartMatchmakingRequest(string entityID, string entityType) {
        // Create a matchmaking request
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest {
                Creator = new MatchmakingPlayer {
                    Entity = new PlayFab.MultiplayerModels.EntityKey {
                        Id = entityID,
                        Type = entityType
                    },
                    Attributes = new MatchmakingPlayerAttributes {
                        DataObject = new {
                            Latencies = new object[] {
                                new {
                                    region = "EastUs",
                                    latency = 100
                                }
                            },
                        },
                    },
                },

                // Cancel matchmaking after this time in seconds with no match found
                GiveUpAfterSeconds = matchmakingTimeout,

                // name of the queue to poll
                QueueName = matchmakingQueue,
            },

            this.OnMatchmakingTicketCreated,
            this.OnPlayFabError
        );
    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult createMatchmakingTicketResult) {
        // Now we need to start polling the ticket periodically, using a coroutine
        StartCoroutine(PollMatchmakingTicket(createMatchmakingTicketResult.TicketId));

        // Display progress in UI
        uiManager.DisplayNetworkMessage("Matchmaking request sent");
    }

    private IEnumerator PollMatchmakingTicket(string ticketId) {
        // Delay ticket request
        yield return new WaitForSeconds(10);

        // Poll the ticket
        PlayFabMultiplayerAPI.GetMatchmakingTicket(
            new GetMatchmakingTicketRequest {
                TicketId = ticketId,
                QueueName = matchmakingQueue
            },

            // callbacks
            this.OnGetMatchmakingTicket,
            this.OnPlayFabError
        );
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult getMatchmakingTicketResult) {
        // When PlayFab returns our matchmaking ticket

        if (getMatchmakingTicketResult.Status == "Matched") {
            // If we found a match, we then need to access its server
            MatchFound(getMatchmakingTicketResult);
        } else if (getMatchmakingTicketResult.Status == "Canceled") {
            // If the matchmaking ticket was canceled we need to reset the input UI
            uiManager.SetInputInteractable(true);
            uiManager.DisplayNetworkMessage("Start Session");
        } else {
            // If we don't have a conclusive matchmaking status, we keep polling the ticket
            StartCoroutine(PollMatchmakingTicket(getMatchmakingTicketResult.TicketId));
        }

        // Display matchmaking status in the UI
        uiManager.DisplayNetworkMessage(getMatchmakingTicketResult.Status);
    }

     private void MatchFound(GetMatchmakingTicketResult getMatchmakingTicketResult) {
        // When we find a match, we need to request the connection variables to join clients
        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest {
                MatchId = getMatchmakingTicketResult.MatchId,
                QueueName = matchmakingQueue
            },

            this.OnGetMatch,
            this.OnPlayFabError
        );
    }

    private void OnGetMatch(GetMatchResult getMatchResult) {
        // Get the server to join
        string ipString = getMatchResult.ServerDetails.IPV4Address;
        int tcpPort = 0;
        int udpPort = 0;

        // Get the ports and names to join
        foreach (Port port in getMatchResult.ServerDetails.Ports) {
            if (port.Name == playfabTCPPortName)
                tcpPort = port.Num;

            if (port.Name == playfabUDPPortName)
                udpPort = port.Num;
        }

        // Connect and initialize the DarkRiftClient, hand over control to the NetworkManager
        if (tcpPort != 0 && udpPort != 0)
            drClient.ConnectInBackground(IPAddress.Parse(ipString), tcpPort, udpPort, true, null);
    }

    // PlayFab error handling //
    private void OnPlayFabError(PlayFabError error) {
        // Debug log an error report
        Debug.Log(error.GenerateErrorReport());
    }
}