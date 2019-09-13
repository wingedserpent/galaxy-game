using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager> {

	public PlayerInputManager playerInputManager;
	public ConnectionMenuController connectionMenuController;
	public SquadMenuController squadMenuController;
	public CustomizationMenuController customizationMenuController;
	public InfoWindowController infoWindowController;
	public ChatWindowController chatWindowController;
	public PlayerListController playerListController;
	public Text scoreDisplay;
	public Text resourceDisplay;
	public RectTransform entityHudContainer;

	public bool IsUIReceivingInput { get; private set; }

	private ClientGameManager clientGameManager;

	protected override void Awake() {
		base.Awake();

		IsUIReceivingInput = false;
	}

	private void Start() {
		clientGameManager = ClientGameManager.Instance;

		if (!ClientNetworkManager.Instance.IsConnectedToServer) {
			OpenConnectionMenu();
		}
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
		squadMenuController.OpenMenu();
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
		foreach (Team team in clientGameManager.GameState.teams.Values) {
			scoreDisplay.text += team.name + ": " + team.score + "\n";
		}

		resourceDisplay.text = "Resources:\n" + clientGameManager.MyPlayer.resources;
	}

	public void OpenInfoWindow() {
		infoWindowController.OpenWindow();
	}

	public void UpdateSelectedEntities(List<Entity> selectedEntities) {
		infoWindowController.UpdateSelectedEntities(selectedEntities);
	}

	public void OnEntityPortraitClick(Entity entity) {
		playerInputManager.ForceSelectedEntity(entity);
	}

	public void OpenBuildMenu(List<InputCommand> buildCommands) {
		infoWindowController.OpenCommandMenu(buildCommands);
	}

	public void CloseBuildMenu() {
		infoWindowController.CloseCommandMenu();
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

	public void OpenPlayerList() {
		IsUIReceivingInput = true;
		playerListController.OpenWindow();
	}

	public void ClosePlayerList() {
		playerListController.CloseWindow();
		IsUIReceivingInput = false;
	}
}
