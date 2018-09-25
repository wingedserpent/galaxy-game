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
				"ORDER BY u.squad_cost, u.unit_type ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			MySqlDataReader reader = command.ExecuteReader();

			int playerId = -999;

			while (reader.Read()) {
				playerId = reader.GetInt32("player_id");

				PlayerUnit playerUnit = new PlayerUnit();
				playerUnit.PlayerId = Convert.IsDBNull(reader["external_id"]) ? null : reader.GetString("external_id");
				playerUnit.PlayerUnitId = Convert.IsDBNull(reader["player_unit_id"]) ? 0 : reader.GetInt32("player_unit_id");
				playerUnit.UnitType = Convert.IsDBNull(reader["unit_type"]) ? null : reader.GetString("unit_type");
				playerUnit.Name = Convert.IsDBNull(reader["unit_display_name"]) ? null : reader.GetString("unit_display_name");
				playerUnit.SquadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				playerUnit.MaxHealth = playerUnit.CurrentHealth = Convert.IsDBNull(reader["base_health"]) ? 0 : reader.GetInt32("base_health");
				playerUnit.MaxShield = Convert.IsDBNull(reader["base_shield"]) ? 0 : reader.GetInt32("base_shield");
				playerUnit.MoveSpeed = Convert.IsDBNull(reader["base_speed"]) ? 0f : reader.GetFloat("base_speed");
				playerUnit.VisionRange = Convert.IsDBNull(reader["base_vision"]) ? 0f : reader.GetFloat("base_vision");
				playerUnits.Add(playerUnit);

				if (!unitsByTypeMap.ContainsKey(playerUnit.UnitType)) {
					unitsByTypeMap.Add(playerUnit.UnitType, new List<PlayerUnit>());
				}
				unitsByTypeMap[playerUnit.UnitType].Add(playerUnit);
			}
			reader.Close();

			PopulateEquipment(playerId, playerUnits, connection);
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		} finally {
			connection.Close();
		}

		return playerUnits;
	}

	private static void PopulateEquipment(int playerId, List<PlayerUnit> playerUnits, MySqlConnection connection) {
		foreach (PlayerUnit playerUnit in playerUnits) {
			List<Weapon> weapons = GetWeapons(playerId, playerUnit.PlayerUnitId, connection);
			List<Equipment> equipments = GetEquipment(playerId, playerUnit.PlayerUnitId, connection);
			playerUnit.WeaponOptions = weapons;
			playerUnit.EquipmentOptions = equipments;
		}
	}

	private static List<Weapon> GetWeapons(int playerId, int playerUnitId, MySqlConnection connection) {
		List<Weapon> weapons = new List<Weapon>();

		try {
			string sql = "SELECT w.*, puw.player_unit_id " +
				"FROM player_unit pu " +
				"INNER JOIN unit_type_weapon w ON w.unit_type = pu.unit_type " +
				"LEFT OUTER JOIN player_unit_weapon puw ON puw.player_id = pu.player_id AND puw.player_unit_id = pu.player_unit_id AND puw.weapon_name = w.name " +
				"WHERE pu.player_id = @playerId AND pu.player_unit_id = @playerUnitId " +
				"ORDER BY w.squad_cost ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("playerId", playerId);
			command.Parameters.AddWithValue("playerUnitId", playerUnitId);
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
				weapon.DamageIncreaseTime = Convert.IsDBNull(reader["damage_increase_time"]) ? 0f : reader.GetFloat("damage_increase_time");
				weapon.IsEquipped = Convert.IsDBNull(reader["player_unit_id"]) ? false : true;
				weapons.Add(weapon);
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}

		return weapons;
	}

	private static List<Equipment> GetEquipment(int playerId, int playerUnitId, MySqlConnection connection) {
		List<Equipment> equipments = new List<Equipment>();

		try {
			string sql = "SELECT e.*, pue.player_unit_id " +
				"FROM player_unit pu " +
				"INNER JOIN unit_type_equipment e ON e.unit_type = pu.unit_type " +
				"LEFT OUTER JOIN player_unit_equipment pue ON pue.player_id = pu.player_id AND pue.player_unit_id = pu.player_unit_id AND pue.equipment_name = e.name " +
				"WHERE pu.player_id = @playerId AND pu.player_unit_id = @playerUnitId " +
				"ORDER BY e.squad_cost ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("playerId", playerId);
			command.Parameters.AddWithValue("playerUnitId", playerUnitId);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				Equipment equipment = new Equipment();
				equipment.Name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
				equipment.EquipmentType = Convert.IsDBNull(reader["equipment_type"]) ? null : reader.GetString("equipment_type");
				equipment.SquadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				equipment.Health = Convert.IsDBNull(reader["health"]) ? 0 : reader.GetInt32("health");
				equipment.Shield = Convert.IsDBNull(reader["shield"]) ? 0 : reader.GetInt32("shield");
				equipment.ShieldRechargeRate = Convert.IsDBNull(reader["shield_recharge_rate"]) ? 0 : reader.GetInt32("shield_recharge_rate");
				equipment.MoveSpeed = Convert.IsDBNull(reader["move_speed"]) ? 0f : reader.GetFloat("move_speed");
				equipment.VisionRange = Convert.IsDBNull(reader["vision_range"]) ? 0f : reader.GetFloat("vision_range");
				equipment.Ability = Convert.IsDBNull(reader["ability"]) ? null : reader.GetString("ability");
				equipment.IsEquipped = Convert.IsDBNull(reader["player_unit_id"]) ? false : true;
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
		List<Weapon> structureWeapons = new List<Weapon>();
			
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
				weapon.DamageIncreaseTime = Convert.IsDBNull(reader["damage_increase_time"]) ? 0f : reader.GetFloat("damage_increase_time");
				structureWeapons.Add(weapon);
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}

		return structureWeapons;
	}

	public static void SavePlayerUnit(string externalPlayerId, SelectedPlayerUnit selectedPlayerUnit) {
		MySqlConnection connection = new MySqlConnection(connectionString);
		try {
			connection.Open();

			string sql = "INSERT INTO player_unit (player_id, player_unit_id, unit_type, name) " +
				"VALUES ((SELECT player_id FROM player WHERE external_id = @externalPlayerId), @playerUnitId, @unitType, @name) " +
				"ON DUPLICATE KEY UPDATE " +
				"name = @name";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.PlayerUnitId);
			command.Parameters.AddWithValue("unitType", selectedPlayerUnit.UnitType);
			command.Parameters.AddWithValue("name", selectedPlayerUnit.UnitType);
			int returnVal = command.ExecuteNonQuery();

			SaveWeapons(externalPlayerId, selectedPlayerUnit, connection);
			SaveEquipment(externalPlayerId, selectedPlayerUnit, connection);
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}

	private static void SaveWeapons(string externalPlayerId, SelectedPlayerUnit selectedPlayerUnit, MySqlConnection connection) {
		try {
			string sql = "INSERT INTO player_unit_weapon (player_id, player_unit_id, weapon_name) " +
				"VALUES ((SELECT player_id FROM player WHERE external_id = @externalPlayerId), @playerUnitId, @weaponName) " +
				"ON DUPLICATE KEY UPDATE " +
				"weapon_name = @weaponName";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.PlayerUnitId);
			command.Parameters.AddWithValue("weaponName", selectedPlayerUnit.WeaponSelection);
			int returnVal = command.ExecuteNonQuery();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}

	private static void SaveEquipment(string externalPlayerId, SelectedPlayerUnit selectedPlayerUnit, MySqlConnection connection) {
		try {
			string deleteSql = "DELETE FROM player_unit_equipment " +
				"WHERE player_id = (SELECT player_id FROM player WHERE external_id = @externalPlayerId) " +
				"AND player_unit_id = @playerUnitId";
			MySqlCommand command = new MySqlCommand(deleteSql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.PlayerUnitId);
			int returnVal = command.ExecuteNonQuery();

			foreach (string weaponName in selectedPlayerUnit.EquipmentSelections) {
				string sql = "INSERT INTO player_unit_equipment (player_id, player_unit_id, equipment_name) " +
					"VALUES ((SELECT player_id FROM player WHERE external_id = @externalPlayerId), @playerUnitId, @equipmentName) " +
					"ON DUPLICATE KEY UPDATE " +
					"equipment_name = @equipmentName";
				command = new MySqlCommand(sql, connection);
				command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
				command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.PlayerUnitId);
				command.Parameters.AddWithValue("equipmentName", weaponName);
				returnVal = command.ExecuteNonQuery();
			}
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}
}
