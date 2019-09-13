using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class ServerGameManager : Singleton<ServerGameManager> {

	public int minPlayersPerTeam = 1;
	public int pointsToWin = 1000;

	public GameState GameState { get; private set; }
	public List<CapturePoint> CapturePoints { get; private set; }
	public Dictionary<ushort, TeamSpawn> TeamSpawns { get; private set; }

	private int nextTeam = 1;
	private Dictionary<string, Player> disconnectedPlayers = new Dictionary<string, Player>();
	private ServerNetworkManager serverNetworkManager;

	protected void Start() {
		serverNetworkManager = ServerNetworkManager.Instance;

		GameState = new GameState(pointsToWin);

		CapturePoints = FindObjectsOfType<CapturePoint>().ToList();

		TeamSpawns = new Dictionary<ushort, TeamSpawn>();
		foreach (TeamSpawn teamSpawn in FindObjectsOfType<TeamSpawn>()) {
			TeamSpawns.Add(teamSpawn.teamId, teamSpawn);
			GameState.teams.Add(teamSpawn.teamId, new Team(teamSpawn.teamId, teamSpawn.teamName, teamSpawn.teamColor));
		}
	}

	private void Update() {
		if (GameState.currentState == GameStates.WAITING_FOR_PLAYERS) {
			bool hasEnoughPlayers = true;
			foreach (Team team in GameState.teams.Values) {
				if (team.players.Count < minPlayersPerTeam) {
					hasEnoughPlayers = false;
					break;
				}
			}
			if (hasEnoughPlayers) {
				StartGame();
			}
		}

		if (CapturePoints != null && CapturePoints.Count > 0) {
			serverNetworkManager.BroadcastCapturePoints(CapturePoints);
		}
	}

	public void TryToStartGame() {
		if (GameState.currentState == GameStates.WAITING_FOR_PLAYERS) {
			bool hasEnoughPlayers = true;
			foreach (Team team in GameState.teams.Values) {
				if (team.players.Count < minPlayersPerTeam) {
					hasEnoughPlayers = false;
					break;
				}
			}
			if (hasEnoughPlayers) {
				StartGame();
			}
		}
	}

	private void StartGame() {
		GameState.currentState = GameStates.GAME_IN_PROGRESS;
		serverNetworkManager.StartGame(GameState);
	}

	public void EndGame() {
		GameState.currentState = GameStates.GAME_COMPLETED;
		serverNetworkManager.EndGame(GameState);

		Invoke("RestartGame", 5f);
	}

	private void RestartGame() {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public Player PlayerJoined(string playerId, string name) {
		Player player;
		if (disconnectedPlayers.ContainsKey(playerId)) {
			player = disconnectedPlayers[playerId];
			disconnectedPlayers.Remove(playerId);
		} else {
			player = new Player(playerId, name);
			player.teamId = (ushort)nextTeam;
			player.resources = 100;
			nextTeam = nextTeam >= 2 ? 1 : nextTeam + 1;
		}
		GameState.teams[player.teamId].players.Add(player.id, player);
		return player;
	}

	public Player PlayerLeft(string playerId) {
		Player player = GameState.GetPlayer(playerId);
		if (player != null) {
			GameState.teams[player.teamId].players.Remove(player.id);
			disconnectedPlayers.Add(player.id, player);
		}
		return player;
	}

	public void IncreaseScore(ushort teamId, int points) {
		GameState.teams[teamId].score += points;
		if (GameState.teams[teamId].score >= GameState.pointsToWin) {
			EndGame();
		} else {
			serverNetworkManager.BroadcastGameState(GameState);
		}
	}

	public void IncreaseResources(ushort teamId, int resources) {
		if (GameState.teams[teamId].players.Count > 0) {
			int perPlayer = resources / GameState.teams[teamId].players.Count;
			foreach (Player player in GameState.teams[teamId].players.Values) {
				player.resources += perPlayer;
			}
		}
		serverNetworkManager.BroadcastGameState(GameState);
	}

	public void DecreaseResources(Player player, int resources) {
		player.resources -= resources;
		serverNetworkManager.BroadcastGameState(GameState);
	}
}
