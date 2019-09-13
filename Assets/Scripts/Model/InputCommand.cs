using DarkRift;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InputCommand {

	public KeyCode key;
	public string command;
	public int cost;

	public InputCommand(KeyCode key, string command, int cost = 0) {
		this.key = key;
		this.command = command;
		this.cost = cost;
	}

	public override bool Equals(object obj) {
		var command = obj as InputCommand;
		return command != null &&
			   this.command == command.command;
	}

	public override int GetHashCode() {
		return -969666430 + EqualityComparer<string>.Default.GetHashCode(command);
	}
}
