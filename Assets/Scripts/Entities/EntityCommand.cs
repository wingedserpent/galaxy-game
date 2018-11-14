using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityCommand : IDarkRiftSerializable {
	
	public CommandType Type { get; set; }
	public List<string> ActingEntityIds { get; set; }
	public string TargetEntityId { get; set; }
	public Vector3 Point { get; set; }

	public EntityCommand() { }

	public EntityCommand(CommandType type, List<string> actingEntityIds) {
		Type = type;
		ActingEntityIds = actingEntityIds;
	}

	public void Deserialize(DeserializeEvent e) {
		Type = (CommandType)e.Reader.ReadInt32();
		ActingEntityIds = new List<string>(e.Reader.ReadStrings());
		Point = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		TargetEntityId = e.Reader.ReadString();
		if ("".Equals(TargetEntityId)) {
			TargetEntityId = null;
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)Type);
		e.Writer.Write(ActingEntityIds.ToArray());
		e.Writer.Write(Point.x);	e.Writer.Write(Point.y);	e.Writer.Write(Point.z);
		e.Writer.Write(TargetEntityId == null ? "" : TargetEntityId);
	}
}

public enum CommandType {
	MOVE,
	RETREAT,
	STOP,
	ATTACK,
	ATTACK_LOCATION,
	NONE,
	ABILITY_SHIELD,
	TOGGLE_MODE
}