using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationMenuController : MonoBehaviour, ICustomizationMenu {

	public Transform weaponContainer;
	public Transform equipmentContainer;
	public WeaponOption weaponOptionPrefab;
	public EquipmentOption equipmentOptionPrefab;
	public Text squadCostText;

	public UnitSelector UnitSelector { get; set; }

	public void OpenMenu() {
		foreach (WeaponData weapon in UnitSelector.PlayerUnit.weaponOptions) {
			WeaponOption weaponOption = Instantiate<GameObject>(weaponOptionPrefab.gameObject, weaponContainer).GetComponent<WeaponOption>();
			weaponOption.CustomizationMenu = this;
			weaponOption.Weapon = weapon;
			weaponOption.GetComponent<Toggle>().group = weaponContainer.GetComponent<ToggleGroup>();
			if (weapon.name.Equals(UnitSelector.SelectedPlayerUnit.weaponSelection)) {
				weaponOption.GetComponent<Toggle>().isOn = true;
			}
		}

		foreach (EquipmentData equipment in UnitSelector.PlayerUnit.equipmentOptions) {
			EquipmentOption equipmentOption = Instantiate<GameObject>(equipmentOptionPrefab.gameObject, equipmentContainer).GetComponent<EquipmentOption>();
			equipmentOption.CustomizationMenu = this;
			equipmentOption.Equipment = equipment;
			if (UnitSelector.SelectedPlayerUnit.equipmentSelections.Contains(equipment.name)) {
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
		
		UnitSelector = null;
	}

	public void OnWeaponOptionSelected(WeaponOption weaponOption) {
		UnitSelector.SelectedPlayerUnit.weaponSelection = weaponOption.Weapon.name;
		UpdateSquadCost();
	}

	public void OnWeaponOptionDeselected(WeaponOption weaponOption) {
		if (UnitSelector.SelectedPlayerUnit.weaponSelection.Equals(weaponOption.Weapon.name)) {
			UnitSelector.SelectedPlayerUnit.weaponSelection = null;
			UpdateSquadCost();
		}
	}

	public void OnEquipmentOptionSelected(EquipmentOption equipmentOption) {
		if (!UnitSelector.SelectedPlayerUnit.equipmentSelections.Contains(equipmentOption.Equipment.name)) {
			UnitSelector.SelectedPlayerUnit.equipmentSelections.Add(equipmentOption.Equipment.name);
			UpdateSquadCost();
		}
	}

	public void OnEquipmentOptionDeselected(EquipmentOption equipmentOption) {
		if (UnitSelector.SelectedPlayerUnit.equipmentSelections.Contains(equipmentOption.Equipment.name)) {
			UnitSelector.SelectedPlayerUnit.equipmentSelections.Remove(equipmentOption.Equipment.name);
			UpdateSquadCost();
		}
	}

	public void OnConfirm() {
		CloseMenu();
	}

	private void UpdateSquadCost() {
		UnitSelector.RecalculateSquadCost();
		squadCostText.text = "Total Unit Cost: " + UnitSelector.SquadCost;
	}
}
