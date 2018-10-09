using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OverallStateManager : Singleton<OverallStateManager> {
	
	public OverallState OverallState { get; private set; }
	public bool IsOfflineTest { get; private set; }
	public bool IsInSceneTransition { get; private set; }

	private GameState initialGameState;
	private bool hasSpawnedEntities = false;

	protected override void Awake() {
		base.Awake();

		OverallState = OverallState.MAIN_MENU;
		IsOfflineTest = false;
		IsInSceneTransition = false;
	}

	private void Start() {
		if (ClientGameManager.Instance != null) {
			OverallState = OverallState.IN_GAME;
		}
	}

	public void StartOfflineTest() {
		IsOfflineTest = true;

		IsInSceneTransition = true;
		SceneManager.sceneLoaded += OnOfflineGameLoaded;
		SceneManager.LoadScene("Client", LoadSceneMode.Single);
	}

	protected void OnOfflineGameLoaded(Scene scene, LoadSceneMode loadSceneMode) {
		SceneManager.sceneLoaded -= OnOfflineGameLoaded;
		IsInSceneTransition = false;

		StartCoroutine(DelayedOfflineStart());
	}

	protected IEnumerator DelayedOfflineStart() {
		//delay for one frame to give everything a chance to 'Start'
		yield return 0;

		OverallState = OverallState.IN_GAME;
		ClientGameManager.Instance.StartOfflineTest();
	}

	public void JoinGame() {
		ClientNetworkManager.Instance.SendJoinGameRequest();
	}

	public void LoadGame(GameState initialGameState, bool hasSpawnedEntities) {
		this.initialGameState = initialGameState;
		this.hasSpawnedEntities = hasSpawnedEntities;

		IsInSceneTransition = true;
		SceneManager.sceneLoaded += OnGameLoaded;
		SceneManager.LoadScene("Client", LoadSceneMode.Single);
	}

	protected void OnGameLoaded(Scene scene, LoadSceneMode loadSceneMode) {
		SceneManager.sceneLoaded -= OnGameLoaded;
		IsInSceneTransition = false;

		StartCoroutine(DelayedGameStateUpdate());
	}

	protected IEnumerator DelayedGameStateUpdate() {
		//delay for one frame to give everything a chance to 'Start'
		yield return 0;

		OverallState = OverallState.IN_GAME;
		ClientGameManager.Instance.UpdateGameState(initialGameState, hasSpawnedEntities);
	}

	public void OnGameEnd() {
		Invoke("LoadMainMenu", 10f);
	}

	protected void LoadMainMenu() {
		IsInSceneTransition = true;
		SceneManager.sceneLoaded += OnMainMenuLoaded;
		SceneManager.LoadScene("ClientMenu", LoadSceneMode.Single);
	}

	protected void OnMainMenuLoaded(Scene scene, LoadSceneMode loadSceneMode) {
		SceneManager.sceneLoaded -= OnMainMenuLoaded;
		IsInSceneTransition = false;
		OverallState = OverallState.MAIN_MENU;
	}
}

public enum OverallState {
	MAIN_MENU,
	IN_GAME
}
