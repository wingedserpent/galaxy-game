using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using DarkRift;

public class StructureData : IDarkRiftSerializable {
	
	public string structureType { get; set; }
	public string name { get; set; }
	public int resourceCost { get; set; }
	public int maxHealth { get; set; }
	public int currentHealth { get; set; }
	public int maxShield { get; set; }
	public float visionRange { get; set; }
	public List<WeaponData> weaponOptions { get; set; }
	public List<EquipmentData> equipmentOptions { get; set; }

	public StructureData() {
		weaponOptions = new List<WeaponData>();
		equipmentOptions = new List<EquipmentData>();
	}

	public void Deserialize(DeserializeEvent e) {
		structureType = e.Reader.ReadString();
		name = e.Reader.ReadString();
		resourceCost = e.Reader.ReadInt32();
		maxHealth = e.Reader.ReadInt32();
		currentHealth = e.Reader.ReadInt32();
		maxShield = e.Reader.ReadInt32();
		visionRange = e.Reader.ReadSingle();

		weaponOptions.Clear();
		int numWeaponOptions = e.Reader.ReadInt32();
		for (int i = 0; i < numWeaponOptions; i++) {
			weaponOptions.Add(e.Reader.ReadSerializable<WeaponData>());
		}

		equipmentOptions.Clear();
		int numEquipmentOptions = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipmentOptions; i++) {
			equipmentOptions.Add(e.Reader.ReadSerializable<EquipmentData>());
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(structureType);
		e.Writer.Write(name);
		e.Writer.Write(resourceCost);
		e.Writer.Write(maxHealth);
		e.Writer.Write(currentHealth);
		e.Writer.Write(maxShield);
		e.Writer.Write(visionRange);

		e.Writer.Write(weaponOptions.Count);
		foreach (WeaponData weaponOption in weaponOptions) {
			e.Writer.Write(weaponOption);
		}

		e.Writer.Write(equipmentOptions.Count);
		foreach (EquipmentData equipmentOption in equipmentOptions) {
			e.Writer.Write(equipmentOption);
		}
	}
}
