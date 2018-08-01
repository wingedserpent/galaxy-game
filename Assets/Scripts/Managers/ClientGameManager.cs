﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public delegate void OnGameStateInitialized();

public class ClientGameManager : Singleton<ClientGameManager> {

	public ConnectionMenuController connectionMenuController;
	public SquadMenuController squadMenuController;
	public Text scoreDisplay;

	public bool IsOfflineTest { get; private set; }
	public bool IsAcceptingGameInput { get; private set; }
	public GameStates ClientState { get; private set; }
	public GameState GameState { get; private set; }
	public Player MyPlayer { get; private set; }
	public Dictionary<ushort, CapturePoint> CapturePoints { get; private set; }
	public Dictionary<ushort, TeamSpawn> TeamSpawns { get; private set; }

	protected override void Awake() {
		base.Awake();
		IsOfflineTest = false;
		IsAcceptingGameInput = false;
		ClientState = GameStates.WAITING_FOR_PLAYERS;
	}

	private void Start() {
		connectionMenuController.OpenMenu();

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

		if (ClientState == GameStates.WAITING_FOR_PLAYERS && GameState.CurrentState == GameStates.GAME_IN_PROGRESS) {
			StartGame(hasSpawnedEntities);
		} else if (ClientState == GameStates.GAME_IN_PROGRESS && GameState.CurrentState == GameStates.GAME_COMPLETED) {
			EndGame();
		}

		UpdateScoreDisplay();
	}

	private void StartGame(bool hasSpawnedEntities) {
		if (!hasSpawnedEntities) {
			squadMenuController.OpenMenu(MyPlayer.MaxSquadCost);
		}
		ClientState = GameStates.GAME_IN_PROGRESS;
		IsAcceptingGameInput = true;
	}

	public void StartOfflineTest() {
		IsOfflineTest = true;
		ClientState = GameStates.GAME_IN_PROGRESS;
		IsAcceptingGameInput = true;
	}

	public void EndGame() {
		ClientState = GameStates.GAME_COMPLETED;
		IsAcceptingGameInput = false;

		Debug.Log("Game Over!");

		Invoke("RestartGame", 10f);
	}

	private void RestartGame() {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public void OnPlayerJoined(Player player) {
		GameState.Teams[player.TeamId].Players.Add(player.ID, player);
		Debug.Log("Player joined: " + player.Name);
	}

	public void OnPlayerLeft(Player player) {
		Player result = GameState.GetPlayer(player.ID);
		if (result != null) {
			GameState.Teams[result.TeamId].Players.Remove(result.ID);
		}
		Debug.Log("Player left: " + player.Name);
	}

	public void OnUnitListReceived(List<PlayerUnit> playerUnits) {
		squadMenuController.OnUnitListReceived(playerUnits);
	}

	public void OnSquadDead() {
		squadMenuController.OpenMenu();
	}

	public void UpdateScoreDisplay() {
		scoreDisplay.text = "Score:\n";
		foreach (Team team in GameState.Teams.Values) {
			scoreDisplay.text += team.Name + ": " + team.Score + "\n";
		}
	}
}
