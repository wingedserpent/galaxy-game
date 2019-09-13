using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceFieldAbility : Ability
{
	public float activeTime;
	public float cooldown;
	public GameObject forceFieldGameObject;

	public override string abilityTypeId { get { return "shield"; } }

	protected float shieldActiveTimer = 0f;
	protected float shieldCooldownTimer = 0f;
	
	protected MeshRenderer shieldRenderer;

	protected override void Awake() {
		base.Awake();

		shieldRenderer = forceFieldGameObject.GetComponent<MeshRenderer>();
	}

	protected override bool OnActivate() {
		if (shieldCooldownTimer <= 0f) {
			shieldRenderer.enabled = true;
			forceFieldGameObject.SetActive(true);
			shieldActiveTimer = activeTime;
			entity.OnModifyDamageTaken += OnModifyDamageTaken;
			return true;
		}
		return false;
	}

	protected override bool OnDeactivate() {
		shieldRenderer.enabled = true;
		forceFieldGameObject.SetActive(false);
		shieldCooldownTimer = cooldown;
		entity.OnModifyDamageTaken -= OnModifyDamageTaken;
		return true;
	}

	protected void Update() {
		if (isActive) {
			shieldActiveTimer -= Time.deltaTime;

			if (shieldActiveTimer <= 0f) {
				Deactivate();
			} else if (shieldActiveTimer <= 4f) {
				if (!LeanTween.isTweening(forceFieldGameObject)) {
					LeanTween.value(forceFieldGameObject, ShieldBlinkEffectUpdate, 1f, 0f, shieldActiveTimer).setEaseOutBounce();
				}
			}
		} else if (shieldCooldownTimer >= 0f) {
			shieldCooldownTimer -= Time.deltaTime;
		}
	}

	protected void ShieldBlinkEffectUpdate(float value) {
		shieldRenderer.enabled = (value >= 0.03f);
	}

	protected void OnModifyDamageTaken(int originalAmount, ref int modifiedAmount) {
		modifiedAmount = 0;
	}
}
