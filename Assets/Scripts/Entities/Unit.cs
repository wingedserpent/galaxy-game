using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Unit : Entity {

	public override bool CanMove { get { return true; } }
	public override bool CanAttackTarget { get { return Weapon != null; } }

	public int PlayerUnitId { get; set; }

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		PlayerUnitId = e.Reader.ReadInt32();
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(PlayerUnitId);
	}
}
