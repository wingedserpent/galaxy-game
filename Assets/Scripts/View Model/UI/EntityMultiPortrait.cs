﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EntityMultiPortrait : MonoBehaviour, IPointerClickHandler {

	public Text nameText;
	public PercentageBar healthBar;
	public PercentageBar shieldBar;

	public Entity Entity { get; set; }

	private bool shieldEnabled = true;

	private void Start() {
		nameText.text = Entity.typeId;

		if (Entity.maxShield <= 0) {
			shieldEnabled = false;
			shieldBar.gameObject.SetActive(false);
		}
	}

	private void Update() {
		UpdateHealthBar();
		if (shieldEnabled) {
			UpdateShieldBar();
		}
	}

	private void UpdateHealthBar() {
		healthBar.SetDisplayAmounts(Entity.currentHealth, Entity.maxHealth);
	}

	private void UpdateShieldBar() {
		shieldBar.SetDisplayAmounts(Entity.currentShield, Entity.maxShield);
	}

	public void OnPointerClick(PointerEventData eventData) {
		UIManager.Instance.OnEntityPortraitClick(Entity);
	}
}
