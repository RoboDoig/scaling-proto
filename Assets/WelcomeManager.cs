using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;

public class WelcomeManager : MonoBehaviour
{

    public string buildID;
    public string region;
    public string titleID;

    private GameObject mainCanvas;
    private InputField inputField;
    private Button startButton;

    // Start is called before the first frame update
    void Start()
    {
        // Get the scene UI components for player name and start button
        mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas");
        inputField = mainCanvas.GetComponentInChildren<InputField>();
        startButton = mainCanvas.GetComponentInChildren<Button>();

        // Link start button click to StartSession method
        startButton.onClick.AddListener(StartSession);
    }

    public void StartSession() {
        string clientName = inputField.text;

        // First attempt to login
        var request = new LoginWithCustomIDRequest { CustomId = clientName, CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);

        startButton.interactable = false;
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
        // If matchmaking is successful...
        Debug.Log("Matchmaking request sent");
        Debug.Log(createMatchmakingTicketResult.TicketId);
        startButton.GetComponentInChildren<Text>().text = "Matchmaking request sent";

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
        Debug.Log(getMatchmakingTicketResult.Status);
        startButton.GetComponentInChildren<Text>().text = getMatchmakingTicketResult.Status;

        if (getMatchmakingTicketResult.Status == "Matched") {
            // If we found a match, we then need to access its server
            MatchFound(getMatchmakingTicketResult);
        } else if (getMatchmakingTicketResult.Status == "Canceled") {
            // If the matchmaking ticket was canceled
            startButton.interactable = false;
            startButton.GetComponentInChildren<Text>().text = "Start Session";
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
        Debug.Log(getMatchResult.ServerDetails.IPV4Address);

        // Get the ports and names to join
        foreach (Port port in getMatchResult.ServerDetails.Ports) {
            Debug.Log(port.Name);
            Debug.Log(port.Num);
        }
    }
}
