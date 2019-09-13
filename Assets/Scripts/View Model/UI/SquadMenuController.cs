using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class SquadMenuController : MonoBehaviour {

	public Transform unitListContainer;
	public UnitSelector unitSelectorPrefab;
	public Text squadCostText;

	public int MaxSquadCost { get; set; }

	private List<UnitSelector> allUnitSelectors = new List<UnitSelector>();
	private List<UnitSelector> selectedUnits = new List<UnitSelector>();
	private int squadCost;

	public void OpenMenu() {
		ClientNetworkManager.Instance.RequestUnitList();
	}

	private void CloseMenu() {
		allUnitSelectors.ForEach(x => Destroy(x.gameObject));
		allUnitSelectors.Clear();
		selectedUnits.Clear();
		UpdateSquadCost(0);

		gameObject.SetActive(false);
		UIManager.Instance.OnSquadMenuClosed();
	}

	public void OnUnitListReceived(List<PlayerUnit> playerUnits) {
		MaxSquadCost = ClientGameManager.Instance.MyPlayer.maxSquadCost;

		gameObject.SetActive(true);

		foreach (PlayerUnit playerUnit in playerUnits) {
			UnitSelector unitSelector = Instantiate<GameObject>(unitSelectorPrefab.gameObject, unitListContainer).GetComponent<UnitSelector>();
			unitSelector.SquadMenuController = this;
			unitSelector.PlayerUnit = playerUnit;
			allUnitSelectors.Add(unitSelector);
		}

		UpdateSquadCost(0);
	}

	public void OnUnitSelected(UnitSelector unitSelector) {
		selectedUnits.Add(unitSelector);
		UpdateSquadCost(squadCost + unitSelector.SquadCost);
	}

	public void OnUnitDeselected(UnitSelector unitSelector) {
		selectedUnits.Remove(unitSelector);
		UpdateSquadCost(squadCost - unitSelector.SquadCost);
	}

	public void OnUnitUpdated(UnitSelector unitSelector) {
		if (selectedUnits.Contains(unitSelector)) {
			UpdateSquadCost(selectedUnits.Sum(x => x.SquadCost));
		}
	}

	public void OnConfirm() {
		if (selectedUnits.Count > 0 && squadCost <= MaxSquadCost) {
			ClientNetworkManager.Instance.SendSquadSelection(selectedUnits.Select(x => x.SelectedPlayerUnit).ToList());

			CloseMenu();

			RTSCamera rtsCam = FindObjectOfType<RTSCamera>();
			if (rtsCam != null) {
				rtsCam.RefocusOn(ClientGameManager.Instance.TeamSpawns[ClientGameManager.Instance.MyPlayer.teamId].transform.position);
			}
		}
	}

	private void UpdateSquadCost(int newCost) {
		squadCost = newCost;
		squadCostText.text = "Total Cost: " + squadCost + " / " + MaxSquadCost;
		if (squadCost > MaxSquadCost) {
			squadCostText.color = Color.red;
		} else {
			squadCostText.color = Color.black;
		}

		foreach (UnitSelector unitSelector in allUnitSelectors) {
			unitSelector.Toggle.interactable = unitSelector.Toggle.isOn || squadCost + unitSelector.SquadCost <= MaxSquadCost;
		}
	}
}
