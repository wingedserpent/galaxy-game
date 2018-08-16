using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equipment : IDarkRiftSerializable {
	
	public string Name { get; set; }
	public string EquipmentType { get; set; }
	public int SquadCost { get; set; }
	public int Health { get; set; }
	public int Shield { get; set; }
	public float ShieldRecharge { get; set; }
	public float MoveSpeed { get; set; }
	public float VisionRange { get; set; }
	public string Ability { get; set; }

	public void Deserialize(DeserializeEvent e) {
		Name = e.Reader.ReadString();
		EquipmentType = e.Reader.ReadString();
		SquadCost = e.Reader.ReadInt32();
		Health = e.Reader.ReadInt32();
		Shield = e.Reader.ReadInt32();
		ShieldRecharge = e.Reader.ReadSingle();
		MoveSpeed = e.Reader.ReadSingle();
		VisionRange = e.Reader.ReadSingle();
		Ability = e.Reader.ReadString();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(Name);
		e.Writer.Write(EquipmentType);
		e.Writer.Write(SquadCost);
		e.Writer.Write(Health);
		e.Writer.Write(Shield);
		e.Writer.Write(ShieldRecharge);
		e.Writer.Write(MoveSpeed);
		e.Writer.Write(VisionRange);
		e.Writer.Write(Ability);
	}
}
