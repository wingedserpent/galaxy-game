using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Team : IDarkRiftSerializable {

	public ushort ID { get; set; }
	public string Name { get; set; }
	public Color Color { get; set; }
	public int Score { get; set; }
	public Dictionary<string, Player> Players { get; private set; }

	public Team() {
		Score = 0;
		Players = new Dictionary<string, Player>();
	}

	public Team(ushort ID, string name) {
		this.ID = ID;
		Name = name;
		Score = 0;
		Players = new Dictionary<string, Player>();
	}

	public void Deserialize(DeserializeEvent e) {
		ID = e.Reader.ReadUInt16();
		Name = e.Reader.ReadString();
		Color = new Color(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		Score = e.Reader.ReadInt32();
		int numPlayers = e.Reader.ReadInt32();
		for (int i = 0; i < numPlayers; i++) {
			Player player = e.Reader.ReadSerializable<Player>();
			Players.Add(player.ID, player);
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(ID);
		e.Writer.Write(Name);
		e.Writer.Write(Color.r); e.Writer.Write(Color.g); e.Writer.Write(Color.b);
		e.Writer.Write(Score);
		e.Writer.Write(Players.Count);
		foreach (Player player in Players.Values) {
			e.Writer.Write(player);
		}
	}
}
