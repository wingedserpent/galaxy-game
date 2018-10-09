using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ICustomizationMenu {

	void OnWeaponOptionSelected(WeaponOption weaponOption);
	void OnWeaponOptionDeselected(WeaponOption weaponOption);
	void OnEquipmentOptionSelected(EquipmentOption equipmentOption);
	void OnEquipmentOptionDeselected(EquipmentOption equipmentOption);
}
