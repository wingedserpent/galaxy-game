using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Team : IDarkRiftSerializable {

	public ushort id { get; set; }
	public string name { get; set; }
	public Color color { get; set; }
	public int score { get; set; }
	public Dictionary<string, Player> players { get; private set; }

	public Team() {
		score = 0;
		players = new Dictionary<string, Player>();
	}

	public Team(ushort id, string name, Color color) {
		this.id = id;
		this.name = name;
		this.color = color;
		score = 0;
		players = new Dictionary<string, Player>();
	}

	public void Deserialize(DeserializeEvent e) {
		id = e.Reader.ReadUInt16();
		name = e.Reader.ReadString();
		color = new Color(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		score = e.Reader.ReadInt32();

		int numPlayers = e.Reader.ReadInt32();
		for (int i = 0; i < numPlayers; i++) {
			string playerId = e.Reader.ReadString();
			if (players.ContainsKey(playerId)) {
				Player player = players[playerId];
				e.Reader.ReadSerializableInto<Player>(ref player);
			} else {
				players.Add(playerId, e.Reader.ReadSerializable<Player>());
			}
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(id);
		e.Writer.Write(name);
		e.Writer.Write(color.r); e.Writer.Write(color.g); e.Writer.Write(color.b);
		e.Writer.Write(score);

		e.Writer.Write(players.Count);
		foreach (Player player in players.Values) {
			e.Writer.Write(player.id);
			e.Writer.Write(player);
		}
	}
}
