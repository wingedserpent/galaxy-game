using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using RTSCam;

public class ArmoryMenuController : MonoBehaviour {

	public Transform unitListContainer;
	public CustomizableUnit customizableUnitPrefab;
	public Transform previewContainer;

	public EntityDatabase entityDatabase;

	private List<CustomizableUnit> allCustomizableUnits = new List<CustomizableUnit>();
	private Entity previewEntity;

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
		if (previewEntity != null) {
			Destroy(previewEntity.gameObject);
		}
		CloseMenu();
	}

	public void OnUnitSelected(CustomizableUnit customizableUnit) {
		previewEntity = entityDatabase.GetEntityInstance(customizableUnit.SelectedPlayerUnit.UnitType, Vector3.zero, Quaternion.identity, previewContainer);
		previewEntity.GetComponent<EntityController>().enabled = false;
		previewEntity.GetComponentInChildren<Vision>().enabled = false;
	}

	public void OnUnitDeselected(CustomizableUnit customizableUnit) {
		if (previewEntity != null) {
			Destroy(previewEntity.gameObject);
		}
	}
}
