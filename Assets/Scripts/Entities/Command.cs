﻿using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Command : IDarkRiftSerializable {
	
	public CommandType Type { get; set; }
	public List<string> ActingEntityIds { get; set; }
	public string TargetEntityId { get; set; }
	public Vector3 Point { get; set; }

	public Command() { }

	public Command(CommandType type, List<string> actingEntityIds) {
		Type = type;
		ActingEntityIds = actingEntityIds;
	}

	public void Deserialize(DeserializeEvent e) {
		Type = (CommandType)e.Reader.ReadInt32();
		ActingEntityIds = new List<string>(e.Reader.ReadStrings());

		if (Type == CommandType.MOVE) {
			Point = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		} else if (Type == CommandType.ATTACK) {
			TargetEntityId = e.Reader.ReadString();
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)Type);
		e.Writer.Write(ActingEntityIds.ToArray());

		if (Type == CommandType.MOVE) {
			e.Writer.Write(Point.x);
			e.Writer.Write(Point.y);
			e.Writer.Write(Point.z);
		} else if (Type == CommandType.ATTACK) {
			e.Writer.Write(TargetEntityId);
		}
	}
}

public enum CommandType {
	MOVE,
	STOP,
	ATTACK,
	NONE,
	ABILITY_SHIELD
}