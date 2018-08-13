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
			GameState.Teams.Add(teamSpawn.teamId, new Team(teamSpawn.teamId, teamSpawn.teamName, teamSpawn.teamColor));
		}
	}

	private void Update() {
		if (GameState.CurrentState == GameStates.WAITING_FOR_PLAYERS) {
			bool hasEnoughPlayers = true;
			foreach (Team team in GameState.Teams.Values) {
				if (team.Players.Count < minPlayersPerTeam) {
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
		if (GameState.CurrentState == GameStates.WAITING_FOR_PLAYERS) {
			bool hasEnoughPlayers = true;
			foreach (Team team in GameState.Teams.Values) {
				if (team.Players.Count < minPlayersPerTeam) {
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
		GameState.CurrentState = GameStates.GAME_IN_PROGRESS;
		serverNetworkManager.StartGame(GameState);
	}

	public void EndGame() {
		GameState.CurrentState = GameStates.GAME_COMPLETED;
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
			player.TeamId = (ushort)nextTeam;
			player.Resources = 100;
			nextTeam = nextTeam >= 2 ? 1 : nextTeam + 1;
		}
		GameState.Teams[player.TeamId].Players.Add(player.ID, player);
		return player;
	}

	public Player PlayerLeft(string playerId) {
		Player player = GameState.GetPlayer(playerId);
		if (player != null) {
			GameState.Teams[player.TeamId].Players.Remove(player.ID);
			disconnectedPlayers.Add(player.ID, player);
		}
		return player;
	}

	public void IncreaseScore(ushort teamId, int points) {
		GameState.Teams[teamId].Score += points;
		if (GameState.Teams[teamId].Score >= GameState.PointsToWin) {
			EndGame();
		} else {
			serverNetworkManager.BroadcastGameState(GameState);
		}
	}

	public void IncreaseResources(ushort teamId, int resources) {
		foreach (Player player in GameState.Teams[teamId].Players.Values) {
			player.Resources += resources;
		}
		serverNetworkManager.BroadcastGameState(GameState);
	}

	public void DecreaseResources(Player player, int resources) {
		player.Resources -= resources;
		serverNetworkManager.BroadcastGameState(GameState);
	}
}
