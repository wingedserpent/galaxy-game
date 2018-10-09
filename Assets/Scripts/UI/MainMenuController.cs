using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class MainMenuController : MonoBehaviour {

	public void OpenMenu() {
		gameObject.SetActive(true);
	}

	private void CloseMenu() {
		gameObject.SetActive(false);
		
		MainMenuManager.Instance.OnMainMenuClosed();
	}

	public void OnJoinGame() {
		OverallStateManager.Instance.JoinGame();
	}

	public void OnOpenArmoryMenu() {
		CloseMenu();

		MainMenuManager.Instance.OpenArmoryMenu();
	}
}
