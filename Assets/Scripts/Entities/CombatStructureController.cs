using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public class CombatStructureController : StructureController {

	public GameObject projectilePrefab;
	public Transform projectileSpawnPoint;
	public AudioClip attackSound;

	public Entity AttackTarget { get; set; }
	public bool AttackTrigger { get; set; }

	protected CombatStructure combatStructure;
	protected float attackCooldown = 0f;

	protected override void Awake() {
		base.Awake();

		combatStructure = GetComponent<CombatStructure>();

		if (projectileSpawnPoint == null) {
			projectileSpawnPoint = transform;
		}
		AttackTrigger = false;
	}

	protected override void Update() {
		base.Update();

		if (NetworkStatus.Instance.IsServer) {
			if (attackCooldown > 0f) {
				attackCooldown -= Time.deltaTime;
			}
		}
	}

	protected override void HandleAIStates() {
		if (constructionTimer > 0f) {
			AttackTarget = null;
			return;
		}

		base.HandleAIStates();

		if (State == AIState.ATTACKING) {
			HandleCombatState();
			return;
		}
	}

	protected override void HandleIdleState() {
		AttackTarget = CheckForTargets();
		if (AttackTarget != null) {
			State = AIState.ATTACKING;
		}
	}

	protected Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where target.TeamId != entity.TeamId && distance <= combatStructure.attackRange
				orderby distance ascending
				select target).FirstOrDefault();
	}

	protected virtual void HandleCombatState() {
		if (AttackTarget == null || !AttackTarget.EntityController.IsVisible) {
			//target disappeared or died
			Stop();
		} else {
			//look at target's x/z position
			Vector3 lookTarget = new Vector3(AttackTarget.transform.position.x,
									   transform.position.y,
									   AttackTarget.transform.position.z);
			transform.LookAt(lookTarget);

			if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > combatStructure.attackRange) {
				//out of range, give up
				AttackTarget = null;
				State = AIState.IDLE;
			} else {
				if (attackCooldown <= 0f) {
					//do the attack
					bool targetDied = AttackTarget.EntityController.TakeDamage(combatStructure.attackDamage);
					if (targetDied) {
						AttackTarget = null;
						State = AIState.IDLE;
					}
					attackCooldown = combatStructure.attackSpeed;
					AttackTrigger = true;
				}
			}
		}
	}

	public override void Attack(Entity attackTarget) {
		if (attackTarget != null) {
			AttackTarget = attackTarget;
			State = AIState.ATTACKING;
		}
	}

	protected override void UpdateEffects() {
		base.UpdateEffects();

		if (AttackTrigger) {
			if (animator != null) {
				animator.SetTrigger("attack");
			}
			if (audioSource != null) {
				audioSource.PlayOneShot(attackSound);
			}
			if (projectilePrefab != null) {
				Projectile projectile = Instantiate<GameObject>(projectilePrefab.gameObject, projectileSpawnPoint.position, projectileSpawnPoint.rotation).GetComponent<Projectile>();
				projectile.target = AttackTarget.transform;
			}
			AttackTrigger = false;
		}
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);
		
		AttackTrigger = e.Reader.ReadBoolean();
		string attackTargetId = e.Reader.ReadString();
		if (attackTargetId != "") {
			AttackTarget = clientEntityManager.GetEntity(attackTargetId);
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);
		
		e.Writer.Write(AttackTrigger);
		e.Writer.Write(AttackTarget != null ? AttackTarget.ID : "");

		AttackTrigger = false;
	}
}
