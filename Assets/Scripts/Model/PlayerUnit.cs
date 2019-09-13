using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using DarkRift;

public class PlayerUnit : IDarkRiftSerializable {

	public string playerId { get; set; }
	public int playerUnitId { get; set; }
	public string unitType { get; set; }
	public string name { get; set; }
	public int squadCost { get; set; }
	public int maxHealth { get; set; }
	public int currentHealth { get; set; }
	public int maxShield { get; set; }
	public float moveSpeed { get; set; }
	public float visionRange { get; set; }
	public List<WeaponData> weaponOptions { get; set; }
	public List<EquipmentData> equipmentOptions { get; set; }

	public PlayerUnit() {
		weaponOptions = new List<WeaponData>();
		equipmentOptions = new List<EquipmentData>();
	}

	public void Deserialize(DeserializeEvent e) {
		playerId = e.Reader.ReadString();
		playerUnitId = e.Reader.ReadInt32();
		unitType = e.Reader.ReadString();
		name = e.Reader.ReadString();
		squadCost = e.Reader.ReadInt32();
		maxHealth = e.Reader.ReadInt32();
		currentHealth = e.Reader.ReadInt32();
		maxShield = e.Reader.ReadInt32();
		moveSpeed = e.Reader.ReadSingle();
		visionRange = e.Reader.ReadSingle();

		weaponOptions.Clear();
		int numWeaponOptions = e.Reader.ReadInt32();
		for (int i=0; i < numWeaponOptions; i++) {
			weaponOptions.Add(e.Reader.ReadSerializable<WeaponData>());
		}

		equipmentOptions.Clear();
		int numEquipmentOptions = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipmentOptions; i++) {
			equipmentOptions.Add(e.Reader.ReadSerializable<EquipmentData>());
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(playerId);
		e.Writer.Write(playerUnitId);
		e.Writer.Write(unitType);
		e.Writer.Write(name);
		e.Writer.Write(squadCost);
		e.Writer.Write(maxHealth);
		e.Writer.Write(currentHealth);
		e.Writer.Write(maxShield);
		e.Writer.Write(moveSpeed);
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
