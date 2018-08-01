using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class CombatStructure : Structure {

	public float attackRange;
	public float attackSpeed;
	public int attackDamage;

	public override bool CanAttack { get { return true; } }
}
