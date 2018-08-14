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
	public Button confirmButton;

	public int MaxSquadCost { get; set; }

	private List<UnitSelector> allUnitSelectors = new List<UnitSelector>();
	private List<SelectedPlayerUnit> selectedUnits = new List<SelectedPlayerUnit>();
	private int squadCost;

	public void OpenMenu(int maxSquadCost = 0) {
		if (maxSquadCost > 0) {
			MaxSquadCost = maxSquadCost;
		}

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
		selectedUnits.Add(unitSelector.SelectedPlayerUnit);
		UpdateSquadCost(squadCost + unitSelector.SquadCost);
	}

	public void OnUnitDeselected(UnitSelector unitSelector) {
		selectedUnits.Remove(unitSelector.SelectedPlayerUnit);
		UpdateSquadCost(squadCost - unitSelector.SquadCost);
	}

	public void OnConfirm() {
		ClientNetworkManager.Instance.SendSquadSelection(selectedUnits);

		CloseMenu();

		RTSCamera rtsCam = FindObjectOfType<RTSCamera>();
		if (rtsCam != null) {
			rtsCam.RefocusOn(ClientGameManager.Instance.TeamSpawns[ClientGameManager.Instance.MyPlayer.TeamId].transform.position);
		}
	}

	private void UpdateSquadCost(int newCost) {
		squadCost = newCost;
		squadCostText.text = "Total Cost: " + squadCost + " / " + MaxSquadCost;

		foreach (UnitSelector unitSelector in allUnitSelectors) {
			unitSelector.Toggle.interactable = unitSelector.Toggle.isOn || squadCost + unitSelector.SquadCost <= MaxSquadCost;
		}
	}
}
