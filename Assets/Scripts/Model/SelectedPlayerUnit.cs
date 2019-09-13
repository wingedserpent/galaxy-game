using DarkRift;
using System.Collections.Generic;

public class SelectedPlayerUnit : IDarkRiftSerializable {
	
	public int playerUnitId { get; set; }
	public string unitType { get; set; }
	public string weaponSelection { get; set; }
	public List<string> equipmentSelections { get; set; }

	public SelectedPlayerUnit() {
		equipmentSelections = new List<string>();
	}

	public void Deserialize(DeserializeEvent e) {
		playerUnitId = e.Reader.ReadInt32();
		unitType = e.Reader.ReadString();
		weaponSelection = e.Reader.ReadString();

		equipmentSelections.Clear();
		int numEquipSelections = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipSelections; i++) {
			equipmentSelections.Add(e.Reader.ReadString());
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write(playerUnitId);
		e.Writer.Write(unitType);
		e.Writer.Write(weaponSelection);
		
		e.Writer.Write(equipmentSelections.Count);
		foreach (string equipmentSelection in equipmentSelections) {
			e.Writer.Write(equipmentSelection);
		}
	}

	public override bool Equals(object obj) {
		var unit = obj as SelectedPlayerUnit;
		return unit != null &&
			   playerUnitId == unit.playerUnitId;
	}

	public override int GetHashCode() {
		return 1013568171 + playerUnitId.GetHashCode();
	}
}
