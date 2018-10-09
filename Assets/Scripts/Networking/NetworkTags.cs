using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkTags {

	public const int Connection = 0;
	public const int JoinGame = 1;
	public const int GameState = 10;
	public const int PlayerJoined = 11;
	public const int PlayerLeft = 12;
	public const int CapturePoint = 13;
	public const int EntityUpdate = 20;
	public const int EntityDeath = 21;
	public const int Command = 30;
	public const int UnitList = 40;
	public const int SquadSelection = 41;
	public const int FullUnitList = 42;
	public const int CustomizedUnits = 43;
	public const int Construction = 50;
	public const int ChatMessage = 60;
}
