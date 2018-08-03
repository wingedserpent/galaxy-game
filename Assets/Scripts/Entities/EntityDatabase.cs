using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class EntityDatabase : ScriptableObject {
	
	public List<Unit> units;
	public List<Structure> structures;

	private Dictionary<string, Entity> entityMap;

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
		return entityMap[typeId];
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

	public GameObject GetConstruction(string structureTypeId, Transform parent = null) {
		Structure structure = GetEntityReference(structureTypeId) as Structure;
		return Instantiate<GameObject>(structure.constructionPrefab, parent);
	}
}
