using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class CombatStructure : Structure {

	public override bool CanAttackTarget { get { return Weapon != null; } }
}
