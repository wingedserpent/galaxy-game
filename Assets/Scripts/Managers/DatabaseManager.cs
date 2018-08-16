using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

public static class DatabaseManager {

	private static string connectionString = "server=127.0.0.1;uid=unity;pwd=password;database=galaxy;SslMode=none";

	private static List<Weapon> structureWeapons;

	public static PlayerData GetPlayerData(string externalPlayerId) {
		PlayerData playerData = new PlayerData();
		
		MySqlConnection connection = new MySqlConnection(connectionString);

		try {
			connection.Open();

			string sql = "SELECT p.* " +
				"FROM player p " +
				"WHERE p.external_id = @externalPlayerId";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				playerData.ID = Convert.IsDBNull(reader["external_id"]) ? null : reader.GetString("external_id");
				playerData.Name = Convert.IsDBNull(reader["display_name"]) ? null : reader.GetString("display_name");
				playerData.MaxSquadCost = Convert.IsDBNull(reader["max_squad_cost"]) ? 0 : reader.GetInt32("max_squad_cost");
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		} finally {
			connection.Close();
		}

		return playerData;
	}

	public static List<PlayerUnit> GetPlayerUnits(string externalPlayerId) {
		List<PlayerUnit> playerUnits = new List<PlayerUnit>();
		Dictionary<string, List<PlayerUnit>> unitsByTypeMap = new Dictionary<string, List<PlayerUnit>>();

		MySqlConnection connection = new MySqlConnection(connectionString);
		try {
			connection.Open();

			string sql = "SELECT pu.player_unit_id, p.*, u.*, u.display_name AS unit_display_name " +
				"FROM player_unit pu " +
				"INNER JOIN player p ON p.player_id = pu.player_id " +
				"INNER JOIN unit_type u ON u.unit_type = pu.unit_type " +
				"WHERE p.external_id = @externalPlayerId " +
				"ORDER BY pu.player_unit_id ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				PlayerUnit playerUnit = new PlayerUnit();
				playerUnit.PlayerId = Convert.IsDBNull(reader["external_id"]) ? null : reader.GetString("external_id");
				playerUnit.PlayerUnitId = Convert.IsDBNull(reader["player_unit_id"]) ? 0 : reader.GetInt32("player_unit_id");
				playerUnit.UnitType = Convert.IsDBNull(reader["unit_type"]) ? null : reader.GetString("unit_type");
				playerUnit.Name = Convert.IsDBNull(reader["unit_display_name"]) ? null : reader.GetString("unit_display_name");
				playerUnit.SquadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				playerUnit.MaxHealth = Convert.IsDBNull(reader["base_health"]) ? 0 : reader.GetInt32("base_health");
				playerUnit.CurrentHealth = playerUnit.MaxHealth;
				playerUnits.Add(playerUnit);

				if (!unitsByTypeMap.ContainsKey(playerUnit.UnitType)) {
					unitsByTypeMap.Add(playerUnit.UnitType, new List<PlayerUnit>());
				}
				unitsByTypeMap[playerUnit.UnitType].Add(playerUnit);
			}
			reader.Close();

			PopulateEquipment(unitsByTypeMap, connection);
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		} finally {
			connection.Close();
		}

		return playerUnits;
	}

	private static void PopulateEquipment(Dictionary<string, List<PlayerUnit>> unitsByTypeMap, MySqlConnection connection) {
		foreach (KeyValuePair<string, List<PlayerUnit>> unitsByType in unitsByTypeMap) {
			List<Weapon> weapons = GetWeapons(unitsByType.Key, connection);
			List<Equipment> equipments = GetEquipment(unitsByType.Key, connection);
			foreach (PlayerUnit playerUnit in unitsByType.Value) {
				playerUnit.WeaponOptions = weapons;
				playerUnit.EquipmentOptions = equipments;
			}
		}
	}

	private static List<Weapon> GetWeapons(string unitType, MySqlConnection connection) {
		List<Weapon> weapons = new List<Weapon>();

		try {
			string sql = "SELECT w.* " +
				"FROM unit_type_weapon w " +
				"WHERE w.unit_type = @unitType " +
				"ORDER BY w.squad_cost ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("unitType", unitType);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				Weapon weapon = new Weapon();
				weapon.Name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
				weapon.WeaponType = Convert.IsDBNull(reader["weapon_type"]) ? null : reader.GetString("weapon_type");
				weapon.SquadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				weapon.Range = Convert.IsDBNull(reader["range"]) ? 0f : reader.GetFloat("range");
				weapon.Damage = Convert.IsDBNull(reader["damage"]) ? 0 : reader.GetInt32("damage");
				weapon.ShieldDamage = Convert.IsDBNull(reader["shield_damage"]) ? 0 : reader.GetInt32("shield_damage");
				weapon.AttackRate = Convert.IsDBNull(reader["attack_rate"]) ? 0f : reader.GetFloat("attack_rate");
				weapon.SplashRadius = Convert.IsDBNull(reader["splash_radius"]) ? 0f : reader.GetFloat("splash_radius");
				weapon.MaxDamage = Convert.IsDBNull(reader["max_damage"]) ? 0 : reader.GetInt32("max_damage");
				weapon.MaxShieldDamage = Convert.IsDBNull(reader["max_shield_damage"]) ? 0 : reader.GetInt32("max_shield_damage");
				weapon.DamageIncreaseRate = Convert.IsDBNull(reader["damage_increase_rate"]) ? 0f : reader.GetFloat("damage_increase_rate");
				weapons.Add(weapon);
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}

		return weapons;
	}

	private static List<Equipment> GetEquipment(string unitType, MySqlConnection connection) {
		List<Equipment> equipments = new List<Equipment>();

		try {
			string sql = "SELECT e.* " +
				"FROM unit_type_equipment e " +
				"WHERE e.unit_type = @unitType " +
				"ORDER BY e.squad_cost ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("unitType", unitType);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				Equipment equipment = new Equipment();
				equipment.Name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
				equipment.EquipmentType = Convert.IsDBNull(reader["equipment_type"]) ? null : reader.GetString("equipment_type");
				equipment.SquadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				equipment.Health = Convert.IsDBNull(reader["health"]) ? 0 : reader.GetInt32("health");
				equipment.Shield = Convert.IsDBNull(reader["shield"]) ? 0 : reader.GetInt32("shield");
				equipment.ShieldRecharge = Convert.IsDBNull(reader["shield_recharge"]) ? 0f : reader.GetFloat("shield_recharge");
				equipment.MoveSpeed = Convert.IsDBNull(reader["move_speed"]) ? 0f : reader.GetFloat("move_speed");
				equipment.VisionRange = Convert.IsDBNull(reader["vision_range"]) ? 0f : reader.GetFloat("vision_range");
				equipment.Ability = Convert.IsDBNull(reader["ability"]) ? null : reader.GetString("ability");
				equipments.Add(equipment);
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}

		return equipments;
	}

	public static StructureData GetStructureData(string structureType) {
		StructureData structureData = new StructureData();
		structureData.StructureType = structureType;

		MySqlConnection connection = new MySqlConnection(connectionString);
		try {
			connection.Open();

			string sql = "SELECT s.* " +
				"FROM structure_type s " +
				"WHERE s.structure_type = @structureType";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("structureType", structureType);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				structureData.Name = Convert.IsDBNull(reader["display_name"]) ? null : reader.GetString("display_name");
				structureData.ResourceCost = Convert.IsDBNull(reader["resource_cost"]) ? 0 : reader.GetInt32("resource_cost");
				structureData.MaxHealth = Convert.IsDBNull(reader["base_health"]) ? 0 : reader.GetInt32("base_health");
				structureData.CurrentHealth = structureData.MaxHealth;
			}
			reader.Close();

			PopulateEquipment(structureData, connection);
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		} finally {
			connection.Close();
		}

		return structureData;
	}

	private static void PopulateEquipment(StructureData structureData, MySqlConnection connection) {
		structureData.WeaponOptions = GetStructureWeapons(structureData.StructureType, connection);
	}

	private static List<Weapon> GetStructureWeapons(string structureType, MySqlConnection connection) {
		if (structureWeapons == null) {
			structureWeapons = new List<Weapon>();
			
			try {
				string sql = "SELECT w.* " +
					"FROM structure_type_weapon w " +
					"WHERE w.structure_type = @structureType " +
					"ORDER BY w.resource_cost ASC";
				MySqlCommand command = new MySqlCommand(sql, connection);
				command.Parameters.AddWithValue("structureType", structureType);
				MySqlDataReader reader = command.ExecuteReader();

				while (reader.Read()) {
					Weapon weapon = new Weapon();
					weapon.Name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
					weapon.WeaponType = Convert.IsDBNull(reader["weapon_type"]) ? null : reader.GetString("weapon_type");
					weapon.SquadCost = Convert.IsDBNull(reader["resource_cost"]) ? 0 : reader.GetInt32("resource_cost");
					weapon.Range = Convert.IsDBNull(reader["range"]) ? 0f : reader.GetFloat("range");
					weapon.Damage = Convert.IsDBNull(reader["damage"]) ? 0 : reader.GetInt32("damage");
					weapon.ShieldDamage = Convert.IsDBNull(reader["shield_damage"]) ? 0 : reader.GetInt32("shield_damage");
					weapon.AttackRate = Convert.IsDBNull(reader["attack_rate"]) ? 0f : reader.GetFloat("attack_rate");
					weapon.SplashRadius = Convert.IsDBNull(reader["splash_radius"]) ? 0f : reader.GetFloat("splash_radius");
					weapon.MaxDamage = Convert.IsDBNull(reader["max_damage"]) ? 0 : reader.GetInt32("max_damage");
					weapon.MaxShieldDamage = Convert.IsDBNull(reader["max_shield_damage"]) ? 0 : reader.GetInt32("max_shield_damage");
					weapon.DamageIncreaseRate = Convert.IsDBNull(reader["damage_increase_rate"]) ? 0f : reader.GetFloat("damage_increase_rate");
					structureWeapons.Add(weapon);
				}
				reader.Close();
			} catch (Exception ex) {
				Debug.LogError(ex.ToString());
			}
		}

		return structureWeapons;
	}
}
