using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousWeapon : TargetedWeapon
{
	public int CurrentDamage { get; private set; }

	private float damageIncreaseTimer = 0f;

	protected override void Update() {
		if (NetworkStatus.Instance.IsServer) {
			//increment damage timer
			if (damageIncreaseTimer > 0f) {
				damageIncreaseTimer -= Time.deltaTime;
				if (damageIncreaseTimer <= 0f) {
					SetCurrentDamage(CurrentDamage + 1);
				}
			}
		}

		base.Update();
	}

	protected void SetCurrentDamage(int damage) {
		CurrentDamage = damage;
		if (CurrentDamage < weaponData.maxDamage) {
			damageIncreaseTimer = (weaponData.damageIncreaseTime / (weaponData.maxDamage - weaponData.damage));
		} else {
			damageIncreaseTimer = 0f;
		}
	}

	protected override void PauseAttacking() {
		base.PauseAttacking();

		SetCurrentDamage(weaponData.damage);
	}

	protected override bool DealDamage(Entity target) {
		return target.TakeDamage(CurrentDamage);
	}
	
	protected override void HandleAttackStartTrigger() {
		base.HandleAttackStartTrigger();

		if (attackTarget != null) {
			foreach (Transform spawnPoint in projectileSpawnPoints) {
				WeaponBeam beam = Instantiate<GameObject>(projectilePrefab.gameObject, spawnPoint).GetComponent<WeaponBeam>();
				beam.transform.localPosition = Vector3.zero;
				if (beam != null) {
					beam.start = transform;
					beam.target = attackTarget != null ? attackTarget.transform : null;
					beam.totalEffectTime = weaponData.damageIncreaseTime;
				}
			}
		}
	}

	protected override void HandleAttackStopTrigger() {
		base.HandleAttackStopTrigger();

		foreach (Transform spawnPoint in projectileSpawnPoints) {
			foreach (WeaponBeam beam in spawnPoint.GetComponentsInChildren<WeaponBeam>()) {
				Destroy(beam.gameObject);
			}
		}
	}
}
