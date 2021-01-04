using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;

public class PlayfabClient : MonoBehaviour
{
    private string buildID = "30bd1df9-6914-4c6a-a653-f64b6f6e41ad";
    private string region = "EastUs";

    public void Start()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId)){
            /*
            Please change the titleId below to your own titleId from PlayFab Game Manager.
            If you have already set the value in the Editor Extensions, this can be skipped.
            */
            PlayFabSettings.staticSettings.TitleId = "6A3B8";
        }
        var request = new LoginWithCustomIDRequest { CustomId = "Andrew", CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        // List multiplayer server
        // ListMutliplayerServers();

        // Requesting multiplayer server
        RequestMultiplayerServer();
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogWarning("Something went wrong with your first API call.  :(");
        Debug.LogError("Here's some debug information:");
        Debug.LogError(error.GenerateErrorReport());
    }

    private void RequestMultiplayerServer() {
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();
        requestData.BuildId = buildID;
        requestData.SessionId = System.Guid.NewGuid().ToString();
        requestData.PreferredRegions = new List<string> {region};
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
    }

    private void ListMutliplayerServers() {
        ListMultiplayerServersRequest serversRequest = new ListMultiplayerServersRequest();
        serversRequest.BuildId = buildID;
        serversRequest.Region = region;

        PlayFabMultiplayerAPI.ListMultiplayerServers(serversRequest, OnListMultiplayerServers, OnListMultiplayerServersError);
    }

    private void OnListMultiplayerServers(ListMultiplayerServersResponse response) {
        Debug.Log("LIST REQUEST SUCCESSFUL");
        Debug.Log(response.ToString());
    }

    private void OnListMultiplayerServersError(PlayFabError error) {
        Debug.Log("ERROR LIST REQUEST");
        Debug.Log(error.GenerateErrorReport());
    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
	{
		Debug.Log(response.ToString());
        Debug.Log(response.IPV4Address);
        foreach (Port port in response.Ports) {
            Debug.Log(port.Num);
            Debug.Log(port.Name);
        }
        Debug.Log(response.Region);
	}

    private void OnRequestMultiplayerServerError(PlayFabError error)
	{
        Debug.Log("ERROR SERVER REQUEST");
		Debug.Log(error.GenerateErrorReport());
	}
}
