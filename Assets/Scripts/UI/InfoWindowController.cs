﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InfoWindowController : MonoBehaviour {

	public Text nameText;
	public Text healthText;
	public Text shieldText;
	public RectTransform entityMultiPortraitContainer;
	public EntityMultiPortrait entityMultiPortraitPrefab;

	private Entity selectedSingleEntity;
	private Dictionary<string, EntityMultiPortrait> entityMultiPortraits = new Dictionary<string, EntityMultiPortrait>();

	private void Start() {
		nameText.text = "";
		healthText.text = "";
		shieldText.text = "";
	}

	private void Update() {
		if (selectedSingleEntity != null) {
			PopulateSingleEntityFields();
		} else if (entityMultiPortraits.Count > 0) {
			PopulateMultipleEntityFields();
		}
	}

	public void OpenWindow() {
		gameObject.SetActive(true);
	}

	public void UpdateSelectedEntities(List<Entity> newEntities) {
		if (newEntities.Count == 1 && newEntities[0] != null && newEntities[0] != selectedSingleEntity) {
			ClearMultipleEntityFields();
			selectedSingleEntity = newEntities[0];
		} else if (newEntities != null) {
			ClearSingleEntityFields();

			List<string> entitiesToRemove = entityMultiPortraits.Where(x => !newEntities.Contains(x.Value.Entity)).Select(x => x.Key).ToList();
			foreach (string entityId in entitiesToRemove) {
				Destroy(entityMultiPortraits[entityId].gameObject);
				entityMultiPortraits.Remove(entityId);
			}

			foreach (Entity entity in newEntities) {
				if (!entityMultiPortraits.ContainsKey(entity.ID)) {
					EntityMultiPortrait entityMultiPortrait = Instantiate<GameObject>(entityMultiPortraitPrefab.gameObject, entityMultiPortraitContainer).GetComponent<EntityMultiPortrait>();
					entityMultiPortrait.Entity = entity;
					entityMultiPortraits.Add(entity.ID, entityMultiPortrait);
				}
			}
		}
	}

	protected void PopulateSingleEntityFields() {
		nameText.text = selectedSingleEntity.typeId;
		healthText.text = selectedSingleEntity.CurrentHealth + " / " + selectedSingleEntity.MaxHealth;
		if (selectedSingleEntity.MaxShield > 0) {
			shieldText.text = selectedSingleEntity.CurrentShield + " / " + selectedSingleEntity.MaxShield;
		}
	}

	protected void ClearSingleEntityFields() {
		selectedSingleEntity = null;
		nameText.text = null;
		healthText.text = null;
		shieldText.text = null;
	}

	protected void PopulateMultipleEntityFields() {
		//do stuff here
	}

	protected void ClearMultipleEntityFields() {
		foreach (EntityMultiPortrait entityMultiPortrait in entityMultiPortraits.Values) {
			Destroy(entityMultiPortrait.gameObject);
		}
		entityMultiPortraits.Clear();
	}
}