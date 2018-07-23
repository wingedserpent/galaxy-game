using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : IDarkRiftSerializable {

	public string ID { get; private set; }
	public ushort TeamId { get; set; }
	public string Name { get; private set; }
	public int MaxSquadCost { get; set; }

	public Player() { }

	public Player(string id, string name) {
		ID = id;
		Name = name;
	}

	public void Deserialize(DeserializeEvent e) {
		ID = e.Reader.ReadString();
		TeamId = e.Reader.ReadUInt16();
		Name = e.Reader.ReadString();
		MaxSquadCost = e.Reader.ReadInt32();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(ID);
		e.Writer.Write(TeamId);
		e.Writer.Write(Name);
		e.Writer.Write(MaxSquadCost);
	}

	public override bool Equals(object obj) {
		var player = obj as Player;
		return player != null &&
			   ID == player.ID;
	}

	public override int GetHashCode() {
		return 1213502048 + EqualityComparer<string>.Default.GetHashCode(ID);
	}
}
