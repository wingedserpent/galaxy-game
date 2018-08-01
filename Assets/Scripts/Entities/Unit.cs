using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Unit : Entity {

	public float attackRange;
	public float attackSpeed;
	public int attackDamage;

	public override bool CanMove { get { return true; } }
	public override bool CanAttack { get { return true; } }

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
