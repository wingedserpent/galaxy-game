﻿using System.Collections;
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

	public Entity CreateEntity(string entityTypeId) {
		return entityDatabase.GetEntityInstance(entityTypeId);
	}

	public void RegisterEntity(Entity entity) {
		if (!entities.ContainsKey(entity.ID)) {
			ScrubEntity(entity);
			ColorizeEntity(entity);
			entities.Add(entity.ID, entity);

			if (entity.PlayerId == clientGameManager.MyPlayer.ID) {
				MySquad.Add(entity.ID, entity);
			}
		} else {
			Debug.LogWarning("An entity with ID: " + entity.ID + " already exists! Destroying new entity.");
			Destroy(entity.gameObject);
		}
	}

	protected void ColorizeEntity(Entity entity) {
		Color teamColor = clientGameManager.GameState.Teams[entity.TeamId].Color;
		foreach (Colorable colorable in entity.GetComponentsInChildren<Colorable>()) {
			colorable.SetColor(teamColor);
		}
	}

	public void DestroyEntity(string entityId, float t = 0f) {
		if (entities.ContainsKey(entityId)) {
			Destroy(entities[entityId].gameObject, t);
			entities.Remove(entityId);

			if (MySquad.ContainsKey(entityId)) {
				MySquad.Remove(entityId);

				if (MySquad.Count == 0) {
					clientGameManager.OnSquadDead();
				}
			}
		}
	}

	private void ScrubEntity(Entity entity) {
		if (entity.TeamId == clientGameManager.MyPlayer.TeamId) {
			//disable visibility updates for teammates since they will always be visible
			entity.EntityController.UpdateVisibility = false;
		} else {
			//immediately send visibility=false update to ensure everything starts hidden
			entity.EntityController.VisibilityTargetDispatch(null);
		}
		
		//remove any children that don't need to exist on a client
		foreach (Transform child in entity.transform) {
			if (child.CompareTag(CLIENT_SELF_TAG) && entity.PlayerId != clientGameManager.MyPlayer.ID) {
				Destroy(child.gameObject);
			} else if (child.CompareTag(CLIENT_TEAM_OR_AUTHORITY_TAG) && entity.TeamId != clientGameManager.MyPlayer.TeamId) {
				Destroy(child.gameObject);
			}
		}

		//disable nav mesh agents since they can cause "jiggling" on the client screen
		if (entity.GetComponent<NavMeshAgent>() != null) {
			entity.GetComponent<NavMeshAgent>().enabled = false;
		}
	}

	public void HandleEntityDeath(string entityId) {
		if (entities.ContainsKey(entityId)) {
			Entity entity = entities[entityId];
			entity.EntityController.Die();

			DestroyEntity(entity.ID, 1f);
		}
	}

	public GameObject SpawnConstruction(string entityTypeId) {
		return entityDatabase.GetConstruction(entityTypeId);
	}

	public Entity GetEntityReference(string typeId) {
		return entityDatabase.GetEntityReference(typeId);
	}
}
