﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class PlayerListController : MonoBehaviour {

	public Transform playerListContainer;
	public Text teamPrefab;
	public Text playerPrefab;

	public void OpenWindow() {
		if (ClientGameManager.Instance.GameState != null) {
			foreach (Team team in ClientGameManager.Instance.GameState.Teams.Values) {
				Text teamText = Instantiate<GameObject>(teamPrefab.gameObject, playerListContainer).GetComponent<Text>();
				teamText.text = "Team " + team.ID;
				foreach (Player player in team.Players.Values) {
					Text playerText = Instantiate<GameObject>(playerPrefab.gameObject, playerListContainer).GetComponent<Text>();
					playerText.text = player.Name;
				}
			}
		}

		gameObject.SetActive(true);
	}

	public void CloseWindow() {
		foreach (Transform child in playerListContainer) {
			Destroy(child.gameObject);
		}

		gameObject.SetActive(false);
	}
}
