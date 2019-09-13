using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DarkRift.Server.Unity;
using PlayFab;
using PlayFab.ServerModels;

public class ServerPlayFabManager : Singleton<ServerPlayFabManager> {
	
	public UnityServer server;
	public string gameMode = "Default";
	public string build = "1.0";
	
	private string ipAddress;
	private string lobbyId;
	private bool isServerRegistered = false;
	
	private void Start() {
		Application.wantsToQuit += OnQuit;
		StartCoroutine(BeginPlayFabRegistration()); 
	}

	private IEnumerator BeginPlayFabRegistration() {
		WWW externalIPRequest = new WWW("http://api.ipify.org/");
		yield return externalIPRequest;
		ipAddress = externalIPRequest.text;

		Debug.Log("PlayFab: Attempting to register game server.");

		PlayFabServerAPI.RegisterGame(new RegisterGameRequest() {
			ServerHost = ipAddress,
			ServerPort = server.Port.ToString(),
			GameMode = gameMode,
			Build = build,
			Region = Region.USEast
		}, PlayFab_OnRegisterSuccess, PlayFab_OnAnyFailure);
	}

	private void PlayFab_OnRegisterSuccess(RegisterGameResponse response) {
		lobbyId = response.LobbyId;
		Debug.Log("PlayFab: Successfully registered game server. LobbyId: " + lobbyId);
		isServerRegistered = true;
		InvokeRepeating("RefreshGameServerInstanceHeartbeat", 90f, 90f);
	}

	private void RefreshGameServerInstanceHeartbeat() {
		PlayFabServerAPI.RefreshGameServerInstanceHeartbeat(new RefreshGameServerInstanceHeartbeatRequest() {
			LobbyId = lobbyId
		}, null, PlayFab_OnAnyFailure);
	}

	private bool OnQuit() {
		if (isServerRegistered) {
			StartCoroutine(ApplicationDelayedQuit());

			Debug.Log("PlayFab: Attempting to deregister game server.");
			PlayFabServerAPI.DeregisterGame(new DeregisterGameRequest() {
				LobbyId = lobbyId
			}, PlayFab_OnDeregisterSuccess, PlayFab_OnAnyFailure);

			return false;
		}

		return true;
	}

	private IEnumerator ApplicationDelayedQuit() {
		yield return new WaitForSecondsRealtime(5f);
		Debug.LogWarning("PlayFab: Failed to deregister server. Allowing application to quit anyway.");
		isServerRegistered = false;
		Application.Quit();
	}

	private void PlayFab_OnDeregisterSuccess(DeregisterGameResponse response) {
		Debug.Log("PlayFab: Successfully deregistered server.");
		isServerRegistered = false;
		Application.Quit();
	}

	public void RedeemMatchmakerTicket(string matchmakerTicket) {
		PlayFabServerAPI.RedeemMatchmakerTicket(new RedeemMatchmakerTicketRequest() { LobbyId = lobbyId, Ticket = matchmakerTicket }, PlayFab_OnRedeemTicketSuccess, PlayFab_OnAnyFailure);
	}

	private void PlayFab_OnRedeemTicketSuccess(RedeemMatchmakerTicketResult result) {
		string ticket = ((RedeemMatchmakerTicketRequest)result.Request).Ticket;
		//Reply to client whether or not they are validated and considered 'joined'
		ServerNetworkManager.Instance.FinalizeJoinRequest(ticket, result.UserInfo, result.TicketIsValid);
	}

	public void NotifyPlayerLeft(string playFabId) {
		PlayFabServerAPI.NotifyMatchmakerPlayerLeft(new NotifyMatchmakerPlayerLeftRequest() { LobbyId = lobbyId, PlayFabId = playFabId }, null, PlayFab_OnAnyFailure);
	}

	public void UpdateUserData(string playFabId, Dictionary<string, string> userData) {
		PlayFabServerAPI.UpdateUserReadOnlyData(new UpdateUserDataRequest() { PlayFabId = playFabId, Data = userData }, PlayFab_OnUpdateUserDataSuccess, PlayFab_OnAnyFailure);
	}

	private void PlayFab_OnUpdateUserDataSuccess(UpdateUserDataResult result) {
		//doesn't do anything yet
	}
	
	private void PlayFab_OnAnyFailure(PlayFabError error) {
		Debug.LogError("PlayFab: Error occurred: " + error.GenerateErrorReport());
	}
}
