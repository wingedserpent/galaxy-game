using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class ArmoryMenuController : MonoBehaviour {

	public Transform unitListContainer;
	public CustomizableUnit customizableUnitPrefab;

	private List<CustomizableUnit> allCustomizableUnits = new List<CustomizableUnit>();

	public void OpenMenu() {
		ClientNetworkManager.Instance.RequestAllUnits();
	}

	private void CloseMenu() {
		allCustomizableUnits.ForEach(x => Destroy(x.gameObject));
		allCustomizableUnits.Clear();

		gameObject.SetActive(false);
		MainMenuManager.Instance.OnArmoryMenuClosed();
	}

	public void OnUnitListReceived(List<PlayerUnit> playerUnits) {
		gameObject.SetActive(true);

		foreach (PlayerUnit playerUnit in playerUnits) {
			CustomizableUnit customizableUnit = Instantiate<GameObject>(customizableUnitPrefab.gameObject, unitListContainer).GetComponent<CustomizableUnit>();
			customizableUnit.PlayerUnit = playerUnit;
			allCustomizableUnits.Add(customizableUnit);
		}
	}

	public void OnClose() {
		ClientNetworkManager.Instance.SendCustomizedUnits(allCustomizableUnits.Select(x => x.SelectedPlayerUnit).ToList());

		CloseMenu();
	}
}
