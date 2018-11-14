using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DarkRift.Server;
using DarkRift.Server.Unity;
using System;
using PlayFab.ServerModels;

public class ServerNetworkManager : Singleton<ServerNetworkManager> {
	
	public UnityServer server;
	public Dictionary<IClient, UserAccountInfo> LobbyClientAccountMap { get; private set; }
	public Dictionary<IClient, UserAccountInfo> InGameClientAccountMap { get; private set; }

	private Dictionary<string, IClient> tempTicketMap = new Dictionary<string, IClient>();

	protected override void Awake() {
		base.Awake();

		LobbyClientAccountMap = new Dictionary<IClient, UserAccountInfo>();
		InGameClientAccountMap = new Dictionary<IClient, UserAccountInfo>();
	}

	private void Start() {
		server.Server.ClientManager.ClientConnected += DarkRift_OnClientConnected;
		server.Server.ClientManager.ClientDisconnected += DarkRift_OnClientDisconnected;
	}

	private void DarkRift_OnClientConnected(object sender, ClientConnectedEventArgs e) {
		e.Client.MessageReceived += DarkRift_OnMessageReceived;
	}

	private void DarkRift_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
		string playFabId = null;

		if (LobbyClientAccountMap.ContainsKey(e.Client)) {
			playFabId = LobbyClientAccountMap[e.Client].PlayFabId;
			LobbyClientAccountMap.Remove(e.Client);
		}

		if (InGameClientAccountMap.ContainsKey(e.Client)) {
			playFabId = InGameClientAccountMap[e.Client].PlayFabId;
			Player player = ServerGameManager.Instance.PlayerLeft(playFabId);

			InGameClientAccountMap.Remove(e.Client);

			if (player != null) {
				//Alert other players of the disconnection
				using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
					writer.Write(player);

					using (Message message = Message.Create(NetworkTags.PlayerLeft, writer)) {
						foreach (IClient otherClient in InGameClientAccountMap.Keys) {
							otherClient.SendMessage(message, SendMode.Reliable);
						}
					}
				}
			}
		}

		if (playFabId != null) {
			ServerPlayFabManager.Instance.NotifyPlayerLeft(playFabId);
		}
	}

	private void DarkRift_OnMessageReceived(object sender, MessageReceivedEventArgs e) {
		using (Message message = e.GetMessage())
		using (DarkRiftReader reader = message.GetReader()) {
			if (InGameClientAccountMap.ContainsKey(e.Client)) {
				//valid, joined player
				if (ServerGameManager.Instance.GameState.CurrentState == GameStates.GAME_IN_PROGRESS) {
					//game is in progress
					if (message.Tag == NetworkTags.Command) {
						ServerEntityManager.Instance.HandleCommand(reader.ReadSerializable<EntityCommand>(), InGameClientAccountMap[e.Client].PlayFabId);
					} else if (message.Tag == NetworkTags.Construction) {
						ServerEntityManager.Instance.SpawnStructure(ServerGameManager.Instance.GameState.GetPlayer(InGameClientAccountMap[e.Client].PlayFabId),
							reader.ReadString(),
							new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
					} else if (message.Tag == NetworkTags.PlayerEvent) {
						ServerEntityManager.Instance.SpawnPlayerEvent(ServerGameManager.Instance.GameState.GetPlayer(InGameClientAccountMap[e.Client].PlayFabId),
							reader.ReadString(),
							new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
					} else if (message.Tag == NetworkTags.ChatMessage) {
						string messageText = reader.ReadString();

						using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
							writer.Write(InGameClientAccountMap[e.Client].PlayFabId);
							writer.Write(messageText);

							using (Message response = Message.Create(NetworkTags.ChatMessage, writer)) {
								e.Client.SendMessage(response, SendMode.Reliable);
							}
						}
					} else if (message.Tag == NetworkTags.UnitList) {
						List<PlayerUnit> playerUnits = ServerEntityManager.Instance.GetUsablePlayerUnits(InGameClientAccountMap[e.Client].PlayFabId);
						if (playerUnits == null) {
							playerUnits = DatabaseManager.GetPlayerUnits(InGameClientAccountMap[e.Client].PlayFabId);
							ServerEntityManager.Instance.SetPlayerUnits(InGameClientAccountMap[e.Client].PlayFabId, playerUnits);
						}

						using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
							foreach (PlayerUnit playerUnit in playerUnits) {
								writer.Write(playerUnit);
							}

							using (Message response = Message.Create(NetworkTags.UnitList, writer)) {
								e.Client.SendMessage(response, SendMode.Reliable);
							}
						}
					} else if (message.Tag == NetworkTags.SquadSelection) {
						List<SelectedPlayerUnit> selectedUnits = new List<SelectedPlayerUnit>();
						while (reader.Position < reader.Length) {
							selectedUnits.Add(reader.ReadSerializable<SelectedPlayerUnit>());
						}
						ServerEntityManager.Instance.SpawnPlayerSquad(ServerGameManager.Instance.GameState.GetPlayer(InGameClientAccountMap[e.Client].PlayFabId), selectedUnits);
					}
				}
			} else if (LobbyClientAccountMap.ContainsKey(e.Client)) {
				if (message.Tag == NetworkTags.FullUnitList) {
					List<PlayerUnit> playerUnits = DatabaseManager.GetPlayerUnits(LobbyClientAccountMap[e.Client].PlayFabId);

					using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
						foreach (PlayerUnit playerUnit in playerUnits) {
							writer.Write(playerUnit);
						}

						using (Message response = Message.Create(NetworkTags.FullUnitList, writer)) {
							e.Client.SendMessage(response, SendMode.Reliable);
						}
					}
				} else if (message.Tag == NetworkTags.CustomizedUnits) {
					List<SelectedPlayerUnit> customizedUnits = new List<SelectedPlayerUnit>();
					while (reader.Position < reader.Length) {
						customizedUnits.Add(reader.ReadSerializable<SelectedPlayerUnit>());
					}
					foreach (SelectedPlayerUnit customizedUnit in customizedUnits) {
						DatabaseManager.SavePlayerUnit(LobbyClientAccountMap[e.Client].PlayFabId, customizedUnit);
					}
				}
			}

			if (message.Tag == NetworkTags.Connection) {
				//Matchmaker join request from a client, submit ticket to PlayFab
				string matchmakerTicket = reader.ReadString();
				tempTicketMap.Add(matchmakerTicket, e.Client);
				ServerPlayFabManager.Instance.RedeemMatchmakerTicket(matchmakerTicket);
			} else if (message.Tag == NetworkTags.JoinGame) {
				UserJoinGame(e.Client);
			}
		}
	}

	public void FinalizeJoinRequest(string matchmakerTicket, UserAccountInfo userInfo, bool approved) {
		IClient client = tempTicketMap[matchmakerTicket];
		tempTicketMap.Remove(matchmakerTicket);

		//Respond to client with approval status
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(approved);

			using (Message message = Message.Create(NetworkTags.Connection, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}

		//Record client in lobby map
		if (LobbyClientAccountMap.ContainsKey(client)) {
			LobbyClientAccountMap[client] = userInfo;
		} else {
			LobbyClientAccountMap.Add(client, userInfo);
		}
	}

	protected void UserJoinGame(IClient client) {
		UserAccountInfo userInfo = null;
		if (LobbyClientAccountMap.ContainsKey(client)) {
			userInfo = LobbyClientAccountMap[client];
			LobbyClientAccountMap.Remove(client);
		} else if (InGameClientAccountMap.ContainsKey(client)) {
			userInfo = InGameClientAccountMap[client];
		}

		//Record client in game map
		if (InGameClientAccountMap.ContainsKey(client)) {
			InGameClientAccountMap[client] = userInfo;
		} else {
			InGameClientAccountMap.Add(client, userInfo);
		}

		Player player = ServerGameManager.Instance.PlayerJoined(userInfo.PlayFabId, userInfo.TitleInfo.DisplayName);
		PlayerData playerData = DatabaseManager.GetPlayerData(userInfo.PlayFabId);
		player.MaxSquadCost = playerData.MaxSquadCost;

		//Alert other players of the new player
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(player);

			using (Message message = Message.Create(NetworkTags.PlayerJoined, writer)) {
				foreach (IClient otherClient in InGameClientAccountMap.Keys) {
					if (!otherClient.Equals(client)) {
						otherClient.SendMessage(message, SendMode.Reliable);
					}
				}
			}
		}

		//Send current game state to newly-joined player
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(ServerGameManager.Instance.GameState);
			//whether player already has entities spawned
			writer.Write(ServerEntityManager.Instance.GetUnitsForPlayer(player.ID).Count > 0);

			using (Message message = Message.Create(NetworkTags.GameState, writer)) {
				client.SendMessage(message, SendMode.Reliable);
			}
		}

		ServerGameManager.Instance.TryToStartGame();
	}

	public void StartGame(GameState gameState) {
		//Send current game state to all players
		BroadcastGameState(gameState);
	}

	public void EndGame(GameState gameState) {
		//Send current game state to all players
		BroadcastGameState(gameState);

		//Mark all players as back in the lobby
		foreach (KeyValuePair<IClient, UserAccountInfo> client in InGameClientAccountMap) {
			LobbyClientAccountMap.Add(client.Key, client.Value);
		}
		InGameClientAccountMap.Clear();
	}

	public void BroadcastGameState(GameState gameState) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(gameState);
			writer.Write(false);

			using (Message message = Message.Create(NetworkTags.GameState, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void BroadcastCapturePoints(List<CapturePoint> capturePoints) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			foreach (CapturePoint capturePoint in capturePoints) {
				writer.Write(capturePoint.ID);
				writer.Write(capturePoint);
			}

			using (Message message = Message.Create(NetworkTags.CapturePoint, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void BroadcastEntities(List<Entity> entities) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			foreach (Entity entity in entities) {
				writer.Write(entity.ID);
				writer.Write(entity.typeId);
				writer.Write(entity);
			}

			using (Message message = Message.Create(NetworkTags.EntityUpdate, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void BroadcastPlayerEvents(List<PlayerEvent> playerEvents) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			foreach (PlayerEvent playerEvent in playerEvents) {
				writer.Write(playerEvent.ID);
				writer.Write(playerEvent.typeId);
				writer.Write(playerEvent);
			}

			using (Message message = Message.Create(NetworkTags.PlayerEventUpdate, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void SendEntityDeath(Entity entity) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(entity.ID);

			using (Message message = Message.Create(NetworkTags.EntityDeath, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void SendEntityDespawn(Entity entity) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(entity.ID);

			using (Message message = Message.Create(NetworkTags.EntityDespawn, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void SendPlayerEventEnd(PlayerEvent playerEvent) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(playerEvent.ID);

			using (Message message = Message.Create(NetworkTags.PlayerEventEnd, writer)) {
				foreach (IClient client in InGameClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}
}
