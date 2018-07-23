using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using System.Linq;
using DarkRift.Client.Unity;
using DarkRift.Client;
using System;

public class NetworkStatus : Singleton<NetworkStatus> {

	public bool IsClient { get; private set; }
	public bool IsServer { get; private set; }

	protected override void Awake() {
		base.Awake();
		IsClient = FindObjectOfType<ClientNetworkManager>() != null;
		IsServer = FindObjectOfType<ServerNetworkManager>() != null;
	}
	
}
