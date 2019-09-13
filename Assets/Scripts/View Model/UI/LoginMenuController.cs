using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class LoginMenuController : MonoBehaviour {

	public InputField loginNameInput;
	public Text statusText;

	private void Start() {
		ClientNetworkManager.Instance.ServerJoinUpdate += UpdateStatusText;
		ClientNetworkManager.Instance.ServerJoinSuccess += OnServerJoinSuccess;
		ClientNetworkManager.Instance.ServerJoinFailure += UpdateStatusText;
	}

	public void OpenMenu() {
		gameObject.SetActive(true);
	}

	private void CloseMenu() {
		gameObject.SetActive(false);
		
		MainMenuManager.Instance.OnLoginMenuClosed();
	}

	/*
	private void OnEnable() {
		ClientNetworkManager.Instance.ServerJoinUpdate += UpdateStatusText;
		ClientNetworkManager.Instance.ServerJoinSuccess += OnServerJoinSuccess;
		ClientNetworkManager.Instance.ServerJoinFailure += UpdateStatusText;
	}
	*/

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
		OverallStateManager.Instance.StartOfflineTest();
	}

	private void OnServerJoinSuccess(string message) {
		UpdateStatusText(message);

		CloseMenu();
	}

	private void UpdateStatusText(string newText) {
		statusText.text = newText;
	}
}
