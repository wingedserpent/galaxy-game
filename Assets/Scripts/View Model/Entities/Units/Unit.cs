using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Unit : Entity {

	public int playerUnitId { get; set; }

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		playerUnitId = e.Reader.ReadInt32();
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(playerUnitId);
	}
}
