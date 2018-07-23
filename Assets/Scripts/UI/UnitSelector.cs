using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelector : MonoBehaviour {

	public Text unitNameText;
	public Text squadCostText;
	
	private string _unitName;
	private int _squadCost;

	public Toggle Toggle { get; set; }
	public SquadMenuController SquadMenuController { get; set; }
	public int UnitId { get; set; }
	public string UnitName {
		get {
			return _unitName;
		}
		set {
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

	public override bool Equals(object obj) {
		var selector = obj as UnitSelector;
		return selector != null &&
			   base.Equals(obj) &&
			   UnitId == selector.UnitId;
	}

	public override int GetHashCode() {
		var hashCode = -648971680;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + UnitId.GetHashCode();
		return hashCode;
	}

	public void OnToggle(bool on) {
		if (on) {
			SquadMenuController.OnUnitSelected(this);
		} else {
			SquadMenuController.OnUnitDeselected(this);
		}
	}

}
