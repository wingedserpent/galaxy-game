using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;

public static class DatabaseManager {

	private static string connectionString = "server=127.0.0.1;uid=unity;pwd=password;database=galaxy;";

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

		MySqlConnection connection = new MySqlConnection(connectionString);
		try {
			connection.Open();

			string sql = "SELECT pu.player_unit_id, p.*, u.*, u.display_name AS unit_display_name " +
				"FROM player_unit pu " +
				"INNER JOIN player p ON p.player_id = pu.player_id " +
				"INNER JOIN unit_type u ON u.unit_type_id = pu.unit_type_id " +
				"WHERE p.external_id = @externalPlayerId";
			MySqlCommand command = new MySqlCommand(sql, connection);
			command.Parameters.AddWithValue("externalPlayerId", externalPlayerId);
			MySqlDataReader reader = command.ExecuteReader();

			while (reader.Read()) {
				PlayerUnit playerUnit = new PlayerUnit();
				playerUnit.PlayerId = reader.GetString("external_id");
				playerUnit.UnitId = reader.GetInt32("player_unit_id");
				playerUnit.UnitType = reader.GetString("unit_type");
				playerUnit.UnitName = reader.GetString("unit_display_name");
				playerUnit.SquadCost = reader.GetInt32("squad_cost");
				playerUnit.MaxHealth = reader.GetInt32("health");
				playerUnit.CurrentHealth = playerUnit.MaxHealth;
				playerUnits.Add(playerUnit);
			}
			reader.Close();
		} catch (Exception ex) {
			Debug.LogError(ex.ToString());
		} finally {
			connection.Close();
		}

		return playerUnits;
	}
}
