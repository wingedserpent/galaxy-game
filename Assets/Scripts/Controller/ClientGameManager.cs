﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ClientGameManager : Singleton<ClientGameManager> {
	
	public bool IsAcceptingGameInput { get; private set; }
	public GameStates ClientState { get; private set; }
	public GameState GameState { get; private set; }
	public Player MyPlayer { get; private set; }
	public Dictionary<ushort, CapturePoint> CapturePoints { get; private set; }
	public Dictionary<ushort, TeamSpawn> TeamSpawns { get; private set; }

	private UIManager uiManager;

	protected override void Awake() {
		base.Awake();
		
		IsAcceptingGameInput = false;
		ClientState = GameStates.WAITING_FOR_PLAYERS;
	}

	private void Start() {
		uiManager = UIManager.Instance;

		CapturePoints = new Dictionary<ushort, CapturePoint>();
		foreach (CapturePoint capturePoint in FindObjectsOfType<CapturePoint>()) {
			CapturePoints.Add(capturePoint.ID, capturePoint);
		}

		TeamSpawns = new Dictionary<ushort, TeamSpawn>();
		foreach (TeamSpawn teamSpawn in FindObjectsOfType<TeamSpawn>()) {
			TeamSpawns.Add(teamSpawn.teamId, teamSpawn);
		}
	}

	public void UpdateGameState(GameState newGameState, bool hasSpawnedEntities = false) {
		GameState = newGameState;
		MyPlayer = GameState.GetPlayer(ClientPlayFabManager.Instance.PlayFabId);

		if (ClientState == GameStates.WAITING_FOR_PLAYERS && GameState.currentState == GameStates.GAME_IN_PROGRESS) {
			StartGame(hasSpawnedEntities);
		} else if (ClientState == GameStates.GAME_IN_PROGRESS && GameState.currentState == GameStates.GAME_COMPLETED) {
			EndGame();
		}

		uiManager.UpdateDisplays();
	}

	private void StartGame(bool hasSpawnedEntities) {
		if (!hasSpawnedEntities) {
			uiManager.OpenSquadMenu();
		}
		ClientState = GameStates.GAME_IN_PROGRESS;
		IsAcceptingGameInput = true;
	}

	public void StartOfflineTest() {
		MyPlayer = new Player("ZZZZ", "Offline Tester");
		MyPlayer.teamId = 1;
		MyPlayer.resources = 9999;
		ClientState = GameStates.GAME_IN_PROGRESS;
		IsAcceptingGameInput = true;
	}

	public void EndGame() {
		ClientState = GameStates.GAME_COMPLETED;
		IsAcceptingGameInput = false;
		uiManager.AddSystemMessage("Game Over! Exiting to menu in 10 seconds...");
		OverallStateManager.Instance.OnGameEnd();
	}

	public void OnPlayerJoined(Player player) {
		GameState.teams[player.teamId].players.Add(player.id, player);
		uiManager.AddSystemMessage("Player joined: " + player.name);
	}

	public void OnPlayerLeft(Player player) {
		Player result = GameState.GetPlayer(player.id);
		if (result != null) {
			GameState.teams[result.teamId].players.Remove(result.id);
		}
		uiManager.AddSystemMessage("Player left: " + player.name);
	}

	public void OnSquadGone() {
		uiManager.OpenSquadMenu();
	}
}
