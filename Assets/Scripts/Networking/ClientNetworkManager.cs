using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using System.Linq;
using DarkRift.Client.Unity;
using DarkRift.Client;
using System;

public class ClientNetworkManager : Singleton<ClientNetworkManager> {

	public UnityClient client;
	public bool forceLocalhost = false;

	public delegate void OnServerJoinUpdate(string message);
	public event OnServerJoinUpdate ServerJoinUpdate = delegate { };
	public delegate void OnServerJoinSuccess(string message);
	public event OnServerJoinSuccess ServerJoinSuccess = delegate { };
	public delegate void OnServerJoinFailure(string message);
	public event OnServerJoinFailure ServerJoinFailure = delegate { };

	public bool IsConnectedToServer { get; private set; } //have we connected to server and also validated matchmaking ticket?
	private ClientGameManager clientGameManager;
	private ClientEntityManager entityManager;

	protected override void Awake() {
		base.Awake();
		IsConnectedToServer = false;
	}

	private void Start() {
		clientGameManager = ClientGameManager.Instance;
		entityManager = ClientEntityManager.Instance;
		
		client.MessageReceived += DarkRift_MessageReceived;
	}

	public void JoinServer(string ipAddress, string matchmakerTicket) {
		if (!client.Connected) {
			if (forceLocalhost) {
				ipAddress = "127.0.0.1";
			}

			try {
				client.Connect(System.Net.IPAddress.Parse(ipAddress), client.Port, client.IPVersion);
				if (client.Connected) {
					Debug.Log("DarkRift: Requesting to join server at IP address: " + ipAddress);
					ServerJoinUpdate?.Invoke("DarkRift: Requesting to join server at IP address: " + ipAddress);

					using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
						writer.Write(matchmakerTicket);
						
						using (Message message = Message.Create(NetworkTags.Connection, writer)) {
							client.SendMessage(message, SendMode.Reliable);
						}
					}
				} else {
					Debug.LogError("DarkRift: Connection failed to server at IP address: " + ipAddress);
					ServerJoinFailure?.Invoke("DarkRift: Connection failed to server at IP address: " + ipAddress);
				}
			} catch (System.Exception e) {
				Debug.LogError("DarkRift: Connection failed to server at IP address: " + ipAddress + "\nError: " + e);
				ServerJoinFailure?.Invoke("DarkRift: Connection failed to server at IP address: " + ipAddress);
			}
		}
	}

	public void OnJoinServerUpdate(string message) {
		Debug.Log(message);
		ServerJoinUpdate?.Invoke(message);
	}

	public void OnJoinServerFailed(string errorMessage) {
		Debug.LogError(errorMessage);
		ServerJoinFailure?.Invoke(errorMessage);
	}

	void DarkRift_MessageReceived(object sender, MessageReceivedEventArgs e) {
		using (Message message = e.GetMessage())
		using (DarkRiftReader reader = message.GetReader()) {
			if (message.Tag == NetworkTags.EntityUpdate) {
				while (reader.Position < reader.Length) {
					string entityId = reader.ReadString();
					string entityTypeId = reader.ReadString();
					Entity entity = entityManager.GetEntity(entityId);
					if (entity == null) {
						entity = entityManager.CreateEntity(entityTypeId);
						reader.ReadSerializableInto(ref entity); //must populate entity before registering since registration depends on entity data
						entityManager.RegisterEntity(entity);
					} else {
						reader.ReadSerializableInto(ref entity);
					}
				}
			} else if (message.Tag == NetworkTags.EntityDeath) {
				entityManager.HandleEntityDeath(reader.ReadString());
			} else if (message.Tag == NetworkTags.GameState) {
				clientGameManager.UpdateGameState(reader.ReadSerializable<GameState>(), reader.ReadBoolean());
			} else if (message.Tag == NetworkTags.CapturePoint) {
				while (reader.Position < reader.Length) {
					CapturePoint capturePoint = clientGameManager.CapturePoints[reader.ReadUInt16()];
					reader.ReadSerializableInto(ref capturePoint);
				}
			} else if (message.Tag == NetworkTags.ChatMessage) {
				UIManager.Instance.AddChatMessage(clientGameManager.GameState.GetPlayer(reader.ReadString()), reader.ReadString());
			} else if (message.Tag == NetworkTags.UnitList) {
				List<PlayerUnit> playerUnits = new List<PlayerUnit>();
				while (reader.Position < reader.Length) {
					playerUnits.Add(reader.ReadSerializable<PlayerUnit>());
				}
				UIManager.Instance.OnUnitListReceived(playerUnits);
			} else if (message.Tag == NetworkTags.PlayerJoined) {
				clientGameManager.OnPlayerJoined(reader.ReadSerializable<Player>());
			} else if (message.Tag == NetworkTags.PlayerLeft) {
				clientGameManager.OnPlayerLeft(reader.ReadSerializable<Player>());
			} else if (message.Tag == NetworkTags.Connection) {
				//Response to join request indicating if our matchmaking ticket was valid
				if (reader.ReadBoolean()) {
					IsConnectedToServer = true;
					//Notify anyone interested that we've completed joining a server
					Debug.Log("PlayFab: Ticket accepted by server, join completed.");
					ServerJoinSuccess?.Invoke("PlayFab: Ticket accepted by server, join completed.");
				} else {
					Debug.LogError("PlayFab: Ticket rejected by server, disconnecting.");
					ServerJoinFailure?.Invoke("PlayFab: Ticket rejected by server, disconnecting.");
					client.Disconnect();
				}
			}
		}
	}

	public void SendCommand(Command command) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(command);

			using (Message message = Message.Create(NetworkTags.Command, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public bool RequestUnitList() {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			using (Message message = Message.Create(NetworkTags.UnitList, writer)) {
				return client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void SendSquadSelection(List<int> unitIds) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(unitIds.ToArray());

			using (Message message = Message.Create(NetworkTags.SquadSelection, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void SendConstruction(string structureTypeId, Vector3 position) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(structureTypeId);
			writer.Write(position.x); writer.Write(position.y); writer.Write(position.z);

			using (Message message = Message.Create(NetworkTags.Construction, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void SendChatMessage(string messageText) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(messageText);

			using (Message message = Message.Create(NetworkTags.ChatMessage, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}
}
