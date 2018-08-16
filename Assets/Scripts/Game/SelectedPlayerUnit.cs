using DarkRift;
using System.Collections.Generic;

public class SelectedPlayerUnit : IDarkRiftSerializable {
	
	public int PlayerUnitId { get; set; }
	public string UnitType { get; set; }
	public string WeaponSelection { get; set; }
	public List<string> EquipmentSelections { get; set; }

	public SelectedPlayerUnit() {
		EquipmentSelections = new List<string>();
	}

	public void Deserialize(DeserializeEvent e) {
		PlayerUnitId = e.Reader.ReadInt32();
		UnitType = e.Reader.ReadString();
		WeaponSelection = e.Reader.ReadString();

		int numEquipSelections = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipSelections; i++) {
			EquipmentSelections.Add(e.Reader.ReadString());
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(PlayerUnitId);
		e.Writer.Write(UnitType);
		e.Writer.Write(WeaponSelection);
		
		e.Writer.Write(EquipmentSelections.Count);
		foreach (string equipmentSelection in EquipmentSelections) {
			e.Writer.Write(equipmentSelection);
		}
	}

	public override bool Equals(object obj) {
		var unit = obj as SelectedPlayerUnit;
		return unit != null &&
			   PlayerUnitId == unit.PlayerUnitId;
	}

	public override int GetHashCode() {
		return 1013568171 + PlayerUnitId.GetHashCode();
	}
}
