using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentData : IDarkRiftSerializable {
	
	public string name { get; set; }
	public string equipmentType { get; set; }
	public int squadCost { get; set; }
	public int health { get; set; }
	public int shield { get; set; }
	public int shieldRechargeRate { get; set; }
	public float moveSpeed { get; set; }
	public float visionRange { get; set; }
	public string ability { get; set; }
	public bool isEquipped { get; set; }

	public void Deserialize(DeserializeEvent e) {
		name = e.Reader.ReadString();
		equipmentType = e.Reader.ReadString();
		squadCost = e.Reader.ReadInt32();
		health = e.Reader.ReadInt32();
		shield = e.Reader.ReadInt32();
		shieldRechargeRate = e.Reader.ReadInt32();
		moveSpeed = e.Reader.ReadSingle();
		visionRange = e.Reader.ReadSingle();
		ability = e.Reader.ReadString();
		ability = (ability.Length == 0) ? null : ability;
		isEquipped = e.Reader.ReadBoolean();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(name);
		e.Writer.Write(equipmentType);
		e.Writer.Write(squadCost);
		e.Writer.Write(health);
		e.Writer.Write(shield);
		e.Writer.Write(shieldRechargeRate);
		e.Writer.Write(moveSpeed);
		e.Writer.Write(visionRange);
		e.Writer.Write(ability ?? "");
		e.Writer.Write(isEquipped);
	}
}
