using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;

public class ClientPlayFabManager : Singleton<ClientPlayFabManager> {
	
	public string gameMode = "Default";
	public string build = "1.0";
	public string loginId = "DanTest";

	public string PlayFabId { get; private set; }

	private ClientNetworkManager clientNetworkManager;

	public void Start() {
		clientNetworkManager = ClientNetworkManager.Instance;

		if (!clientNetworkManager.offlineTest) {
			clientNetworkManager.OnJoinServerUpdate("PlayFab: Attempting to log into PlayFab.");
			PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest { CustomId = loginId, CreateAccount = true }, PlayFab_OnLoginSuccess, PlayFab_OnServerJoinFailure);
		}
	}

	private void PlayFab_OnLoginSuccess(LoginResult result) {
		PlayFabId = result.PlayFabId;
		clientNetworkManager.OnJoinServerUpdate("PlayFab: Successfully logged in.");
		PlayFabClientAPI.Matchmake(new MatchmakeRequest() { GameMode = gameMode, BuildVersion = build, Region = Region.USEast, StartNewIfNoneFound = false }, PlayFab_OnMatchmakeSuccess, PlayFab_OnServerJoinFailure);
	}

	private void PlayFab_OnMatchmakeSuccess(MatchmakeResult result) {
		if (result.Status == MatchmakeStatus.Complete) {
			clientNetworkManager.JoinServer(result.ServerHostname, result.Ticket);
		} else if (result.Status == MatchmakeStatus.GameNotFound || result.Status == MatchmakeStatus.NoAvailableSlots) {
			clientNetworkManager.OnJoinServerFailed("PlayFab: No servers with available slots were found.");
		} else {
			clientNetworkManager.OnJoinServerFailed("PlayFab: Returned unknown matchmaking status: " + result.Status);
		}
	}

	private void PlayFab_OnServerJoinFailure(PlayFabError error) {
		clientNetworkManager.OnJoinServerFailed("PlayFab: Error occurred: " + error.GenerateErrorReport());
	}

	public void UpdateUserData() {
		//surely this will be used for something...
		//PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest { Keys = new List<string> { "CurrentHealth" } }, PlayFab_OnUserDataSuccess, PlayFab_OnAnyFailure);
	}

	private void PlayFab_OnUserDataSuccess(GetUserDataResult result) {
		throw new NotImplementedException();
	}

	private void PlayFab_OnAnyFailure(PlayFabError error) {
		Debug.LogError("PlayFab: Error occurred: " + error.GenerateErrorReport());
	}

	public bool IsConnectedToPlayFab() {
		return PlayFabClientAPI.IsClientLoggedIn();
	}
}