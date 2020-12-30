using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;

public class PlayfabClient : MonoBehaviour
{
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
        requestData.BuildId = "75696de3-25a9-47a9-ac19-795c261bd57b";
        requestData.SessionId = System.Guid.NewGuid().ToString();
        requestData.PreferredRegions = new List<string> {"EastUs"};
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
	{
		Debug.Log(response.ToString());
	}

    private void OnRequestMultiplayerServerError(PlayFabError error)
	{
        Debug.Log("ERROR SERVER REQUEST");
		Debug.Log(error.GenerateErrorReport());
	}
}
