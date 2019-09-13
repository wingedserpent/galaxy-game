using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameState : IDarkRiftSerializable {

	public int pointsToWin { get; set; }
	public GameStates currentState { get; set; }
	public Dictionary<ushort, Team> teams { get; private set; }

	public GameState() {
		currentState = GameStates.WAITING_FOR_PLAYERS;
		teams = new Dictionary<ushort, Team>();
	}

	public GameState(int pointsToWin) {
		this.pointsToWin = pointsToWin;
		currentState = GameStates.WAITING_FOR_PLAYERS;
		teams = new Dictionary<ushort, Team>();
	}

	public Player GetPlayer(string playerId) {
		return (from team in teams.Values
				where team.players.ContainsKey(playerId)
				select team.players[playerId]).FirstOrDefault();
	}

	public void Deserialize(DeserializeEvent e) {
		currentState = (GameStates)e.Reader.ReadInt32();

		int numTeams = e.Reader.ReadInt32();
		for (int i = 0; i < numTeams; i++) {
			ushort teamId = e.Reader.ReadUInt16();
			if (teams.ContainsKey(teamId)) {
				Team team = teams[teamId];
				e.Reader.ReadSerializableInto<Team>(ref team);
			} else {
				teams.Add(teamId, e.Reader.ReadSerializable<Team>());
			}
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)currentState);

		e.Writer.Write(teams.Count);
		foreach (Team team in teams.Values) {
			e.Writer.Write(team.id);
			e.Writer.Write(team);
		}
	}
}

public enum GameStates {
	WAITING_FOR_PLAYERS,
	GAME_IN_PROGRESS,
	GAME_COMPLETED
}