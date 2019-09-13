using DarkRift;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocationWeapon : Weapon
{
	public GameObject targetingPrefab;
	public GameObject targetedAreaPrefab;

	public Vector3? attackLocation { get; protected set; }

	protected GameObject targetedAreaMarker;

	public override bool Attack(object target) {
		if (target != null) {
			if (target is Vector3) {
				attackLocation = Vector3.MoveTowards(transform.position, (Vector3)target, weaponData.range);
			} else if (target is Entity) {
				attackLocation = Vector3.MoveTowards(transform.position, ((Entity)target).transform.position, weaponData.range);
			}
			SetState(WeaponState.ATTACKING_COMMAND);
			return true;
		}

		return false;
	}

	protected override void HandleIdleState() {
		base.HandleIdleState();

		//right now, location weapons cannot auto attack
	}

	protected override void HandleCombatState() {
		if (!attackLocation.HasValue) {
			Stop();
		} else if (!isDelayingAttack && attackCooldown <= 0f && GetTargetsAtAttackLocation(attackLocation.Value).Count > 0) {
			//set initial attacking variables
			if (!isAttackingAndInRange) {
				StartAttacking();
			}
			
			//do the attack after a delay (for visual effects)
			StartCoroutine(DelayedLocationAttack(attackLocation.Value));
			attackEffectTrigger = true;
		}
	}

	protected virtual List<Entity> GetTargetsAtAttackLocation(Vector3 attackLocation) {
		Collider[] overlaps = Physics.OverlapSphere(attackLocation, weaponData.splashRadius, LayerManager.Instance.damageableMask);
		return (from target in overlaps.Select(x => x.GetComponentInParent<Entity>())
				where CanHitTarget(target)
				select target).ToList();
	}

	protected virtual IEnumerator DelayedLocationAttack(Vector3 attackLocation) {
		if (attackEffectTime > 0f) {
			isDelayingAttack = true;
			yield return new WaitForSeconds(attackEffectTime);
		}
		isDelayingAttack = false;

		DoLocationAttack(attackLocation);

		attackCooldown = weaponData.attackRate - attackEffectTime;
	}

	protected virtual void DoLocationAttack(Vector3 attackLocation) {
		foreach (Entity entity in GetTargetsAtAttackLocation(attackLocation)) {
			DealDamage(entity);
		}
	}

	protected override void ClearTargets() {
		attackLocation = null;
	}

	protected void CreateTargetedAreaMarker() {
		if (targetedAreaMarker == null && attackLocation.HasValue) {
			targetedAreaMarker = Instantiate<GameObject>(targetedAreaPrefab, attackLocation.Value, Quaternion.identity);
		}
	}

	protected void DestroyTargetedAreaMarker() {
		if (targetedAreaMarker != null) {
			Destroy(targetedAreaMarker);
			targetedAreaMarker = null;
		}
	}

	protected override void HandleAttackEffectTrigger() {
		base.HandleAttackEffectTrigger();

		if (attackLocation.HasValue) {
			if (projectilePrefab != null) {
				foreach (Transform spawnPoint in projectileSpawnPoints) {
					Tweener tweener = Instantiate<GameObject>(projectilePrefab.gameObject, spawnPoint.position, spawnPoint.rotation).GetComponent<Tweener>();
					if (tweener != null) {
						tweener.TargetPos = attackLocation.Value;
						tweener.time = attackEffectTime;
						tweener.enabled = true;
					}
				}
			}

			if (existingProjectiles != null) {
				foreach (GameObject projectileGO in existingProjectiles) {
					Tweener projectile = projectileGO.GetComponent<Tweener>();
					projectile.TargetPos = attackLocation.Value;
					projectile.time = attackEffectTime;
					projectile.enabled = true;
				}
			}
		}
	}

	protected override void OnEntitySelected() {
		CreateTargetedAreaMarker();
	}

	protected override void OnEntityDeselected() {
		DestroyTargetedAreaMarker();
	}

	protected virtual void OnDestroy() {
		DestroyTargetedAreaMarker();
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);
		
		bool hasAttackLocation = e.Reader.ReadBoolean();
		if (hasAttackLocation) {
			Vector3? oldAttackLocation = attackLocation;

			attackLocation = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle()); ;
			//TODO refactor so reactions to variable changes can be handled elsewhere (like property setters but only on client, for example)
			if (oldAttackLocation.HasValue && oldAttackLocation.Value != attackLocation) {
				DestroyTargetedAreaMarker();
			}
			CreateTargetedAreaMarker();
		} else {
			DestroyTargetedAreaMarker();
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(attackLocation.HasValue);
		if (attackLocation.HasValue) {
			e.Writer.Write(attackLocation.Value.x); e.Writer.Write(attackLocation.Value.y); e.Writer.Write(attackLocation.Value.z);
		}
	}
}
