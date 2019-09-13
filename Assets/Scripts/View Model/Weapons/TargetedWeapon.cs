using DarkRift;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetedWeapon : Weapon
{
	public Entity attackTarget { get; protected set; }

	public override bool Attack(object target) {
		Entity attackTarget = target as Entity;
		if (CanHitTarget(attackTarget)) {
			this.attackTarget = attackTarget;
			SetState(WeaponState.ATTACKING_COMMAND);
			return true;
		}

		return false;
	}

	protected override void HandleIdleState() {
		base.HandleIdleState();
		
		if (entity.CanAutoAttack()) {
			attackTarget = CheckForTargets(entity.vision.VisibleTargets);
			if (attackTarget != null) {
				entity.OnAutoTargetAcquired(attackTarget);
				SetState(WeaponState.ATTACKING_AUTO);
			}
		}
	}

	protected override void HandleCombatState() {
		if (attackTarget == null || !attackTarget.isVisible) {
			entity.OnTargetLost(attackTarget);
		} else {
			if (Vector3.Distance(attackTarget.transform.position, transform.position) > weaponData.range) {
				//out of range, pause attacking and reset weapon damage
				PauseAttacking();

				//try to move into range if allowed
				if (!entity.MoveToAttack(attackTarget)) {
					SetState(WeaponState.IDLE);
				}
			} else {
				//set initial attacking variables
				if (!isAttackingAndInRange) {
					StartAttacking();
				}

				if (!isDelayingAttack && attackCooldown <= 0f) {
					//do the attack after a delay (for visual effects)
					StartCoroutine(DelayedAttack(attackTarget));
					attackEffectTrigger = true;
				}
			}
		}
	}

	protected override void ClearTargets() {
		attackTarget = null;
	}

	protected override void StartAttacking() {
		base.StartAttacking();
		
		if (attackTarget != null) {
			entity.OnReachedAttackRange(attackTarget);
		}
	}

	protected virtual Entity CheckForTargets(List<Entity> visibleTargets) {
		return (from target in visibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, transform.position)
				where CanHitTarget(target) && distance <= weaponData.range
				orderby distance ascending
				select target).FirstOrDefault();
	}

	protected IEnumerator DelayedAttack(Entity attackTarget) {
		if (attackEffectTime > 0f) {
			isDelayingAttack = true;
			yield return new WaitForSeconds(attackEffectTime);
		}
		isDelayingAttack = false;

		if (attackTarget != null) {
			bool targetDied = DealDamage(attackTarget);
			DoSplashAttack(attackTarget);

			if (targetDied) {
				entity.OnTargetLost(attackTarget);
				SetState(WeaponState.IDLE);
			}
		}

		attackCooldown = weaponData.attackRate - attackEffectTime;
	}

	protected virtual void DoSplashAttack(Entity attackTarget) {
		if (weaponData.splashRadius > 0f) {
			Collider[] overlaps = Physics.OverlapSphere(attackTarget.transform.position, weaponData.splashRadius, LayerManager.Instance.damageableMask);
			IEnumerable<Entity> damagedTargets = (from target in overlaps.Select(x => x.GetComponentInParent<Entity>())
												  where target != attackTarget && CanSplashHitTarget(target, attackTarget)
												  select target);
			foreach (Entity entity in damagedTargets) {
				DealDamage(entity);
			}
		}
	}

	protected bool CanSplashHitTarget(Entity target, Entity originalTarget) {
		return target != null && target.teamId != entity.teamId && originalTarget.isInAir == target.isInAir;
	}

	protected override void HandleAttackEffectTrigger() {
		base.HandleAttackEffectTrigger();

		if (attackTarget != null) {
			if (projectilePrefab != null) {
				foreach (Transform spawnPoint in projectileSpawnPoints) {
					Tweener tweener = Instantiate<GameObject>(projectilePrefab.gameObject, spawnPoint.position, spawnPoint.rotation).GetComponent<Tweener>();
					if (tweener != null) {
						tweener.target = attackTarget.transform;
						tweener.time = attackEffectTime;
						tweener.enabled = true;
					}
				}
			}

			if (existingProjectiles != null) {
				foreach (GameObject projectileGO in existingProjectiles) {
					Tweener projectile = projectileGO.GetComponent<Tweener>();
					projectile.target = attackTarget.transform;
					projectile.time = attackEffectTime;
					projectile.enabled = true;
				}
			}
		}
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		string attackTargetId = e.Reader.ReadString();
		if (attackTarget == null || !attackTarget.uniqueId.Equals(attackTargetId)) {
			attackTarget = ClientEntityManager.Instance.GetEntity(attackTargetId);
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(attackTarget != null ? attackTarget.uniqueId : "");
	}
}
