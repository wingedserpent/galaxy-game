using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public class UnitController : EntityController {

	public GameObject projectilePrefab;
	public Transform projectileSpawnPoint;
	public AudioClip attackSound;

	public Vector3 CurrentVelocity { get; set; }
	public Entity AttackTarget { get; set; }
	public bool AttackTrigger { get; set; }

	protected float attackCooldown = 0f;
	protected bool isRespondingToAttackCommand = false;

	protected Unit unit;
	protected NavMeshAgent agent;
	protected NavMeshObstacle obstacle;

	protected override void Awake() {
		base.Awake();

		unit = GetComponent<Unit>();
		agent = GetComponent<NavMeshAgent>();
		obstacle = GetComponent<NavMeshObstacle>();

		if (projectileSpawnPoint == null) {
			projectileSpawnPoint = transform;
		}
		AttackTrigger = false;
	}

	protected override void Update() {
		if (NetworkStatus.Instance.IsServer) {
			if (attackCooldown > 0f) {
				attackCooldown -= Time.deltaTime;
			}
			CurrentVelocity = transform.InverseTransformVector(agent.velocity);
		}

		base.Update();
	}

	protected override void HandleAIStates() {
		base.HandleAIStates();

		if (State == AIState.MOVING) {
			HandleMovingState();
			return;
		} else if (State == AIState.ATTACKING) {
			HandleCombatState();
			return;
		}
	}

	protected override void HandleIdleState() {
		isRespondingToAttackCommand = false;

		AttackTarget = CheckForTargets();
		if (AttackTarget != null) {
			State = AIState.ATTACKING;
		}
	}

	protected Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where target.TeamId != entity.TeamId && distance <= unit.attackRange
				orderby distance ascending
				select target).FirstOrDefault();
	}

	protected void HandleMovingState() {
		isRespondingToAttackCommand = false;

		// Check if we've reached the destination
		if (agent.enabled && !agent.pathPending) {
			if (agent.remainingDistance <= agent.stoppingDistance) {
				if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) {
					StopMovement();
					State = AIState.IDLE;
				}
			}
		}
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

			if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > unit.attackRange) {
				//out of range, move into range if responding to an explicit attack command
				if (isRespondingToAttackCommand) {
					SetMoveDestination(FindTrueMoveDestination(AttackTarget.transform.position));
				}
			} else {
				//in range, stop moving
				StopMovement();

				if (attackCooldown <= 0f) {
					//do the attack
					bool targetDied = AttackTarget.EntityController.TakeDamage(unit.attackDamage);
					if (targetDied) {
						AttackTarget = null;
						State = AIState.IDLE;
					}
					attackCooldown = unit.attackSpeed;
					AttackTrigger = true;
				}
			}
		}
	}

	public override void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		AttackTarget = null;
		if (SetMoveDestination(FindTrueMoveDestination(target, groupMovementCenter))) {
			State = AIState.MOVING;
		}
	}

	public override void Stop() {
		base.Stop();

		AttackTarget = null;
		StopMovement();
	}

	public override void Attack(Entity attackTarget) {
		if (attackTarget != null) {
			isRespondingToAttackCommand = true;
			StopMovement();
			AttackTarget = attackTarget;
			State = AIState.ATTACKING;
		}
	}

	protected Vector3 FindTrueMoveDestination(Vector3 target, Vector3? groupMovementCenter = null) {
		Vector3 destination = target;

		if (groupMovementCenter != null) {
			//maintain an offset from the center of the group if using group movement
			destination += (transform.position - (Vector3)groupMovementCenter).normalized * agent.radius * 3;
		}

		NavMeshHit navMeshHit;
		if (NavMesh.SamplePosition(destination, out navMeshHit, 1f, NavMesh.AllAreas)) {
			return navMeshHit.position;
		}

		return destination;
	}

	protected bool SetMoveDestination(Vector3 destination) {
		if (agent.destination != destination) {
			StartCoroutine(DelayedMove(destination));
			return true;
		} else {
			return false;
		}
	}

	protected IEnumerator DelayedMove(Vector3 destination) {
		if (obstacle.enabled) {
			obstacle.enabled = false;
			yield return 1; //wait one frame before moving to let the nav mesh update
		}

		agent.enabled = true;
		agent.destination = destination;
		agent.isStopped = false;
	}

	protected void StopMovement() {
		if (agent.enabled) {
			agent.isStopped = true;
			agent.enabled = false;
			obstacle.enabled = true;
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

		if (animator != null) {
			animator.SetFloat("velocityX", CurrentVelocity.x);
			animator.SetFloat("velocityY", CurrentVelocity.y);
			animator.SetFloat("velocityZ", CurrentVelocity.z);
		}
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		CurrentVelocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		AttackTrigger = e.Reader.ReadBoolean();
		string attackTargetId = e.Reader.ReadString();
		if (attackTargetId != "") {
			AttackTarget = clientEntityManager.GetEntity(attackTargetId);
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);
		
		e.Writer.Write(CurrentVelocity.x); e.Writer.Write(CurrentVelocity.y); e.Writer.Write(CurrentVelocity.z);
		e.Writer.Write(AttackTrigger);
		e.Writer.Write(AttackTarget != null ? AttackTarget.ID : "");

		AttackTrigger = false;
	}
}
