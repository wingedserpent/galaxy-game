﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

public class ServerEntityManager : Singleton<ServerEntityManager> {
	
	private static readonly string CLIENT_ANY_TAG = "ClientAny";
	private static readonly string CLIENT_SELF_TAG = "ClientSelf";

	public EntityDatabase entityDatabase;
	public LayerMask constructionOverlapLayers;

	private Dictionary<string, List<PlayerUnit>> playerUnits = new Dictionary<string, List<PlayerUnit>>();
	private Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
	
	private ServerNetworkManager serverNetworkManager;
	private ServerGameManager serverGameManager;

	void Start() {
		serverNetworkManager = ServerNetworkManager.Instance;
		serverGameManager = ServerGameManager.Instance;

		EntityController.OnDeath += HandleEntityDeath;
		
		//register any entities already existing in the scene (for dev/testing convenience)
		foreach (Entity entity in FindObjectsOfType<Entity>()) {
			RegisterEntity(entity);
		}
	}

	void Update() {
		serverNetworkManager.BroadcastEntities(entities.Values.ToList());
	}

	public List<PlayerUnit> GetUsablePlayerUnits(string playerId) {
		if (playerUnits.ContainsKey(playerId)) {
			return playerUnits[playerId].Where(x => x.CurrentHealth > 0).ToList();
		}
		return null;
	}

	public void SetPlayerUnits(string playerId, List<PlayerUnit> units) {
		playerUnits.Add(playerId, units);
	}

	public Entity GetEntity(string entityId) {
		if (entities.ContainsKey(entityId)) {
			return entities[entityId];
		}
		return null;
	}

	public List<Unit> GetUnitsForPlayer(string playerId) {
		return (from entity in entities.Values
				where entity is Unit && entity.PlayerId.Equals(playerId)
				select (Unit)entity).ToList();
	}

	public void SpawnPlayerSquad(Player player, List<PlayerUnit> playerUnits) {
		Vector3 spawnPos = serverGameManager.TeamSpawns[player.TeamId].transform.position;
		foreach (PlayerUnit playerUnit in playerUnits) {
			Unit newUnit = (Unit)entityDatabase.GetEntityInstance(playerUnit.UnitType, spawnPos, Quaternion.identity);
			newUnit.SetPlayer(player);
			newUnit.PlayerUnitId = playerUnit.PlayerUnitId;
			RegisterEntity(newUnit);
		}
	}

	public void SpawnStructure(Player player, string structureTypeId, Vector3 spawnPos) {
		Entity entityRef = entityDatabase.GetEntityReference(structureTypeId);
		if (player.Resources >= (entityRef as Structure).resourceCost) {
			//navmesh check
			NavMeshHit navHit;
			if (NavMesh.SamplePosition(spawnPos, out navHit, 0.1f, NavMesh.AllAreas)) {
				GameObject construction = entityDatabase.GetConstruction(structureTypeId);
				construction.transform.position = spawnPos;
				Collider constructionCollider = construction.GetComponentInChildren<Collider>();

				//collision/overlap check
				Collider[] colliders = Physics.OverlapBox(constructionCollider.bounds.center,
					constructionCollider.bounds.extents, constructionCollider.transform.rotation, constructionOverlapLayers);
				if (colliders.Count(x => x != constructionCollider) == 0) {
					Structure newStructure = (Structure)entityDatabase.GetEntityInstance(structureTypeId, spawnPos, Quaternion.identity);
					newStructure.SetPlayer(player);
					RegisterEntity(newStructure);
					
					serverGameManager.DecreaseResources(player, (entityRef as Structure).resourceCost);
				}

				Destroy(construction);
			}
		}
	}

	public void RegisterEntity(Entity entity) {
		if (!entities.ContainsKey(entity.ID)) {
			ScrubEntity(entity);
			entities.Add(entity.ID, entity);
		} else {
			Debug.LogWarning("An entity with ID: " + entity.ID + " already exists! Destroying new entity.");
			Destroy(entity.gameObject);
		}
	}

	public void DestroyEntity(string entityId) {
		if (entities.ContainsKey(entityId)) {
			Destroy(entities[entityId].gameObject);
			entities.Remove(entityId);
		}
	}

	private void ScrubEntity(Entity entity) {
		//remove any children that don't need to exist on the server
		foreach (Transform child in entity.transform) {
			if (child.CompareTag(CLIENT_ANY_TAG) || child.CompareTag(CLIENT_SELF_TAG)) {
				Destroy(child.gameObject);
			}
		}

		//destroy any remaining visibility togglers
		foreach (VisibilityToggle visibilityToggle in entity.GetComponentsInChildren<VisibilityToggle>()) {
			Destroy(visibilityToggle);
		}

		//destroy any remaining animators
		foreach (Animator animator in entity.GetComponentsInChildren<Animator>()) {
			Destroy(animator);
		}

		//destroy any remaining audio sources
		foreach (AudioSource audioSource in entity.GetComponents<AudioSource>()) {
			Destroy(audioSource);
		}
	}

	public void HandleCommand(Command command, string sourcePlayerId) {
		List<Entity> actingEntities = new List<Entity>();
		foreach (string entityId in command.ActingEntityIds) {
			if (entities.ContainsKey(entityId)) {
				actingEntities.Add(entities[entityId]);
			}
		}

		Vector3? groupMovementCenter = null;
		if (command.Type == CommandType.MOVE && actingEntities.Count > 1) {
			groupMovementCenter = Vector3.zero;
			foreach (Entity entity in actingEntities) {
				groupMovementCenter += entity.transform.position;
			}
			groupMovementCenter = groupMovementCenter / actingEntities.Count;
		}

		foreach (Entity entity in actingEntities) {
			if (entity.PlayerId.Equals(sourcePlayerId)) {
				if (command.Type == CommandType.MOVE && entity.CanMove) {
					entity.EntityController.Move(command.Point, groupMovementCenter);
				} else if (command.Type == CommandType.STOP) {
					entity.EntityController.Stop();
				} else if (command.Type == CommandType.ATTACK && entity.CanAttack) {
					if (entities.ContainsKey(command.TargetEntityId)) {
						entity.EntityController.Attack(entities[command.TargetEntityId]);
					}
				} else {
					entity.EntityController.ExecuteCommand(command.Type);
				}
			}
		}
	}

	public void HandleEntityDeath(Entity entity) {
		entity.EntityController.Die();
		
		if (entity is Unit && playerUnits.ContainsKey(entity.PlayerId)) {
			PlayerUnit playerUnit = (from pu in playerUnits[entity.PlayerId]
									 where pu.PlayerUnitId == (entity as Unit).PlayerUnitId
									 select pu).FirstOrDefault();
			if (playerUnit != null) {
				playerUnit.CurrentHealth = 0;

				if (CheckGameEnd(entity)) {
					serverGameManager.EndGame();
				}
			}
		}

		serverNetworkManager.SendEntityDeath(entity);

		DestroyEntity(entity.ID);
	}

	private bool CheckGameEnd(Entity lastDeadEntity) {
		//check affected player first
		foreach (PlayerUnit playerUnit in playerUnits[lastDeadEntity.PlayerId]) {
			if (playerUnit.CurrentHealth > 0) {
				return false;
			}
		}

		//check rest of players on affected team
		foreach (Player player in serverGameManager.GameState.Teams[lastDeadEntity.TeamId].Players.Values.Where(x => !x.ID.Equals(lastDeadEntity.PlayerId))) {
			foreach (PlayerUnit playerUnit in playerUnits[player.ID]) {
				if (playerUnit.CurrentHealth > 0) {
					return false;
				}
			}
		}
		return true; //all playerunits for entire team are dead
	}
}
