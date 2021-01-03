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

    void Update() {

    }

    public void StartSession() {
        string clientName = inputField.text;

        // First attempt to login
        var request = new LoginWithCustomIDRequest { CustomId = clientName, CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result) {
        // If login is a success, attempt to start matchmaking with the client's entity key values
        StartMatchmakingRequest(result.EntityToken.Entity.Id, result.EntityToken.Entity.Type);
    }

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

        // Now we need to start polling the ticket
        PollMatchmakingTicket(createMatchmakingTicketResult.TicketId);
    }

    private void OnMatchmakingError(PlayFabError error) {
        // If matchmaking request fails, log an error report
        Debug.Log(error.GenerateErrorReport());
    }

    private void PollMatchmakingTicket(string ticketId) {
        PlayFabMultiplayerAPI.GetMatchmakingTicket(
            new GetMatchmakingTicketRequest {
                TicketId = ticketId,
                QueueName = "standard_queue"
            },

            this.OnGetMatchmakingTicket,
            this.OnMatchmakingError
        );
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult getMatchmakingTicketRequest) {

    }
}
