using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public class WarriorController : UnitController {

	public KeyCode shieldKey;
	public GameObject shieldGameObject;
	public float shieldActiveTime;
	public float shieldCooldown;

	protected bool isShieldActive = false;
	protected float shieldActiveTimer = 0f;
	protected float shieldCooldownTimer = 0f;
	protected MeshRenderer shieldRenderer;

	protected override void Awake() {
		base.Awake();

		shieldRenderer = shieldGameObject.GetComponent<MeshRenderer>();
	}

	protected override void Update() {
		if (isShieldActive) {
			shieldActiveTimer -= Time.deltaTime;
			
			if (shieldActiveTimer <= 0f) {
				DeactivateShield();
			} else if (shieldActiveTimer <= 4f) {
				if (!LeanTween.isTweening(shieldGameObject)) {
					LeanTween.value(shieldGameObject, ShieldBlinkEffectUpdate, 1f, 0f, shieldActiveTimer).setEaseOutBounce();
				}
			}
		} else if (shieldCooldownTimer >= 0f) {
			shieldCooldownTimer -= Time.deltaTime;
		}

		base.Update();
	}

	public override CommandType GetCommandTypeFromInput() {
		if (Input.GetKeyDown(shieldKey)) {
			return CommandType.ABILITY_SHIELD;
		}
		return CommandType.NONE;
	}

	public override void ExecuteCommand(CommandType commandType) {
		if (commandType == CommandType.ABILITY_SHIELD) {
			ActivateShield();
		}
	}

	public void ActivateShield() {
		if (!isShieldActive && shieldCooldownTimer <= 0f) {
			shieldRenderer.enabled = true;
			shieldGameObject.SetActive(true);
			shieldActiveTimer = shieldActiveTime;
			isShieldActive = true;
		}
	}

	protected void DeactivateShield() {
		shieldRenderer.enabled = true;
		shieldGameObject.SetActive(false);
		shieldCooldownTimer = shieldCooldown;
		isShieldActive = false;
	}

	public override bool TakeDamage(int amount) {
		if (!isShieldActive) {
			return base.TakeDamage(amount);
		}
		return false;
	}

	protected void ShieldBlinkEffectUpdate(float value) {
		shieldRenderer.enabled = (value >= 0.03f);
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		bool shieldActive = e.Reader.ReadBoolean();
		if (shieldActive != isShieldActive) {
			if (shieldActive) {
				ActivateShield();
			} else {
				DeactivateShield();
			}
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(isShieldActive);
	}
}
