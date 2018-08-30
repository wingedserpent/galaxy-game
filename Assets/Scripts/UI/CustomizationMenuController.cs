using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationMenuController : Singleton<CustomizationMenuController> {

	public Transform weaponContainer;
	public Transform equipmentContainer;
	public WeaponOption weaponOptionPrefab;
	public EquipmentOption equipmentOptionPrefab;
	public Text squadCostText;

	public UnitSelector UnitSelector { get; set; }

	private int squadCost;

	public void OpenMenu() {
		int initialCost = UnitSelector.PlayerUnit.SquadCost;

		foreach (Weapon weapon in UnitSelector.PlayerUnit.WeaponOptions) {
			WeaponOption weaponOption = Instantiate<GameObject>(weaponOptionPrefab.gameObject, weaponContainer).GetComponent<WeaponOption>();
			weaponOption.CustomizationMenuController = this;
			weaponOption.Weapon = weapon;
			weaponOption.GetComponent<Toggle>().group = weaponContainer.GetComponent<ToggleGroup>();
			if (weapon.Name.Equals(UnitSelector.SelectedPlayerUnit.WeaponSelection)) {
				weaponOption.GetComponent<Toggle>().isOn = true;
				initialCost += weapon.SquadCost;
			}
		}

		foreach (Equipment equipment in UnitSelector.PlayerUnit.EquipmentOptions) {
			EquipmentOption equipmentOption = Instantiate<GameObject>(equipmentOptionPrefab.gameObject, equipmentContainer).GetComponent<EquipmentOption>();
			equipmentOption.CustomizationMenuController = this;
			equipmentOption.Equipment = equipment;
			if (UnitSelector.SelectedPlayerUnit.EquipmentSelections.Contains(equipment.Name)) {
				equipmentOption.GetComponent<Toggle>().isOn = true;
				initialCost += equipment.SquadCost;
			}
		}

		UpdateSquadCost(initialCost);
		UnitSelector.UpdateSquadCost(initialCost);

		gameObject.SetActive(true);
	}

	private void CloseMenu() {
		gameObject.SetActive(false);

		UnitSelector = null;
		foreach (WeaponOption weaponOption in weaponContainer.GetComponentsInChildren<WeaponOption>()) {
			Destroy(weaponOption.gameObject);
		}
		foreach (EquipmentOption equipmentOption in equipmentContainer.GetComponentsInChildren<EquipmentOption>()) {
			Destroy(equipmentOption.gameObject);
		}

		UpdateSquadCost(0);
	}

	public void OnWeaponOptionSelected(WeaponOption weaponOption) {
		UnitSelector.SelectedPlayerUnit.WeaponSelection = weaponOption.Weapon.Name;
		UpdateSquadCost(squadCost + weaponOption.Weapon.SquadCost);
		UnitSelector.UpdateSquadCost(squadCost);
	}

	public void OnWeaponOptionDeselected(WeaponOption weaponOption) {
		if (UnitSelector.SelectedPlayerUnit.WeaponSelection.Equals(weaponOption.Weapon.Name)) {
			UnitSelector.SelectedPlayerUnit.WeaponSelection = null;
		}
		UpdateSquadCost(squadCost - weaponOption.Weapon.SquadCost);
		UnitSelector.UpdateSquadCost(squadCost);
	}

	public void OnEquipmentOptionSelected(EquipmentOption equipmentOption) {
		if (!UnitSelector.SelectedPlayerUnit.EquipmentSelections.Contains(equipmentOption.Equipment.Name)) {
			UnitSelector.SelectedPlayerUnit.EquipmentSelections.Add(equipmentOption.Equipment.Name);
			UpdateSquadCost(squadCost + equipmentOption.Equipment.SquadCost);
			UnitSelector.UpdateSquadCost(squadCost);
		}
	}

	public void OnEquipmentOptionDeselected(EquipmentOption equipmentOption) {
		if (UnitSelector.SelectedPlayerUnit.EquipmentSelections.Contains(equipmentOption.Equipment.Name)) {
			UnitSelector.SelectedPlayerUnit.EquipmentSelections.Remove(equipmentOption.Equipment.Name);
			UpdateSquadCost(squadCost - equipmentOption.Equipment.SquadCost);
			UnitSelector.UpdateSquadCost(squadCost);
		}
	}

	public void OnConfirm() {
		CloseMenu();
	}

	private void UpdateSquadCost(int newCost) {
		squadCost = newCost;
		squadCostText.text = "Total Unit Cost: " + squadCost;
	}
}
