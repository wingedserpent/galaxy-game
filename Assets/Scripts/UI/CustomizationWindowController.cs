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

		foreach (Weapon weapon in customizableUnit.PlayerUnit.WeaponOptions) {
			WeaponOption weaponOption = Instantiate<GameObject>(weaponOptionPrefab.gameObject, weaponContainer).GetComponent<WeaponOption>();
			weaponOption.CustomizationMenu = this;
			weaponOption.Weapon = weapon;
			weaponOption.GetComponent<Toggle>().group = weaponContainer.GetComponent<ToggleGroup>();
			if (weapon.Name.Equals(customizableUnit.SelectedPlayerUnit.WeaponSelection)) {
				weaponOption.GetComponent<Toggle>().isOn = true;
			}
		}

		foreach (Equipment equipment in customizableUnit.PlayerUnit.EquipmentOptions) {
			EquipmentOption equipmentOption = Instantiate<GameObject>(equipmentOptionPrefab.gameObject, equipmentContainer).GetComponent<EquipmentOption>();
			equipmentOption.CustomizationMenu = this;
			equipmentOption.Equipment = equipment;
			if (customizableUnit.SelectedPlayerUnit.EquipmentSelections.Contains(equipment.Name)) {
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

		customizableUnit = null;
	}

	public void OnWeaponOptionSelected(WeaponOption weaponOption) {
		customizableUnit.SelectedPlayerUnit.WeaponSelection = weaponOption.Weapon.Name;
		UpdateSquadCost();
	}

	public void OnWeaponOptionDeselected(WeaponOption weaponOption) {
		if (customizableUnit.SelectedPlayerUnit.WeaponSelection.Equals(weaponOption.Weapon.Name)) {
			customizableUnit.SelectedPlayerUnit.WeaponSelection = null;
			UpdateSquadCost();
		}
	}

	public void OnEquipmentOptionSelected(EquipmentOption equipmentOption) {
		if (!customizableUnit.SelectedPlayerUnit.EquipmentSelections.Contains(equipmentOption.Equipment.Name)) {
			customizableUnit.SelectedPlayerUnit.EquipmentSelections.Add(equipmentOption.Equipment.Name);
			UpdateSquadCost();
		}
	}

	public void OnEquipmentOptionDeselected(EquipmentOption equipmentOption) {
		if (customizableUnit.SelectedPlayerUnit.EquipmentSelections.Contains(equipmentOption.Equipment.Name)) {
			customizableUnit.SelectedPlayerUnit.EquipmentSelections.Remove(equipmentOption.Equipment.Name);
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
