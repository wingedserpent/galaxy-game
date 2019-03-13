using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

public class ClientEntityManager : Singleton<ClientEntityManager> {
	
	private static readonly string CLIENT_SELF_TAG = "ClientSelf";
	private static readonly string CLIENT_TEAM_OR_AUTHORITY_TAG = "ClientTeamOrAuthority";
	
	public EntityDatabase entityDatabase;

	public Dictionary<string, Entity> MySquad { get; private set; }

	private Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
	private Dictionary<string, PlayerEvent> playerEvents = new Dictionary<string, PlayerEvent>();

	private ClientGameManager clientGameManager;

	protected override void Awake() {
		base.Awake();

		MySquad = new Dictionary<string, Entity>();
	}

	private void Start() {
		clientGameManager = ClientGameManager.Instance;
	}

	public Entity GetEntity(string entityId) {
		if (entities.ContainsKey(entityId)) {
			return entities[entityId];
		}
		return null;
	}

	public PlayerEvent GetPlayerEvent(string playerEventId) {
		if (playerEvents.ContainsKey(playerEventId)) {
			return playerEvents[playerEventId];
		}
		return null;
	}

	public Entity CreateEntity(string entityTypeId) {
		return entityDatabase.GetEntityInstance(entityTypeId);
	}

	public PlayerEvent CreatePlayerEvent(string playerEventTypeId) {
		PlayerEvent playerEventRef = entityDatabase.GetPlayerEventReference(playerEventTypeId);
		return Instantiate(playerEventRef.gameObject).GetComponent<PlayerEvent>();
	}

	public void RegisterEntity(Entity entity) {
		if (!entities.ContainsKey(entity.ID)) {
			ScrubOwnedObject(entity);
			ColorizeOwnedObject(entity);
			entities.Add(entity.ID, entity);

			if (entity.PlayerId == clientGameManager.MyPlayer.ID) {
				MySquad.Add(entity.ID, entity);
			}
		} else {
			Debug.LogWarning("An entity with ID: " + entity.ID + " already exists! Destroying new entity.");
			Destroy(entity.gameObject);
		}
	}

	public void RegisterPlayerEvent(PlayerEvent playerEvent) {
		if (!playerEvents.ContainsKey(playerEvent.ID)) {
			ScrubOwnedObject(playerEvent);
			ColorizeOwnedObject(playerEvent);
			playerEvents.Add(playerEvent.ID, playerEvent);
		} else {
			Debug.LogWarning("A player event with ID: " + playerEvent.ID + " already exists! Destroying new player event.");
			Destroy(playerEvent.gameObject);
		}
	}

	protected void ColorizeOwnedObject(OwnedObject ownedObject) {
		Color teamColor = clientGameManager.GameState.Teams[ownedObject.TeamId].Color;
		foreach (Colorable colorable in ownedObject.GetComponentsInChildren<Colorable>()) {
			colorable.SetColor(teamColor);
		}
	}

	public void RemoveEntity(string entityId) {
		if (entities.ContainsKey(entityId)) {
			//Destroy(entities[entityId].gameObject, t); entities now destroy themselves
			entities.Remove(entityId);

			if (MySquad.ContainsKey(entityId)) {
				MySquad.Remove(entityId);

				if (MySquad.Count == 0) {
					clientGameManager.OnSquadGone();
				}
			}
		}
	}

	private void ScrubOwnedObject(OwnedObject ownedObject) {
		if (ownedObject is Entity) {
			Entity entity = ownedObject as Entity;
			if (ownedObject.TeamId == clientGameManager.MyPlayer.TeamId) {
				//disable visibility updates for teammates since they will always be visible
				entity.EntityController.UpdateVisibility = false;
			} else {
				//immediately send visibility=false update to ensure everything starts hidden
				entity.EntityController.VisibilityTargetDispatch(null);
			}
		}
		
		//remove any children that don't need to exist on a client
		foreach (Transform child in ownedObject.transform) {
			if (child.CompareTag(CLIENT_SELF_TAG) && ownedObject.PlayerId != clientGameManager.MyPlayer.ID) {
				Destroy(child.gameObject);
			} else if (child.CompareTag(CLIENT_TEAM_OR_AUTHORITY_TAG) && ownedObject.TeamId != clientGameManager.MyPlayer.TeamId) {
				Destroy(child.gameObject);
			}
		}

		//disable nav mesh agents since they can cause "jiggling" on the client screen
		if (ownedObject.GetComponent<NavMeshAgent>() != null) {
			ownedObject.GetComponent<NavMeshAgent>().enabled = false;
		}
	}

	public void HandleEntityDeath(string entityId) {
		if (entities.ContainsKey(entityId)) {
			Entity entity = entities[entityId];
			StartCoroutine(entity.EntityController.Die(true, 1f));
			RemoveEntity(entity.ID);
		}
	}

	public void HandleEntityDespawn(string entityId) {
		if (entities.ContainsKey(entityId)) {
			Entity entity = entities[entityId];
			entity.EntityController.CleanUpForDespawn();
			RemoveEntity(entity.ID);
			Destroy(entity.gameObject);
		}
	}

	public void HandlePlayerEventEnd(string playerEventId) {
		if (playerEvents.ContainsKey(playerEventId)) {
			playerEvents[playerEventId].End();
			playerEvents.Remove(playerEventId);
		}
	}

	public Entity GetEntityReference(string typeId) {
		return entityDatabase.GetEntityReference(typeId);
	}

	public PlayerEvent GetPlayerEventReference(string typeId) {
		return entityDatabase.GetPlayerEventReference(typeId);
	}
}
