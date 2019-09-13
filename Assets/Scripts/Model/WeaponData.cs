using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponData : IDarkRiftSerializable {

	public string name { get; set; }
	public string typeId { get; set; }
	public int squadCost { get; set; }
	public float range { get; set; }
	public int damage { get; set; }
	public int shieldDamage { get; set; }
	public float attackRate { get; set; }
	public float splashRadius { get; set; }
	public int maxDamage { get; set; }
	public int maxShieldDamage { get; set; }
	public float damageIncreaseTime { get; set; }
	public bool isEquipped { get; set; }

	public void Deserialize(DeserializeEvent e) {
		name = e.Reader.ReadString();
		typeId = e.Reader.ReadString();
		squadCost = e.Reader.ReadInt32();
		range = e.Reader.ReadSingle();
		damage = e.Reader.ReadInt32();
		shieldDamage = e.Reader.ReadInt32();
		attackRate = e.Reader.ReadSingle();
		splashRadius = e.Reader.ReadSingle();
		maxDamage = e.Reader.ReadInt32();
		maxShieldDamage = e.Reader.ReadInt32();
		damageIncreaseTime = e.Reader.ReadSingle();
		isEquipped = e.Reader.ReadBoolean();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(name);
		e.Writer.Write(typeId);
		e.Writer.Write(squadCost);
		e.Writer.Write(range);
		e.Writer.Write(damage);
		e.Writer.Write(shieldDamage);
		e.Writer.Write(attackRate);
		e.Writer.Write(splashRadius);
		e.Writer.Write(maxDamage);
		e.Writer.Write(maxShieldDamage);
		e.Writer.Write(damageIncreaseTime);
		e.Writer.Write(isEquipped);
	}
}
