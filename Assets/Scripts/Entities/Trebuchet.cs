using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Trebuchet : Unit {

	public override bool CanAttackTarget { get { return false; } }
	public override bool CanAttackLocation { get { return Weapon != null; } }
}
