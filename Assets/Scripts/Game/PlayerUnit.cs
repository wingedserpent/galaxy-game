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
	public string UnitName { get; set; }
	public int SquadCost { get; set; }
	public int MaxHealth { get; set; }
	public int CurrentHealth { get; set; }

	public void Deserialize(DeserializeEvent e) {
		PlayerId = e.Reader.ReadString();
		PlayerUnitId = e.Reader.ReadInt32();
		UnitType = e.Reader.ReadString();
		UnitName = e.Reader.ReadString();
		SquadCost = e.Reader.ReadInt32();
		MaxHealth = e.Reader.ReadInt32();
		CurrentHealth = e.Reader.ReadInt32();
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(PlayerId);
		e.Writer.Write(PlayerUnitId);
		e.Writer.Write(UnitType);
		e.Writer.Write(UnitName);
		e.Writer.Write(SquadCost);
		e.Writer.Write(MaxHealth);
		e.Writer.Write(CurrentHealth);
	}
}
