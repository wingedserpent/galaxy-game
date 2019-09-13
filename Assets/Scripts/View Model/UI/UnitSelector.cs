using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelector : MonoBehaviour {

	public Text unitNameText;
	public Text squadCostText;

	private PlayerUnit _playerUnit;
	private int _playerUnitId;
	private int _baseSquadCost;
	private string _unitName;
	private int _totalSquadCost;

	public SelectedPlayerUnit SelectedPlayerUnit { get; private set; }
	public Toggle Toggle { get; set; }
	public SquadMenuController SquadMenuController { get; set; }

	public PlayerUnit PlayerUnit {
		get {
			return _playerUnit;
		}
		set {
			_playerUnit = value;
			SelectedPlayerUnit = new SelectedPlayerUnit();
			SelectedPlayerUnit.playerUnitId = _playerUnit.playerUnitId;
			SelectedPlayerUnit.unitType = _playerUnit.unitType;

			WeaponData equippedWeapon = _playerUnit.weaponOptions.Where(x => x.isEquipped).FirstOrDefault();
			if (equippedWeapon == null) {
				equippedWeapon = _playerUnit.weaponOptions[0];
			}
			SelectedPlayerUnit.weaponSelection = equippedWeapon.name;
			
			SelectedPlayerUnit.equipmentSelections = _playerUnit.equipmentOptions.Where(x => x.isEquipped).Select(x => x.name).ToList();

			_playerUnitId = _playerUnit.playerUnitId;
			_baseSquadCost = _playerUnit.squadCost;
			UnitName = _playerUnit.name;
			SquadCost = _playerUnit.squadCost;

			RecalculateSquadCost();
		}
	}
	public string UnitName {
		get {
			return _unitName;
		}
		private set {
			_unitName = value;
			unitNameText.text = _unitName;
		}
	}
	public int SquadCost {
		get {
			return _totalSquadCost;
		}
		private set {
			_totalSquadCost = value;
			squadCostText.text = _totalSquadCost.ToString();
		}
	}

	private void Awake() {
		Toggle = GetComponent<Toggle>();
	}

	public void OnToggle(bool on) {
		if (on) {
			SquadMenuController.OnUnitSelected(this);
		} else {
			SquadMenuController.OnUnitDeselected(this);
		}
	}

	public void OnCustomize() {
		CustomizationMenuController custMenu = UIManager.Instance.customizationMenuController;
		custMenu.UnitSelector = this;
		custMenu.OpenMenu();
	}

	public void RecalculateSquadCost() {
		SquadCost = _baseSquadCost
					+ _playerUnit.weaponOptions.Where(x => x.name.Equals(SelectedPlayerUnit.weaponSelection)).Sum(x => x.squadCost)
					+ _playerUnit.equipmentOptions.Where(x => SelectedPlayerUnit.equipmentSelections.Contains(x.name)).Sum(x => x.squadCost);
		SquadMenuController.OnUnitUpdated(this);
	}
	
	public override bool Equals(object obj) {
		var selector = obj as UnitSelector;
		return selector != null &&
			   base.Equals(obj) &&
			   _playerUnitId == selector._playerUnitId;
	}

	public override int GetHashCode() {
		var hashCode = -1390341253;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + _playerUnitId.GetHashCode();
		return hashCode;
	}
}
