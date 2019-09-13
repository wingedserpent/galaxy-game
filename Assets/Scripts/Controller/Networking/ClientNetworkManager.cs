using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using System.Linq;
using DarkRift.Client.Unity;
using DarkRift.Client;
using System;
using UnityEngine.SceneManagement;

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
	
	private bool waitingToJoin = false;

	protected override void Awake() {
		base.Awake();
		IsConnectedToServer = false;
	}

	private void Start() {
		client.MessageReceived += DarkRift_MessageReceived;
	}

	public void JoinServer(string ipAddress, string matchmakerTicket) {
		if (!client.Connected) {
			if (forceLocalhost) {
				ipAddress = "127.0.0.1";
			}

			try {
				Debug.Log("DarkRift: Requesting to join server at IP address: " + ipAddress);
				ServerJoinUpdate?.Invoke("DarkRift: Requesting to join server at IP address: " + ipAddress);
				client.Connect(System.Net.IPAddress.Parse(ipAddress), client.Port, client.IPVersion);
			} catch (System.Exception e) {
				Debug.LogError("DarkRift: Connection failed to server at IP address: " + ipAddress + "\nError: " + e);
				ServerJoinFailure?.Invoke("DarkRift: Connection failed to server at IP address: " + ipAddress);
			}
		}

		if (client.Connected) {
			Debug.Log("DarkRift: Writing matchmaker ticket to server...");
			ServerJoinUpdate?.Invoke("DarkRift: Writing matchmaker ticket to server...");
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
			if (OverallStateManager.Instance.OverallState == OverallState.IN_GAME) {
				if (ClientGameManager.Instance.ClientState == GameStates.GAME_IN_PROGRESS
					|| ClientGameManager.Instance.ClientState == GameStates.WAITING_FOR_PLAYERS) {
					//game is in progress
					if (message.Tag == NetworkTags.EntityUpdate) {
						ClientEntityManager entityManager = ClientEntityManager.Instance;
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
					} else if (message.Tag == NetworkTags.PlayerEventUpdate) {
						ClientEntityManager entityManager = ClientEntityManager.Instance;
						while (reader.Position < reader.Length) {
							string playerEventId = reader.ReadString();
							string playerEventTypeId = reader.ReadString();
							PlayerEvent playerEvent = entityManager.GetPlayerEvent(playerEventId);
							if (playerEvent == null) {
								playerEvent = entityManager.CreatePlayerEvent(playerEventTypeId);
								reader.ReadSerializableInto(ref playerEvent); //must populate event before registering since registration depends on event data
								entityManager.RegisterPlayerEvent(playerEvent);
							} else {
								reader.ReadSerializableInto(ref playerEvent);
							}
						}
					} else if (message.Tag == NetworkTags.EntityDeath) {
						ClientEntityManager.Instance.HandleEntityDeath(reader.ReadString());
					} else if (message.Tag == NetworkTags.EntityDespawn) {
						ClientEntityManager.Instance.HandleEntityDespawn(reader.ReadString());
					} else if (message.Tag == NetworkTags.PlayerEventEnd) {
						ClientEntityManager.Instance.HandlePlayerEventEnd(reader.ReadString());
					} else if (message.Tag == NetworkTags.CapturePoint) {
						while (reader.Position < reader.Length) {
							CapturePoint capturePoint = ClientGameManager.Instance.CapturePoints[reader.ReadUInt16()];
							reader.ReadSerializableInto(ref capturePoint);
						}
					} else if (message.Tag == NetworkTags.GameState) {
						ClientGameManager.Instance.UpdateGameState(reader.ReadSerializable<GameState>(), reader.ReadBoolean());
					}
				}

				if (message.Tag == NetworkTags.ChatMessage) {
					UIManager.Instance.AddChatMessage(ClientGameManager.Instance.GameState.GetPlayer(reader.ReadString()), reader.ReadString());
				} else if (message.Tag == NetworkTags.UnitList) {
					List<PlayerUnit> playerUnits = new List<PlayerUnit>();
					while (reader.Position < reader.Length) {
						playerUnits.Add(reader.ReadSerializable<PlayerUnit>());
					}
					UIManager.Instance.OnUnitListReceived(playerUnits);
				} else if (message.Tag == NetworkTags.PlayerJoined) {
					ClientGameManager.Instance.OnPlayerJoined(reader.ReadSerializable<Player>());
				} else if (message.Tag == NetworkTags.PlayerLeft) {
					ClientGameManager.Instance.OnPlayerLeft(reader.ReadSerializable<Player>());
				}
			} else if (OverallStateManager.Instance.OverallState != OverallState.IN_GAME) {
				if (waitingToJoin) {
					if (message.Tag == NetworkTags.GameState) {
						waitingToJoin = false;
						OverallStateManager.Instance.LoadGame(reader.ReadSerializable<GameState>(), reader.ReadBoolean());
					}
				} else {
					if (message.Tag == NetworkTags.FullUnitList) {
						List<PlayerUnit> playerUnits = new List<PlayerUnit>();
						while (reader.Position < reader.Length) {
							playerUnits.Add(reader.ReadSerializable<PlayerUnit>());
						}
						MainMenuManager.Instance.OnUnitListReceived(playerUnits);
					}
				}
			}

			if (message.Tag == NetworkTags.Connection) {
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

	public void SendJoinGameRequest() {
		waitingToJoin = true;
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			using (Message message = Message.Create(NetworkTags.JoinGame, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void SendCommand(EntityCommand command) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(command);

			using (Message message = Message.Create(NetworkTags.Command, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void RequestUnitList() {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			using (Message message = Message.Create(NetworkTags.UnitList, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void SendSquadSelection(List<SelectedPlayerUnit> selectedUnits) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			foreach (SelectedPlayerUnit selectedUnit in selectedUnits) {
				writer.Write(selectedUnit);
			}

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

	public void SendPlayerEvent(string playerEventTypeId, Vector3 position) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(playerEventTypeId);
			writer.Write(position.x); writer.Write(position.y); writer.Write(position.z);

			using (Message message = Message.Create(NetworkTags.PlayerEvent, writer)) {
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

	public void RequestAllUnits() {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			using (Message message = Message.Create(NetworkTags.FullUnitList, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}

	public void SendCustomizedUnits(List<SelectedPlayerUnit> selectedUnits) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			foreach (SelectedPlayerUnit selectedUnit in selectedUnits) {
				writer.Write(selectedUnit);
			}

			using (Message message = Message.Create(NetworkTags.CustomizedUnits, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}
	}
}
