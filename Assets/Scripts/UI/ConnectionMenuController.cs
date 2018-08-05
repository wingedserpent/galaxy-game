using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class ConnectionMenuController : MonoBehaviour {

	public InputField loginNameInput;
	public Text statusText;

	public void OpenMenu(int maxSquadCost = 0) {
		gameObject.SetActive(true);
	}

	private void CloseMenu() {
		gameObject.SetActive(false);
		UIManager.Instance.OnConnectionMenuClosed();
	}

	private void OnEnable() {
		ClientNetworkManager.Instance.ServerJoinUpdate += UpdateStatusText;
		ClientNetworkManager.Instance.ServerJoinSuccess += OnServerJoinSuccess;
		ClientNetworkManager.Instance.ServerJoinFailure += UpdateStatusText;
	}

	private void OnDisable() {
		ClientNetworkManager.Instance.ServerJoinUpdate -= UpdateStatusText;
		ClientNetworkManager.Instance.ServerJoinSuccess -= OnServerJoinSuccess;
		ClientNetworkManager.Instance.ServerJoinFailure -= UpdateStatusText;
	}

	public void OnConnect() {
		ClientPlayFabManager.Instance.loginId = loginNameInput.text;
		ClientPlayFabManager.Instance.StartJoinProcess();
	}

	public void OnStartOfflineTest() {
		ClientGameManager.Instance.StartOfflineTest();

		CloseMenu();
	}

	public void OnQuit() {
		Application.Quit();
	}

	private void OnServerJoinSuccess(string message) {
		UpdateStatusText(message);

		CloseMenu();
	}

	private void UpdateStatusText(string newText) {
		statusText.text = newText;
	}
}
