using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : Singleton<MainMenuManager> {

	public LoginMenuController loginMenuController;
	public MainMenuController mainMenuController;
	public ArmoryMenuController armoryMenuController;
	public CustomizationWindowController customizationWindowController;

	public void OpenLoginMenu() {
		loginMenuController.OpenMenu();
	}

	public void OnLoginMenuClosed() {
		OpenMainMenu();
	}

	public void OpenMainMenu() {
		mainMenuController.OpenMenu();
	}

	public void OnMainMenuClosed() {

	}

	public void OpenArmoryMenu() {
		armoryMenuController.OpenMenu();
	}

	public void OnUnitListReceived(List<PlayerUnit> playerUnits) {
		armoryMenuController.OnUnitListReceived(playerUnits);
	}

	public void OnArmoryMenuClosed() {
		mainMenuController.OpenMenu();
	}

	public void OnQuit() {
		Application.Quit();
	}
}
