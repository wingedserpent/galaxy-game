using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : IDarkRiftSerializable {

	public string id { get; private set; }
	public ushort teamId { get; set; }
	public string name { get; private set; }
	public int maxSquadCost { get; set; }
	public int resources { get; set; }

	public Player() { }

	public Player(string id, string name) {
		this.id = id;
		this.name = name;
		resources = 0;
	}

	public void Deserialize(DeserializeEvent e) {
		id = e.Reader.ReadString();
		teamId = e.Reader.ReadUInt16();
		name = e.Reader.ReadString();
		maxSquadCost = e.Reader.ReadInt32();
		resources = e.Reader.ReadInt32();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(id);
		e.Writer.Write(teamId);
		e.Writer.Write(name);
		e.Writer.Write(maxSquadCost);
		e.Writer.Write(resources);
	}

	public override bool Equals(object obj) {
		var player = obj as Player;
		return player != null &&
			   id == player.id;
	}

	public override int GetHashCode() {
		return 1213502048 + EqualityComparer<string>.Default.GetHashCode(id);
	}
}
