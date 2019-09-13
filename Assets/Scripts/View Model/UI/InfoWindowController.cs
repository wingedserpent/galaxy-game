using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InfoWindowController : MonoBehaviour {

	public Text nameText;
	public Text healthText;
	public Text shieldText;
	public Text commandMenuText;
	public RectTransform entityMultiPortraitContainer;
	public EntityMultiPortrait entityMultiPortraitPrefab;
	public List<InputCommand> noSelectionCommands;

	private Entity selectedSingleEntity;
	private Dictionary<string, EntityMultiPortrait> entityMultiPortraits = new Dictionary<string, EntityMultiPortrait>();

	private void Start() {
		nameText.text = "";
		healthText.text = "";
		shieldText.text = "";
		CloseCommandMenu();
	}

	private void Update() {
		if (selectedSingleEntity != null) {
			PopulateSingleEntityFields();
		} else {
			ClearSingleEntityFields();
		}

		if (entityMultiPortraits.Count > 0) {
			//remove any missing (i.e. dead) entities
			List<string> missingEntities = entityMultiPortraits.Where(x => x.Value.Entity == null).Select(x => x.Key).ToList();
			foreach (string missingEntity in missingEntities) {
				Destroy(entityMultiPortraits[missingEntity].gameObject);
				entityMultiPortraits.Remove(missingEntity);
			}

			if (entityMultiPortraits.Count > 0) {
				PopulateMultipleEntityFields();
			} else {
				ClearMultipleEntityFields();
			}
		}
	}

	public void OpenWindow() {
		gameObject.SetActive(true);
	}

	public void OpenCommandMenu(List<InputCommand> commands) {
		commandMenuText.text = "";
		foreach (InputCommand command in commands) {
			commandMenuText.text += command.key + " - " + command.command + (command.cost > 0 ? " (" + command.cost + ")" : "") + "\n";
		}
	}

	public void CloseCommandMenu() {
		commandMenuText.text = "";
		foreach (InputCommand command in noSelectionCommands) {
			commandMenuText.text += command.key + " - " + command.command + "\n";
		}
	}

	public void UpdateSelectedEntities(List<Entity> newEntities) {
		if (newEntities.Count == 1) {
			if (newEntities[0] != null && newEntities[0] != selectedSingleEntity) {
				ClearMultipleEntityFields();
				selectedSingleEntity = newEntities[0];
				PopulateSingleEntityFields();
			}
		} else if (newEntities.Count > 1) {
			ClearSingleEntityFields();
			commandMenuText.gameObject.SetActive(false);

			List<string> entitiesToRemove = entityMultiPortraits.Where(x => !newEntities.Contains(x.Value.Entity)).Select(x => x.Key).ToList();
			foreach (string entityId in entitiesToRemove) {
				Destroy(entityMultiPortraits[entityId].gameObject);
				entityMultiPortraits.Remove(entityId);
			}

			foreach (Entity entity in newEntities) {
				if (!entityMultiPortraits.ContainsKey(entity.uniqueId)) {
					EntityMultiPortrait entityMultiPortrait = Instantiate<GameObject>(entityMultiPortraitPrefab.gameObject, entityMultiPortraitContainer).GetComponent<EntityMultiPortrait>();
					entityMultiPortrait.Entity = entity;
					entityMultiPortraits.Add(entity.uniqueId, entityMultiPortrait);
				}
			}
			PopulateMultipleEntityFields();
		} else {
			ClearSingleEntityFields();
			ClearMultipleEntityFields();
		}
	}

	protected void PopulateSingleEntityFields() {
		nameText.text = selectedSingleEntity.typeId;
		healthText.text = selectedSingleEntity.currentHealth + " / " + selectedSingleEntity.maxHealth;
		if (selectedSingleEntity.maxShield > 0) {
			shieldText.text = selectedSingleEntity.currentShield + " / " + selectedSingleEntity.maxShield;
		}
		OpenCommandMenu(selectedSingleEntity.availableCommands);
	}

	protected void ClearSingleEntityFields() {
		if (nameText.text != "") {
			CloseCommandMenu();
		}
		selectedSingleEntity = null;
		nameText.text = "";
		healthText.text = "";
		shieldText.text = "";
	}

	protected void PopulateMultipleEntityFields() {
		//do stuff here
	}

	protected void ClearMultipleEntityFields() {
		commandMenuText.gameObject.SetActive(true);

		foreach (EntityMultiPortrait entityMultiPortrait in entityMultiPortraits.Values) {
			Destroy(entityMultiPortrait.gameObject);
		}
		entityMultiPortraits.Clear();
	}
}
