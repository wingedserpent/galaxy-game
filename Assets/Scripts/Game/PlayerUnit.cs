using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using DarkRift;

public class PlayerUnit : IDarkRiftSerializable {

	public string PlayerId { get; set; }
	public int PlayerUnitId { get; set; }
	public string UnitType { get; set; }
	public string Name { get; set; }
	public int SquadCost { get; set; }
	public int MaxHealth { get; set; }
	public int CurrentHealth { get; set; }
	public int MaxShield { get; set; }
	public float MoveSpeed { get; set; }
	public float VisionRange { get; set; }
	public List<Weapon> WeaponOptions { get; set; }
	public List<Equipment> EquipmentOptions { get; set; }

	public PlayerUnit() {
		WeaponOptions = new List<Weapon>();
		EquipmentOptions = new List<Equipment>();
	}

	public void Deserialize(DeserializeEvent e) {
		PlayerId = e.Reader.ReadString();
		PlayerUnitId = e.Reader.ReadInt32();
		UnitType = e.Reader.ReadString();
		Name = e.Reader.ReadString();
		SquadCost = e.Reader.ReadInt32();
		MaxHealth = e.Reader.ReadInt32();
		CurrentHealth = e.Reader.ReadInt32();
		MaxShield = e.Reader.ReadInt32();
		MoveSpeed = e.Reader.ReadSingle();
		VisionRange = e.Reader.ReadSingle();

		int numWeaponOptions = e.Reader.ReadInt32();
		for (int i=0; i < numWeaponOptions; i++) {
			WeaponOptions.Add(e.Reader.ReadSerializable<Weapon>());
		}
		int numEquipmentOptions = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipmentOptions; i++) {
			EquipmentOptions.Add(e.Reader.ReadSerializable<Equipment>());
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(PlayerId);
		e.Writer.Write(PlayerUnitId);
		e.Writer.Write(UnitType);
		e.Writer.Write(Name);
		e.Writer.Write(SquadCost);
		e.Writer.Write(MaxHealth);
		e.Writer.Write(CurrentHealth);
		e.Writer.Write(MaxShield);
		e.Writer.Write(MoveSpeed);
		e.Writer.Write(VisionRange);

		e.Writer.Write(WeaponOptions.Count);
		foreach (Weapon weaponOption in WeaponOptions) {
			e.Writer.Write(weaponOption);
		}
		e.Writer.Write(EquipmentOptions.Count);
		foreach (Equipment equipmentOption in EquipmentOptions) {
			e.Writer.Write(equipmentOption);
		}
	}
}
