using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameState : IDarkRiftSerializable {

	public int PointsToWin { get; set; }
	public GameStates CurrentState { get; set; }
	public Dictionary<ushort, Team> Teams { get; private set; }

	public GameState() {
		CurrentState = GameStates.WAITING_FOR_PLAYERS;
		Teams = new Dictionary<ushort, Team>();
	}

	public GameState(int pointsToWin) {
		PointsToWin = pointsToWin;
		CurrentState = GameStates.WAITING_FOR_PLAYERS;
		Teams = new Dictionary<ushort, Team>();
	}

	public Player GetPlayer(string playerId) {
		return (from team in Teams.Values
				where team.Players.ContainsKey(playerId)
				select team.Players[playerId]).FirstOrDefault();
	}

	public void Deserialize(DeserializeEvent e) {
		CurrentState = (GameStates)e.Reader.ReadInt32();
		int numTeams = e.Reader.ReadInt32();
		for (int i = 0; i < numTeams; i++) {
			Team team = e.Reader.ReadSerializable<Team>();
			Teams.Add(team.ID, team);
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)CurrentState);
		e.Writer.Write(Teams.Count);
		foreach (Team team in Teams.Values) {
			e.Writer.Write(team);
		}
	}
}

public enum GameStates {
	WAITING_FOR_PLAYERS,
	GAME_IN_PROGRESS,
	GAME_COMPLETED
}