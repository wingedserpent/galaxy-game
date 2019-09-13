using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationWindowController : MonoBehaviour, ICustomizationMenu {

	public Transform weaponContainer;
	public Transform equipmentContainer;
	public WeaponOption weaponOptionPrefab;
	public EquipmentOption equipmentOptionPrefab;
	public Text squadCostText;

	private CustomizableUnit customizableUnit;

	public void Populate(CustomizableUnit unit) {
		customizableUnit = unit;

		foreach (WeaponData weapon in customizableUnit.PlayerUnit.weaponOptions) {
			WeaponOption weaponOption = Instantiate<GameObject>(weaponOptionPrefab.gameObject, weaponContainer).GetComponent<WeaponOption>();
			weaponOption.CustomizationMenu = this;
			weaponOption.Weapon = weapon;
			weaponOption.GetComponent<Toggle>().group = weaponContainer.GetComponent<ToggleGroup>();
			if (weapon.name.Equals(customizableUnit.SelectedPlayerUnit.weaponSelection)) {
				weaponOption.GetComponent<Toggle>().isOn = true;
			}
		}

		foreach (EquipmentData equipment in customizableUnit.PlayerUnit.equipmentOptions) {
			EquipmentOption equipmentOption = Instantiate<GameObject>(equipmentOptionPrefab.gameObject, equipmentContainer).GetComponent<EquipmentOption>();
			equipmentOption.CustomizationMenu = this;
			equipmentOption.Equipment = equipment;
			if (customizableUnit.SelectedPlayerUnit.equipmentSelections.Contains(equipment.name)) {
				equipmentOption.GetComponent<Toggle>().isOn = true;
			}
		}

		UpdateSquadCost();

		gameObject.SetActive(true);
	}

	private void CloseMenu() {
		gameObject.SetActive(false);

		UpdateSquadCost();

		foreach (WeaponOption weaponOption in weaponContainer.GetComponentsInChildren<WeaponOption>()) {
			Destroy(weaponOption.gameObject);
		}
		foreach (EquipmentOption equipmentOption in equipmentContainer.GetComponentsInChildren<EquipmentOption>()) {
			Destroy(equipmentOption.gameObject);
		}

		MainMenuManager.Instance.armoryMenuController.OnUnitDeselected(customizableUnit);

		customizableUnit = null;
	}

	public void OnWeaponOptionSelected(WeaponOption weaponOption) {
		customizableUnit.SelectedPlayerUnit.weaponSelection = weaponOption.Weapon.name;
		UpdateSquadCost();
	}

	public void OnWeaponOptionDeselected(WeaponOption weaponOption) {
		if (customizableUnit.SelectedPlayerUnit.weaponSelection.Equals(weaponOption.Weapon.name)) {
			customizableUnit.SelectedPlayerUnit.weaponSelection = null;
			UpdateSquadCost();
		}
	}

	public void OnEquipmentOptionSelected(EquipmentOption equipmentOption) {
		if (!customizableUnit.SelectedPlayerUnit.equipmentSelections.Contains(equipmentOption.Equipment.name)) {
			customizableUnit.SelectedPlayerUnit.equipmentSelections.Add(equipmentOption.Equipment.name);
			UpdateSquadCost();
		}
	}

	public void OnEquipmentOptionDeselected(EquipmentOption equipmentOption) {
		if (customizableUnit.SelectedPlayerUnit.equipmentSelections.Contains(equipmentOption.Equipment.name)) {
			customizableUnit.SelectedPlayerUnit.equipmentSelections.Remove(equipmentOption.Equipment.name);
			UpdateSquadCost();
		}
	}

	public void OnConfirm() {
		CloseMenu();
	}

	private void UpdateSquadCost() {
		customizableUnit.RecalculateSquadCost();
		squadCostText.text = "Total Unit Cost: " + customizableUnit.SquadCost;
	}
}
