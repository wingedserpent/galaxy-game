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
				playerData.ID = reader.GetString("external_id");
				playerData.Name = reader.GetString("display_name");
				playerData.MaxSquadCost = reader.GetInt32("max_squad_cost");
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
				"INNER JOIN unit_type u ON u.unit_type_id = pu.unit_type_id " +
				"WHERE p.external_id = @externalPlayerId " +
				"ORDER BY pu.player_unit_id ASC";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				PlayerUnit playerUnit = new PlayerUnit();
				playerUnit.PlayerId = reader.GetString("external_id");
				playerUnit.PlayerUnitId = reader.GetInt32("player_unit_id");
				playerUnit.UnitType = reader.GetString("unit_type");
				playerUnit.UnitName = reader.GetString("unit_display_name");
				playerUnit.SquadCost = reader.GetInt32("squad_cost");
				playerUnit.MaxHealth = reader.GetInt32("health");
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
				weapon.Name = reader.GetString("name");
				weapon.SquadCost = reader.GetInt32("squad_cost");
				weapon.Range = reader.GetFloat("range");
				weapon.Damage = reader.GetInt32("damage");
				weapon.ShieldDamage = reader.GetInt32("shield_damage");
				weapon.AttackRate = reader.GetFloat("attack_rate");
				weapon.SplashRadius = reader.GetFloat("splash_radius");
				weapon.MaxDamage = reader.GetInt32("max_damage");
				weapon.MaxShieldDamage = reader.GetInt32("max_shield_damage");
				weapon.DamageIncreaseRate = reader.GetFloat("damage_increase_rate");
				weapons.Add(weapon);
			}
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
				equipment.EquipmentType = reader.GetString("equipment_type");
				equipment.Name = reader.GetString("name");
				equipment.SquadCost = reader.GetInt32("squad_cost");
				equipment.Health = reader.GetInt32("health");
				equipment.Shield = reader.GetInt32("shield");
				equipment.ShieldRecharge = reader.GetFloat("shield_recharge");
				equipment.MoveSpeed = reader.GetFloat("move_speed");
				equipment.VisionRange = reader.GetFloat("vision_range");
				equipment.Ability = reader.GetString("ability");
				equipments.Add(equipment);
			}
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}

		return equipments;
	}
}
