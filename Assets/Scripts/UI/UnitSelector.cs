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
	private string _unitName;
	private int _squadCost;

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
			_playerUnitId = _playerUnit.PlayerUnitId;
			UnitName = _playerUnit.Name;
			SquadCost = _playerUnit.SquadCost;
			//temporary automatic selection
			if (_playerUnit.WeaponOptions.Count > 0) {
				SelectedPlayerUnit.WeaponSelection = _playerUnit.WeaponOptions[0].Name;
			}
			SelectedPlayerUnit.EquipmentSelections = _playerUnit.EquipmentOptions.Select(x => x.Name).ToList();
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
			return _squadCost;
		}
		set {
			_squadCost = value;
			squadCostText.text = _squadCost.ToString();
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
