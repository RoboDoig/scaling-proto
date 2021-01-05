using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkStarter : MonoBehaviour
{
    // PlayFab settings
    public string titleId;
    public string region;
    public string buildId;

    // DarkRift
    private UnityClient drClient;

    // UI i/o components
    private UIManager uiManager;

    // Start is called before the first frame update
    void Start()
    {
        // Get reference to UI singleton
        uiManager = UIManager.singleton;

        // Link start button click to StartSession method
        uiManager.startSessionButton.onClick.AddListener(StartSession);
        uiManager.localTestButton.onClick.AddListener(StartLocalSession);

        // Link to DR client
        drClient = GetComponent<UnityClient>();
    }

    public void StartSession() {
        string clientName = uiManager.nameInputField.text;

        // First attempt to login
        var request = new LoginWithCustomIDRequest { CustomId = clientName, CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);

        uiManager.startSessionButton.interactable = false;
        uiManager.localTestButton.interactable = false;
        uiManager.nameInputField.interactable = false;
    }

    // Connect with local test server
    public void StartLocalSession() {
        drClient.ConnectInBackground(IPAddress.Parse("127.0.0.1"), 7777, 7777, true, delegate {OnLocalSessionCallback();} );

        uiManager.startSessionButton.interactable = false;
        uiManager.localTestButton.interactable = false;
        uiManager.nameInputField.interactable = false;
    }

    public void OnLocalSessionCallback() {
        if (drClient.ConnectionState == ConnectionState.Connected) {
            uiManager.readyButton.interactable = true;
        } else {
            uiManager.startSessionButton.interactable = true;
            uiManager.localTestButton.interactable = true;
            uiManager.nameInputField.interactable = true;
        }
    }

    private void OnLoginSuccess(LoginResult result) {
        // If login is a success, attempt to start matchmaking with the client's entity key values
        StartMatchmakingRequest(result.EntityToken.Entity.Id, result.EntityToken.Entity.Type);
    }

    // TODO - all these error handlers do basically the same thing so far, maybe they can be consolidated
    private void OnLoginFailure(PlayFabError error) {
        // If login fails, debug log an error report
        Debug.Log(error.GenerateErrorReport());
    }

    private void StartMatchmakingRequest(string entityID, string entityType) {
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
                GiveUpAfterSeconds = 100,

                // name of the queue to poll
                QueueName = "standard_queue",
            },

            this.OnMatchmakingTicketCreated,
            this.OnMatchmakingError
        );
    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult createMatchmakingTicketResult) {
        uiManager.startSessionButton.GetComponentInChildren<Text>().text = "Matchmaking request sent";

        // Now we need to start polling the ticket
        StartCoroutine(PollMatchmakingTicket(createMatchmakingTicketResult.TicketId));
    }

    private void OnMatchmakingError(PlayFabError error) {
        // If matchmaking request fails, log an error report
        Debug.Log(error.GenerateErrorReport());
    }

    private IEnumerator PollMatchmakingTicket(string ticketId) {
        // Delay ticket request
        yield return new WaitForSeconds(10);

        // Poll the ticket
        PlayFabMultiplayerAPI.GetMatchmakingTicket(
            new GetMatchmakingTicketRequest {
                TicketId = ticketId,
                QueueName = "standard_queue"
            },

            // callbacks
            this.OnGetMatchmakingTicket,
            this.OnMatchmakingError
        );
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult getMatchmakingTicketResult) {
        uiManager.startSessionButton.GetComponentInChildren<Text>().text = getMatchmakingTicketResult.Status;

        if (getMatchmakingTicketResult.Status == "Matched") {
            // If we found a match, we then need to access its server
            MatchFound(getMatchmakingTicketResult);
        } else if (getMatchmakingTicketResult.Status == "Canceled") {
            // If the matchmaking ticket was canceled
            uiManager.startSessionButton.interactable = true;
            uiManager.localTestButton.interactable = true;
            uiManager.nameInputField.interactable = true;
            uiManager.startSessionButton.GetComponentInChildren<Text>().text = "Start Session";
        } else {
            // else we keep polling the ticket
            StartCoroutine(PollMatchmakingTicket(getMatchmakingTicketResult.TicketId));
        }
    }

    private void MatchFound(GetMatchmakingTicketResult getMatchmakingTicketResult) {
        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest {
                MatchId = getMatchmakingTicketResult.MatchId,
                QueueName = "standard_queue"
            },

            this.OnGetMatch,
            this.OnMatchmakingError
        );
    }

    private void OnGetMatch(GetMatchResult getMatchResult) {
        // Get the server to join
        string ipString = getMatchResult.ServerDetails.IPV4Address;
        int tcpPort = 0;
        int udpPort = 0;

        // Get the ports and names to join
        foreach (Port port in getMatchResult.ServerDetails.Ports) {
            if (port.Name == "gg_tcp")
                tcpPort = port.Num;

            if (port.Name == "gg_udp")
                udpPort = port.Num;
        }

        // Connect and initialize the DarkRiftClient, hand over control to the NetworkManager
        if (tcpPort != 0 && udpPort != 0)
            drClient.ConnectInBackground(IPAddress.Parse(ipString), tcpPort, udpPort, true, null);
    }
}
