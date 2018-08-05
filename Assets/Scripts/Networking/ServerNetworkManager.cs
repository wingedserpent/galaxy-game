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
	public Dictionary<string, IClient> ClientConnectionMap { get; private set; } //may not be needed, clientaccountmap may be enough
	public Dictionary<IClient, UserAccountInfo> ClientAccountMap { get; private set; }

	private Dictionary<string, IClient> tempTicketMap = new Dictionary<string, IClient>();
	private ServerPlayFabManager serverPlayFabManager;
	private ServerGameManager serverGameManager;
	private ServerEntityManager entityManager;

	protected override void Awake() {
		base.Awake();
		ClientConnectionMap = new Dictionary<string, IClient>();
		ClientAccountMap = new Dictionary<IClient, UserAccountInfo>();
	}

	private void Start() {
		serverPlayFabManager = ServerPlayFabManager.Instance;
		serverGameManager = ServerGameManager.Instance;
		entityManager = ServerEntityManager.Instance;
		
		server.Server.ClientManager.ClientConnected += DarkRift_OnClientConnected;
		server.Server.ClientManager.ClientDisconnected += DarkRift_OnClientDisconnected;
	}

	private void DarkRift_OnClientConnected(object sender, ClientConnectedEventArgs e) {
		e.Client.MessageReceived += DarkRift_OnMessageReceived;
	}

	private void DarkRift_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
		if (ClientAccountMap.ContainsKey(e.Client)) {
			Player player = serverGameManager.PlayerLeft(ClientAccountMap[e.Client].PlayFabId);

			serverPlayFabManager.NotifyPlayerLeft(ClientAccountMap[e.Client].PlayFabId);
			ClientConnectionMap.Remove(ClientAccountMap[e.Client].PlayFabId);
			ClientAccountMap.Remove(e.Client);

			//Alert other players of the disconnection
			using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
				writer.Write(player);

				using (Message message = Message.Create(NetworkTags.PlayerLeft, writer)) {
					foreach (IClient otherClient in ClientAccountMap.Keys) {
						otherClient.SendMessage(message, SendMode.Reliable);
					}
				}
			}

		}
	}

	private void DarkRift_OnMessageReceived(object sender, MessageReceivedEventArgs e) {
		using (Message message = e.GetMessage())
		using (DarkRiftReader reader = message.GetReader()) {
			if (ClientAccountMap.ContainsKey(e.Client)) {
				//valid, joined player
				if (serverGameManager.GameState.CurrentState == GameStates.GAME_IN_PROGRESS) {
					//game is in progress
					if (message.Tag == NetworkTags.Command) {
						entityManager.HandleCommand(reader.ReadSerializable<Command>(), ClientAccountMap[e.Client].PlayFabId);
					} else if (message.Tag == NetworkTags.Construction) {
						entityManager.SpawnStructure(serverGameManager.GameState.GetPlayer(ClientAccountMap[e.Client].PlayFabId),
							reader.ReadString(),
							new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
					} else if (message.Tag == NetworkTags.ChatMessage) {
						string messageText = reader.ReadString();

						using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
							writer.Write(ClientAccountMap[e.Client].PlayFabId);
							writer.Write(messageText);

							using (Message response = Message.Create(NetworkTags.ChatMessage, writer)) {
								e.Client.SendMessage(response, SendMode.Reliable);
							}
						}
					} else if (message.Tag == NetworkTags.UnitList) {
						List<PlayerUnit> playerUnits = entityManager.GetUsablePlayerUnits(ClientAccountMap[e.Client].PlayFabId);
						if (playerUnits == null) {
							playerUnits = DatabaseManager.GetPlayerUnits(ClientAccountMap[e.Client].PlayFabId);
							entityManager.SetPlayerUnits(ClientAccountMap[e.Client].PlayFabId, playerUnits);
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
						int[] unitIds = reader.ReadInt32s();
						List<PlayerUnit> playerUnits = entityManager.GetUsablePlayerUnits(ClientAccountMap[e.Client].PlayFabId);
						entityManager.SpawnPlayerSquad(serverGameManager.GameState.GetPlayer(ClientAccountMap[e.Client].PlayFabId), playerUnits.Where(x => unitIds.Contains(x.PlayerUnitId)).ToList());
					}
				}
			} else if (message.Tag == NetworkTags.Connection) {
				//Matchmaker join request from a client, submit ticket to PlayFab
				string matchmakerTicket = reader.ReadString();
				tempTicketMap.Add(matchmakerTicket, e.Client);
				serverPlayFabManager.RedeemMatchmakerTicket(matchmakerTicket);
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

		if (approved) {
			//Record client in maps
			if (ClientAccountMap.ContainsKey(client)) {
				ClientConnectionMap[userInfo.PlayFabId] = client;
				ClientAccountMap[client] = userInfo;
			} else {
				ClientConnectionMap.Add(userInfo.PlayFabId, client);
				ClientAccountMap.Add(client, userInfo);
			}

			Player player = serverGameManager.PlayerJoined(userInfo.PlayFabId, userInfo.TitleInfo.DisplayName);
			PlayerData playerData = DatabaseManager.GetPlayerData(userInfo.PlayFabId);
			player.MaxSquadCost = playerData.MaxSquadCost;

			//Alert other players of the new player
			using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
				writer.Write(player);

				using (Message message = Message.Create(NetworkTags.PlayerJoined, writer)) {
					foreach (IClient otherClient in ClientAccountMap.Keys) {
						if (!otherClient.Equals(client)) {
							otherClient.SendMessage(message, SendMode.Reliable);
						}
					}
				}
			}

			//Send current game state to newly-joined player
			using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
				writer.Write(serverGameManager.GameState);
				//whether player already has entities spawned
				writer.Write(entityManager.GetUnitsForPlayer(player.ID).Count > 0);

				using (Message message = Message.Create(NetworkTags.GameState, writer)) {
					client.SendMessage(message, SendMode.Reliable);
				}
			}

			serverGameManager.TryToStartGame();
		}
	}

	public void StartGame(GameState gameState) {
		//Send current game state to all players
		BroadcastGameState(gameState);
	}

	public void EndGame(GameState gameState) {
		//Send current game state to all players
		BroadcastGameState(gameState);
	}

	public void BroadcastGameState(GameState gameState) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(gameState);
			writer.Write(false);

			using (Message message = Message.Create(NetworkTags.GameState, writer)) {
				foreach (IClient client in ClientAccountMap.Keys) {
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
				foreach (IClient client in ClientAccountMap.Keys) {
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
				foreach (IClient client in ClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}

	public void SendEntityDeath(Entity entity) {
		using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
			writer.Write(entity.ID);

			using (Message message = Message.Create(NetworkTags.EntityDeath, writer)) {
				foreach (IClient client in ClientAccountMap.Keys) {
					client.SendMessage(message, SendMode.Unreliable);
				}
			}
		}
	}
}
