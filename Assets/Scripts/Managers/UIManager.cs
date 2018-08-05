using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager> {

	public ConnectionMenuController connectionMenuController;
	public SquadMenuController squadMenuController;
	public ChatWindowController chatWindowController;
	public Text scoreDisplay;
	public Text resourceDisplay;

	public bool IsUIReceivingInput { get; private set; }

	private ClientGameManager clientGamaManager;

	protected override void Awake() {
		base.Awake();

		IsUIReceivingInput = false;
	}

	private void Start() {
		clientGamaManager = ClientGameManager.Instance;

		OpenConnectionMenu();
	}

	public void OpenConnectionMenu() {
		IsUIReceivingInput = true;
		connectionMenuController.OpenMenu();
	}

	public void OnConnectionMenuClosed() {
		IsUIReceivingInput = false;
	}

	public void OpenSquadMenu(int maxSquadCost = 0) {
		IsUIReceivingInput = true;
		squadMenuController.OpenMenu(maxSquadCost);
	}

	public void OnUnitListReceived(List<PlayerUnit> playerUnits) {
		squadMenuController.OnUnitListReceived(playerUnits);
	}

	public void OnSquadMenuClosed() {
		IsUIReceivingInput = false;
		//OpenChatWindow();
	}

	public void UpdateDisplays() {
		scoreDisplay.text = "Score:\n";
		foreach (Team team in clientGamaManager.GameState.Teams.Values) {
			scoreDisplay.text += team.Name + ": " + team.Score + "\n";
		}

		resourceDisplay.text = "Resources:\n" + clientGamaManager.MyPlayer.Resources;
	}

	public void OpenChatWindow() {
		chatWindowController.OpenWindow();
	}

	public void AddChatMessage(Player messagingPlayer, string messageText) {
		chatWindowController.AddChatMessage(messagingPlayer, messageText);
	}

	public void AddSystemMessage(string messageText) {
		chatWindowController.AddSystemMessage(messageText);
		Debug.Log(messageText);
	}

	public void OnChatInputFocusChange(bool isFocused) {
		IsUIReceivingInput = isFocused;
	}
}
