using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityHUD : MonoBehaviour {

	public PercentageBar healthBar;
	public bool hideBarsWhenFull = false;
	public float scalingRatio = 50f;
	public float yPosRatio = 2000f;

	public Entity Entity { get; set; }
	private RectTransform rectTransform;

	private void Awake() {
		rectTransform = GetComponent<RectTransform>();
	}

	private void Start() {
		if (GetComponent<VisibilityToggle>() != null) {
			GetComponent<VisibilityToggle>().EntityController = Entity.EntityController;
		}
	}

	private void OnEnable() {
		UpdateRendering(); //call this immediately otherwise the hud can appear incorrectly for one frame
	}

	private void Update() {
		UpdateRendering();
		UpdateHealthBar();
	}

	private void UpdateRendering() {
		if (Entity != null) {
			float distance = Vector3.Distance(Camera.main.transform.position, Entity.transform.position);

			//add to y pos based on distance
			Vector3 newPos = Camera.main.WorldToScreenPoint(Entity.transform.position);
			newPos.y += (yPosRatio / distance);
			rectTransform.position = newPos;

			//scale based on distance
			float scalingFactor = (scalingRatio / distance);
			rectTransform.localScale = new Vector3(scalingFactor, scalingFactor, scalingFactor);
		}
	}

	private void UpdateHealthBar() {
		healthBar.SetDisplayAmounts(Entity.currentHealth, Entity.baseHealth);
		healthBar.gameObject.SetActive(!hideBarsWhenFull || Entity.currentHealth < Entity.baseHealth);
	}
}
