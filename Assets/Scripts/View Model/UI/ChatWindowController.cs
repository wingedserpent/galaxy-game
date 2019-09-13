using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatWindowController : MonoBehaviour {

	public Text chatTextPrefab;
	public Text systemTextPrefab;
	public RectTransform messageContainer;
	public Scrollbar containerScrollbar;
	public InputField textInput;
	public Button submitButton;
	public bool autoScroll = true;

	private bool isInputFocused = false;

	private ClientNetworkManager clientNetworkManager;

	private void Start() {
		clientNetworkManager = ClientNetworkManager.Instance;
	}

	private void Update() {
		if (textInput.isFocused != isInputFocused) {
			isInputFocused = !isInputFocused;
			UIManager.Instance.OnChatInputFocusChange(isInputFocused);
		}
	}

	public void OpenWindow() {
		gameObject.SetActive(true);
	}

	public void SubmitInput() {
		if (textInput.text != "") {
			clientNetworkManager.SendChatMessage(textInput.text);
			textInput.text = "";
		}
	}

	public void AddChatMessage(Player messagingPlayer, string messageText) {
		if (messagingPlayer != null) {
			Text newText = Instantiate<GameObject>(chatTextPrefab.gameObject, messageContainer).GetComponent<Text>();
			newText.text = messagingPlayer.name + ": " + messageText;
			if (autoScroll) {
				containerScrollbar.value = 0;
			}
		}
	}

	public void AddSystemMessage(string messageText) {
		Text newText = Instantiate<GameObject>(systemTextPrefab.gameObject, messageContainer).GetComponent<Text>();
		newText.text = "System: " + messageText;
		if (autoScroll) {
			containerScrollbar.value = 0;
		}
	}
}
