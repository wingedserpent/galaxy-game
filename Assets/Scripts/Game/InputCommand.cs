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
}
