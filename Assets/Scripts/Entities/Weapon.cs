using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : IDarkRiftSerializable {

	public string Name { get; set; }
	public string WeaponType { get; set; }
	public int SquadCost { get; set; }
	public float Range { get; set; }
	public int Damage { get; set; }
	public int ShieldDamage { get; set; }
	public float AttackRate { get; set; }
	public float SplashRadius { get; set; }
	public int MaxDamage { get; set; }
	public int MaxShieldDamage { get; set; }
	public float DamageIncreaseTime { get; set; }
	public bool IsEquipped { get; set; }

	public void Deserialize(DeserializeEvent e) {
		Name = e.Reader.ReadString();
		WeaponType = e.Reader.ReadString();
		SquadCost = e.Reader.ReadInt32();
		Range = e.Reader.ReadSingle();
		Damage = e.Reader.ReadInt32();
		ShieldDamage = e.Reader.ReadInt32();
		AttackRate = e.Reader.ReadSingle();
		SplashRadius = e.Reader.ReadSingle();
		MaxDamage = e.Reader.ReadInt32();
		MaxShieldDamage = e.Reader.ReadInt32();
		DamageIncreaseTime = e.Reader.ReadSingle();
		IsEquipped = e.Reader.ReadBoolean();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(Name);
		e.Writer.Write(WeaponType);
		e.Writer.Write(SquadCost);
		e.Writer.Write(Range);
		e.Writer.Write(Damage);
		e.Writer.Write(ShieldDamage);
		e.Writer.Write(AttackRate);
		e.Writer.Write(SplashRadius);
		e.Writer.Write(MaxDamage);
		e.Writer.Write(MaxShieldDamage);
		e.Writer.Write(DamageIncreaseTime);
		e.Writer.Write(IsEquipped);
	}
}
