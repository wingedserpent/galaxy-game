using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class EntityDatabase : ScriptableObject {
	
	public List<Unit> units;
	public List<Structure> structures;
	public List<PlayerEvent> playerEvents;

	private Dictionary<string, Entity> entityMap;
	private Dictionary<string, PlayerEvent> playerEventMap;

	public Entity GetEntityReference(string typeId) {
		if (entityMap == null) {
			entityMap = new Dictionary<string, Entity>();
			foreach (Entity entity in units) {
				if (entityMap.ContainsKey(entity.typeId)) {
					throw new System.Exception("Multiple entites have the same ID: " + entity.typeId);
				}
				entityMap.Add(entity.typeId, entity);
			}
			foreach (Entity entity in structures) {
				if (entityMap.ContainsKey(entity.typeId)) {
					throw new System.Exception("Multiple entites have the same ID: " + entity.typeId);
				}
				entityMap.Add(entity.typeId, entity);
			}
		}
		if (entityMap.ContainsKey(typeId)) {
			return entityMap[typeId];
		}
		return null;
	}

	public Entity GetEntityInstance(string typeId, Transform parent = null) {
		return Instantiate<GameObject>(GetEntityReference(typeId).gameObject, parent).GetComponent<Entity>();
	}

	public Entity GetEntityInstance(string typeId, Vector3 position, Quaternion rotation) {
		return Instantiate<GameObject>(GetEntityReference(typeId).gameObject, position, rotation).GetComponent<Entity>();
	}

	public Entity GetEntityInstance(string typeId, Vector3 position, Quaternion rotation, Transform parent) {
		return Instantiate<GameObject>(GetEntityReference(typeId).gameObject, position, rotation, parent).GetComponent<Entity>();
	}

	public PlayerEvent GetPlayerEventReference(string typeId) {
		if (playerEventMap == null) {
			playerEventMap = new Dictionary<string, PlayerEvent>();
			foreach (PlayerEvent playerEvent in playerEvents) {
				if (playerEventMap.ContainsKey(playerEvent.typeId)) {
					throw new System.Exception("Multiple player events have the same ID: " + playerEvent.typeId);
				}
				playerEventMap.Add(playerEvent.typeId, playerEvent);
			}
		}
		if (playerEventMap.ContainsKey(typeId)) {
			return playerEventMap[typeId];
		}
		return null;
	}
}
