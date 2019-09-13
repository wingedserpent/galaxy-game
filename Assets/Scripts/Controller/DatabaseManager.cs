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
				playerUnit.playerId = Convert.IsDBNull(reader["external_id"]) ? null : reader.GetString("external_id");
				playerUnit.playerUnitId = Convert.IsDBNull(reader["player_unit_id"]) ? 0 : reader.GetInt32("player_unit_id");
				playerUnit.unitType = Convert.IsDBNull(reader["unit_type"]) ? null : reader.GetString("unit_type");
				playerUnit.name = Convert.IsDBNull(reader["unit_display_name"]) ? null : reader.GetString("unit_display_name");
				playerUnit.squadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				playerUnit.maxHealth = playerUnit.currentHealth = Convert.IsDBNull(reader["base_health"]) ? 0 : reader.GetInt32("base_health");
				playerUnit.maxShield = Convert.IsDBNull(reader["base_shield"]) ? 0 : reader.GetInt32("base_shield");
				playerUnit.moveSpeed = Convert.IsDBNull(reader["base_speed"]) ? 0f : reader.GetFloat("base_speed");
				playerUnit.visionRange = Convert.IsDBNull(reader["base_vision"]) ? 0f : reader.GetFloat("base_vision");
				playerUnits.Add(playerUnit);

				if (!unitsByTypeMap.ContainsKey(playerUnit.unitType)) {
					unitsByTypeMap.Add(playerUnit.unitType, new List<PlayerUnit>());
				}
				unitsByTypeMap[playerUnit.unitType].Add(playerUnit);
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
			List<WeaponData> weapons = GetWeapons(playerId, playerUnit.playerUnitId, connection);
			List<EquipmentData> equipments = GetEquipment(playerId, playerUnit.playerUnitId, connection);
			playerUnit.weaponOptions = weapons;
			playerUnit.equipmentOptions = equipments;
		}
	}

	private static List<WeaponData> GetWeapons(int playerId, int playerUnitId, MySqlConnection connection) {
		List<WeaponData> weapons = new List<WeaponData>();

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
				WeaponData weapon = new WeaponData();
				weapon.name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
				weapon.typeId = Convert.IsDBNull(reader["weapon_type"]) ? null : reader.GetString("weapon_type");
				weapon.squadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				weapon.range = Convert.IsDBNull(reader["range"]) ? 0f : reader.GetFloat("range");
				weapon.damage = Convert.IsDBNull(reader["damage"]) ? 0 : reader.GetInt32("damage");
				weapon.shieldDamage = Convert.IsDBNull(reader["shield_damage"]) ? 0 : reader.GetInt32("shield_damage");
				weapon.attackRate = Convert.IsDBNull(reader["attack_rate"]) ? 0f : reader.GetFloat("attack_rate");
				weapon.splashRadius = Convert.IsDBNull(reader["splash_radius"]) ? 0f : reader.GetFloat("splash_radius");
				weapon.maxDamage = Convert.IsDBNull(reader["max_damage"]) ? 0 : reader.GetInt32("max_damage");
				weapon.maxShieldDamage = Convert.IsDBNull(reader["max_shield_damage"]) ? 0 : reader.GetInt32("max_shield_damage");
				weapon.damageIncreaseTime = Convert.IsDBNull(reader["damage_increase_time"]) ? 0f : reader.GetFloat("damage_increase_time");
				weapon.isEquipped = Convert.IsDBNull(reader["player_unit_id"]) ? false : true;
				weapons.Add(weapon);
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}

		return weapons;
	}

	private static List<EquipmentData> GetEquipment(int playerId, int playerUnitId, MySqlConnection connection) {
		List<EquipmentData> equipments = new List<EquipmentData>();

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
				EquipmentData equipment = new EquipmentData();
				equipment.name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
				equipment.equipmentType = Convert.IsDBNull(reader["equipment_type"]) ? null : reader.GetString("equipment_type");
				equipment.squadCost = Convert.IsDBNull(reader["squad_cost"]) ? 0 : reader.GetInt32("squad_cost");
				equipment.health = Convert.IsDBNull(reader["health"]) ? 0 : reader.GetInt32("health");
				equipment.shield = Convert.IsDBNull(reader["shield"]) ? 0 : reader.GetInt32("shield");
				equipment.shieldRechargeRate = Convert.IsDBNull(reader["shield_recharge_rate"]) ? 0 : reader.GetInt32("shield_recharge_rate");
				equipment.moveSpeed = Convert.IsDBNull(reader["move_speed"]) ? 0f : reader.GetFloat("move_speed");
				equipment.visionRange = Convert.IsDBNull(reader["vision_range"]) ? 0f : reader.GetFloat("vision_range");
				equipment.ability = Convert.IsDBNull(reader["ability"]) ? null : reader.GetString("ability");
				equipment.isEquipped = Convert.IsDBNull(reader["player_unit_id"]) ? false : true;
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
		structureData.structureType = structureType;

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
				structureData.name = Convert.IsDBNull(reader["display_name"]) ? null : reader.GetString("display_name");
				structureData.resourceCost = Convert.IsDBNull(reader["resource_cost"]) ? 0 : reader.GetInt32("resource_cost");
				structureData.maxHealth = Convert.IsDBNull(reader["base_health"]) ? 0 : reader.GetInt32("base_health");
				structureData.maxShield = Convert.IsDBNull(reader["base_shield"]) ? 0 : reader.GetInt32("base_shield");
				structureData.visionRange = Convert.IsDBNull(reader["base_vision"]) ? 0f : reader.GetFloat("base_vision");
				structureData.currentHealth = structureData.maxHealth;
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
		structureData.weaponOptions = GetStructureWeapons(structureData.structureType, connection);
	}

	private static List<WeaponData> GetStructureWeapons(string structureType, MySqlConnection connection) {
		List<WeaponData> structureWeapons = new List<WeaponData>();
			
		try {
			string sql = "SELECT w.* " +
				"FROM structure_type_weapon w " +
				"WHERE w.structure_type = @structureType " +
				"ORDER BY w.resource_cost ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("structureType", structureType);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				WeaponData weapon = new WeaponData();
				weapon.name = Convert.IsDBNull(reader["name"]) ? null : reader.GetString("name");
				weapon.typeId = Convert.IsDBNull(reader["weapon_type"]) ? null : reader.GetString("weapon_type");
				weapon.squadCost = Convert.IsDBNull(reader["resource_cost"]) ? 0 : reader.GetInt32("resource_cost");
				weapon.range = Convert.IsDBNull(reader["range"]) ? 0f : reader.GetFloat("range");
				weapon.damage = Convert.IsDBNull(reader["damage"]) ? 0 : reader.GetInt32("damage");
				weapon.shieldDamage = Convert.IsDBNull(reader["shield_damage"]) ? 0 : reader.GetInt32("shield_damage");
				weapon.attackRate = Convert.IsDBNull(reader["attack_rate"]) ? 0f : reader.GetFloat("attack_rate");
				weapon.splashRadius = Convert.IsDBNull(reader["splash_radius"]) ? 0f : reader.GetFloat("splash_radius");
				weapon.maxDamage = Convert.IsDBNull(reader["max_damage"]) ? 0 : reader.GetInt32("max_damage");
				weapon.maxShieldDamage = Convert.IsDBNull(reader["max_shield_damage"]) ? 0 : reader.GetInt32("max_shield_damage");
				weapon.damageIncreaseTime = Convert.IsDBNull(reader["damage_increase_time"]) ? 0f : reader.GetFloat("damage_increase_time");
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
			command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.playerUnitId);
			command.Parameters.AddWithValue("unitType", selectedPlayerUnit.unitType);
			command.Parameters.AddWithValue("name", selectedPlayerUnit.unitType);
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
			command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.playerUnitId);
			command.Parameters.AddWithValue("weaponName", selectedPlayerUnit.weaponSelection);
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
			command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.playerUnitId);
			int returnVal = command.ExecuteNonQuery();

			foreach (string weaponName in selectedPlayerUnit.equipmentSelections) {
				string sql = "INSERT INTO player_unit_equipment (player_id, player_unit_id, equipment_name) " +
					"VALUES ((SELECT player_id FROM player WHERE external_id = @externalPlayerId), @playerUnitId, @equipmentName) " +
					"ON DUPLICATE KEY UPDATE " +
					"equipment_name = @equipmentName";
				command = new MySqlCommand(sql, connection);
				command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
				command.Parameters.AddWithValue("playerUnitId", selectedPlayerUnit.playerUnitId);
				command.Parameters.AddWithValue("equipmentName", weaponName);
				returnVal = command.ExecuteNonQuery();
			}
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}
}
