﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentOption : MonoBehaviour {

	public Text equipmentNameText;
	public Text squadCostText;

	private EquipmentData _equipment;
	private string _equipmentName;
	private int _squadCost;
	
	public ICustomizationMenu CustomizationMenu { get; set; }

	public EquipmentData Equipment {
		get {
			return _equipment;
		}
		set {
			_equipment = value;
			EquipmentName = _equipment.name;
			SquadCost = _equipment.squadCost;
		}
	}
	public string EquipmentName {
		get {
			return _equipmentName;
		}
		private set {
			_equipmentName = value;
			equipmentNameText.text = _equipmentName;
		}
	}
	public int SquadCost {
		get {
			return _squadCost;
		}
		set {
			_squadCost = value;
			squadCostText.text = _squadCost.ToString();
		}
	}

	public void OnToggle(bool on) {
		if (on) {
			CustomizationMenu.OnEquipmentOptionSelected(this);
		} else {
			CustomizationMenu.OnEquipmentOptionDeselected(this);
		}
	}

	/* not needed?
	public override bool Equals(object obj) {
		var option = obj as EquipmentOption;
		return option != null &&
			   base.Equals(obj) &&
			   _equipmentName == option._equipmentName;
	}

	public override int GetHashCode() {
		var hashCode = -1553815717;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_equipmentName);
		return hashCode;
	}
	*/
}
