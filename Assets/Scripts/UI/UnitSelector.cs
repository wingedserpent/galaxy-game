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
			SelectedPlayerUnit.PlayerUnitId = _playerUnit.PlayerUnitId;
			SelectedPlayerUnit.UnitType = _playerUnit.UnitType;

			Weapon equippedWeapon = _playerUnit.WeaponOptions.Where(x => x.IsEquipped).FirstOrDefault();
			if (equippedWeapon == null) {
				equippedWeapon = _playerUnit.WeaponOptions[0];
			}
			SelectedPlayerUnit.WeaponSelection = equippedWeapon.Name;
			
			SelectedPlayerUnit.EquipmentSelections = _playerUnit.EquipmentOptions.Where(x => x.IsEquipped).Select(x => x.Name).ToList();

			_playerUnitId = _playerUnit.PlayerUnitId;
			_baseSquadCost = _playerUnit.SquadCost;
			UnitName = _playerUnit.Name;
			SquadCost = _playerUnit.SquadCost;

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
					+ _playerUnit.WeaponOptions.Where(x => x.Name.Equals(SelectedPlayerUnit.WeaponSelection)).Sum(x => x.SquadCost)
					+ _playerUnit.EquipmentOptions.Where(x => SelectedPlayerUnit.EquipmentSelections.Contains(x.Name)).Sum(x => x.SquadCost);
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
