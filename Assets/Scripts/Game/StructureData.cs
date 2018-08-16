using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using DarkRift;

public class StructureData : IDarkRiftSerializable {
	
	public string StructureType { get; set; }
	public string Name { get; set; }
	public int ResourceCost { get; set; }
	public int MaxHealth { get; set; }
	public int CurrentHealth { get; set; }
	public List<Weapon> WeaponOptions { get; set; }
	public List<Equipment> EquipmentOptions { get; set; }

	public StructureData() {
		WeaponOptions = new List<Weapon>();
		EquipmentOptions = new List<Equipment>();
	}

	public void Deserialize(DeserializeEvent e) {
		StructureType = e.Reader.ReadString();
		Name = e.Reader.ReadString();
		ResourceCost = e.Reader.ReadInt32();
		MaxHealth = e.Reader.ReadInt32();
		CurrentHealth = e.Reader.ReadInt32();

		int numWeaponOptions = e.Reader.ReadInt32();
		for (int i = 0; i < numWeaponOptions; i++) {
			WeaponOptions.Add(e.Reader.ReadSerializable<Weapon>());
		}
		int numEquipmentOptions = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipmentOptions; i++) {
			EquipmentOptions.Add(e.Reader.ReadSerializable<Equipment>());
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(StructureType);
		e.Writer.Write(Name);
		e.Writer.Write(ResourceCost);
		e.Writer.Write(MaxHealth);
		e.Writer.Write(CurrentHealth);

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
