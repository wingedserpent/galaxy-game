using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

public class ServerEntityManager : Singleton<ServerEntityManager> {
	
	private static readonly string CLIENT_ANY_TAG = "ClientAny";
	private static readonly string CLIENT_SELF_TAG = "ClientSelf";

	public EntityDatabase entityDatabase;

	private Dictionary<string, List<PlayerUnit>> playerUnits = new Dictionary<string, List<PlayerUnit>>();
	private Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
	private Dictionary<string, PlayerEvent> playerEvents = new Dictionary<string, PlayerEvent>();

	private ServerNetworkManager serverNetworkManager;
	private ServerGameManager serverGameManager;

	void Start() {
		serverNetworkManager = ServerNetworkManager.Instance;
		serverGameManager = ServerGameManager.Instance;

		Entity.OnDeath += HandleEntityDeath;
		Entity.OnDespawn += HandleEntityDespawn;
		PlayerEvent.OnEventEnd += HandlePlayerEventEnd;

		//register any entities already existing in the scene (for dev/testing convenience)
		foreach (Entity entity in FindObjectsOfType<Entity>()) {
			RegisterEntity(entity);
		}
	}

	void Update() {
		serverNetworkManager.BroadcastEntities(entities.Values.ToList());
		serverNetworkManager.BroadcastPlayerEvents(playerEvents.Values.ToList());
	}

	public List<PlayerUnit> GetUsablePlayerUnits(string playerId) {
		if (playerUnits.ContainsKey(playerId)) {
			return playerUnits[playerId].Where(x => x.currentHealth > 0).ToList();
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
				where entity is Unit && entity.playerId.Equals(playerId)
				select (Unit)entity).ToList();
	}

	public void SpawnPlayerSquad(Player player, List<SelectedPlayerUnit> selectedUnits) {
		List<PlayerUnit> usablePlayerUnits = GetUsablePlayerUnits(player.id);
		Vector3 spawnPos = serverGameManager.TeamSpawns[player.teamId].transform.position;

		foreach (SelectedPlayerUnit selectedUnit in selectedUnits) {
			PlayerUnit usablePlayerUnit = usablePlayerUnits.Where(x => x.playerUnitId == selectedUnit.playerUnitId).FirstOrDefault();
			if (usablePlayerUnit != null) {
				Unit newUnit = (Unit)entityDatabase.GetEntityInstance(selectedUnit.unitType, spawnPos, Quaternion.identity);
				newUnit.SetPlayer(player);
				newUnit.playerUnitId = selectedUnit.playerUnitId;
				newUnit.maxHealth = usablePlayerUnit.maxHealth;
				newUnit.currentHealth = usablePlayerUnit.currentHealth;
				newUnit.maxShield = newUnit.currentShield = usablePlayerUnit.maxShield;
				newUnit.moveSpeed = usablePlayerUnit.moveSpeed;
				newUnit.visionRange = usablePlayerUnit.visionRange;
				newUnit.EquipWeapon(usablePlayerUnit.weaponOptions.Where(x => x.name.Equals(selectedUnit.weaponSelection)).FirstOrDefault());
				if (newUnit.equippedWeapon == null && usablePlayerUnit.weaponOptions.Count > 0) {
					newUnit.EquipWeapon(usablePlayerUnit.weaponOptions.Where(x => x.squadCost == 0).FirstOrDefault());
				}
				newUnit.equipment = usablePlayerUnit.equipmentOptions.Where(x => selectedUnit.equipmentSelections.Contains(x.name)).ToList();
				newUnit.ApplyEquipment();

				RegisterEntity(newUnit);
			}
		}
	}

	public void SpawnStructure(Player player, string structureTypeId, Vector3 spawnPos) {
		StructureData structureData = DatabaseManager.GetStructureData(structureTypeId); //TODO cache this or something
		if (player.resources >= structureData.resourceCost) {
			//navmesh check
			NavMeshHit navHit;
			if (NavMesh.SamplePosition(spawnPos, out navHit, 0.1f, NavMesh.AllAreas)) {
				Structure structureRef = entityDatabase.GetEntityReference(structureTypeId) as Structure;
				GameObject construction = Instantiate<GameObject>(structureRef.targetingPrefab);
				construction.transform.position = spawnPos;
				Collider constructionCollider = construction.GetComponentInChildren<Collider>();

				//collision/overlap check
				Collider[] colliders = Physics.OverlapBox(constructionCollider.bounds.center,
					constructionCollider.bounds.extents, constructionCollider.transform.rotation, LayerManager.Instance.constructionOverlapMask);
				if (colliders.Count(x => x != constructionCollider) == 0) {
					Structure newStructure = (Structure)entityDatabase.GetEntityInstance(structureTypeId, spawnPos, Quaternion.identity);
					newStructure.SetPlayer(player);
					newStructure.maxHealth = structureData.maxHealth;
					newStructure.currentHealth = structureData.currentHealth;
					newStructure.maxShield = newStructure.currentShield = structureData.maxShield;
					newStructure.visionRange = structureData.visionRange;
					newStructure.EquipWeapon(structureData.weaponOptions.FirstOrDefault());
					RegisterEntity(newStructure);
					
					serverGameManager.DecreaseResources(player, structureData.resourceCost);
				}

				Destroy(construction);
			}
		}
	}

	public void SpawnPlayerEvent(Player player, string playerEventTypeId, Vector3 spawnPos) {
		PlayerEvent playerEventRef = entityDatabase.GetPlayerEventReference(playerEventTypeId);
		if (player.resources >= playerEventRef.resourceCost) {
			PlayerEvent newPlayerEvent = Instantiate(playerEventRef.gameObject, spawnPos, Quaternion.identity).GetComponent<PlayerEvent>();
			newPlayerEvent.SetPlayer(player);
			RegisterPlayerEvent(newPlayerEvent);

			serverGameManager.DecreaseResources(player, playerEventRef.resourceCost);
		}
	}

	public void RegisterEntity(Entity entity) {
		if (!entities.ContainsKey(entity.uniqueId)) {
			ScrubOwnedObject(entity);
			entities.Add(entity.uniqueId, entity);
		} else {
			Debug.LogWarning("An entity with ID: " + entity.uniqueId + " already exists! Destroying new entity.");
			Destroy(entity.gameObject);
		}
	}

	public void RegisterPlayerEvent(PlayerEvent playerEvent) {
		if (!playerEvents.ContainsKey(playerEvent.uniqueId)) {
			ScrubOwnedObject(playerEvent);
			playerEvents.Add(playerEvent.uniqueId, playerEvent);
		} else {
			Debug.LogWarning("A player event with ID: " + playerEvent.uniqueId + " already exists! Destroying new player event.");
			Destroy(playerEvent.gameObject);
		}
	}

	public void RemoveEntity(string entityId) {
		if (entities.ContainsKey(entityId)) {
			//Destroy(entities[entityId].gameObject); entities now destroy themselves
			entities.Remove(entityId);
		}
	}

	private void DestroyPlayerEvent(string playerEventId) {
		if (playerEvents.ContainsKey(playerEventId)) {
			Destroy(playerEvents[playerEventId].gameObject);
			playerEvents.Remove(playerEventId);
		}
	}

	private void ScrubOwnedObject(OwnedObject ownedObject) {
		//remove any children that don't need to exist on the server
		foreach (Transform child in ownedObject.transform) {
			if (child.CompareTag(CLIENT_ANY_TAG) || child.CompareTag(CLIENT_SELF_TAG)) {
				Destroy(child.gameObject);
			}
		}

		//destroy any remaining visibility togglers
		foreach (VisibilityToggle visibilityToggle in ownedObject.GetComponentsInChildren<VisibilityToggle>()) {
			Destroy(visibilityToggle);
		}

		//destroy any remaining animators
		foreach (Animator animator in ownedObject.GetComponentsInChildren<Animator>()) {
			Destroy(animator);
		}

		//destroy any remaining audio sources
		foreach (AudioSource audioSource in ownedObject.GetComponents<AudioSource>()) {
			Destroy(audioSource);
		}
	}

	public void HandleCommand(EntityCommand command, string sourcePlayerId) {
		List<Entity> actingEntities = new List<Entity>();
		foreach (string entityId in command.actingEntityIds) {
			if (entities.ContainsKey(entityId)) {
				actingEntities.Add(entities[entityId]);
			}
		}

		Vector3? groupMovementCenter = null;
		if (command.type == CommandType.MOVE && actingEntities.Count > 1) {
			groupMovementCenter = Vector3.zero;
			foreach (Entity entity in actingEntities) {
				groupMovementCenter += entity.transform.position;
			}
			groupMovementCenter = groupMovementCenter / actingEntities.Count;
		}

		foreach (Entity entity in actingEntities) {
			if (entity.playerId.Equals(sourcePlayerId)) {
				if (command.type == CommandType.MOVE && entity.canMove) {
					entity.Move(command.point, groupMovementCenter);
				} else if (command.type == CommandType.RETREAT && entity.canMove) {
					entity.Retreat(serverGameManager.TeamSpawns[entity.teamId]);
				} else if (command.type == CommandType.STOP) {
					entity.Stop();
				} else if (command.type == CommandType.ATTACK) {
					if (entities.ContainsKey(command.targetEntityId)) {
						entity.Attack(entities[command.targetEntityId]);
					}
				} else if (command.type == CommandType.ATTACK_LOCATION) {
					entity.Attack(command.point);
				} else if (command.type == CommandType.ABILITY) {
					entity.TriggerAbility(command.abilityTypeId);
				}
			}
		}
	}

	private void HandleEntityDeath(Entity entity) {
		serverNetworkManager.SendEntityDeath(entity);
		//entity.EntityController.Die(0f); not necessary on server

		if (entity is Unit && playerUnits.ContainsKey(entity.playerId)) {
			PlayerUnit playerUnit = (from pu in playerUnits[entity.playerId]
									 where pu.playerUnitId == (entity as Unit).playerUnitId
									 select pu).FirstOrDefault();
			if (playerUnit != null) {
				playerUnit.currentHealth = 0;

				if (CheckGameEnd(entity)) {
					serverGameManager.EndGame();
				}
			}
		}

		RemoveEntity(entity.uniqueId);
		Destroy(entity.gameObject);
	}

	private void HandleEntityDespawn(Entity entity) {
		PlayerUnit playerUnit = (from pu in playerUnits[entity.playerId]
								 where pu.playerUnitId == (entity as Unit).playerUnitId
								 select pu).FirstOrDefault();
		if (playerUnit != null) {
			playerUnit.currentHealth = entity.currentHealth;
		}

		serverNetworkManager.SendEntityDespawn(entity);
		//entity.EntityController.CleanUpForDespawn(); not necessary on server
		RemoveEntity(entity.uniqueId);
	}

	private void HandlePlayerEventEnd(PlayerEvent playerEvent) {
		//playerEvent.End(); not necessary on server

		serverNetworkManager.SendPlayerEventEnd(playerEvent);

		DestroyPlayerEvent(playerEvent.uniqueId);
	}

	private bool CheckGameEnd(Entity lastDeadEntity) {
		//check affected player first
		foreach (PlayerUnit playerUnit in playerUnits[lastDeadEntity.playerId]) {
			if (playerUnit.currentHealth > 0) {
				return false;
			}
		}

		//check rest of players on affected team
		foreach (Player player in serverGameManager.GameState.teams[lastDeadEntity.teamId].players.Values.Where(x => !x.id.Equals(lastDeadEntity.playerId))) {
			foreach (PlayerUnit playerUnit in playerUnits[player.id]) {
				if (playerUnit.currentHealth > 0) {
					return false;
				}
			}
		}
		return true; //all playerunits for entire team are dead
	}
}
