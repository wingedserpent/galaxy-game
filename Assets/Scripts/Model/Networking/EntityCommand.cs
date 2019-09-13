using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityCommand : IDarkRiftSerializable {
	
	public CommandType type { get; set; }
	public string abilityTypeId { get; set; }
	public List<string> actingEntityIds { get; set; }
	public string targetEntityId { get; set; }
	public Vector3 point { get; set; }

	public EntityCommand() { }

	public EntityCommand(CommandType type, List<string> actingEntityIds) {
		this.type = type;
		this.actingEntityIds = actingEntityIds;
	}

	public void Deserialize(DeserializeEvent e) {
		type = (CommandType)e.Reader.ReadInt32();
		abilityTypeId = e.Reader.ReadString();
		abilityTypeId = (abilityTypeId.Length == 0) ? null : abilityTypeId;
		actingEntityIds = new List<string>(e.Reader.ReadStrings());
		targetEntityId = e.Reader.ReadString();
		targetEntityId = (targetEntityId.Length == 0) ? null : targetEntityId;
		point = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)type);
		e.Writer.Write(abilityTypeId ?? "");
		e.Writer.Write(actingEntityIds.ToArray());
		e.Writer.Write(targetEntityId ?? "");
		e.Writer.Write(point.x);	e.Writer.Write(point.y);	e.Writer.Write(point.z);
	}
}

public enum CommandType {
	MOVE,
	RETREAT,
	STOP,
	ATTACK,
	ATTACK_LOCATION,
	NONE,
	ABILITY,
	TOGGLE_MODE
}