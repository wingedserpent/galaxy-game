using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WeaponOption : MonoBehaviour {

	public Text weaponNameText;
	public Text squadCostText;

	private WeaponData _weapon;
	private string _weaponName;
	private int _squadCost;
	
	public ICustomizationMenu CustomizationMenu { get; set; }

	public WeaponData Weapon {
		get {
			return _weapon;
		}
		set {
			_weapon = value;
			WeaponName = _weapon.name;
			SquadCost = _weapon.squadCost;
		}
	}
	public string WeaponName {
		get {
			return _weaponName;
		}
		private set {
			_weaponName = value;
			weaponNameText.text = _weaponName;
		}
	}
	public int SquadCost {
		get {
			return _squadCost;
		}
		private set {
			_squadCost = value;
			squadCostText.text = _squadCost.ToString();
		}
	}

	public void OnToggle(bool on) {
		if (on) {
			CustomizationMenu.OnWeaponOptionSelected(this);
		} else {
			CustomizationMenu.OnWeaponOptionDeselected(this);
		}
	}

	/* not needed?
	public override bool Equals(object obj) {
		var option = obj as WeaponOption;
		return option != null &&
			   base.Equals(obj) &&
			   _weaponName == option._weaponName;
	}

	public override int GetHashCode() {
		var hashCode = -1553815717;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_weaponName);
		return hashCode;
	}
	*/
}
