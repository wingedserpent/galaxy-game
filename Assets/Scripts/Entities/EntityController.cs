using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public delegate void OnDeath(Entity entity);
public delegate void OnVisibilityUpdated(bool isVisible);

public class EntityController : MonoBehaviour, IDarkRiftSerializable {

	public GameObject projectilePrefab;
	public GameObject deathEffectPrefab;

	public AudioClip attackSound;
	public AudioClip damagedSound;
	public AudioClip deathSound;

	public UnitHUD unitHUDPrefab;

	public AIState State { get; set; }
	public Entity AttackTarget { get; set; }
	public Vector3 CurrentVelocity { get; set; }
	public bool AttackTrigger { get; set; }
	public bool DamagedTrigger { get; set; }
	public bool UpdateVisibility { get; set; }
	public UnitHUD UnitHUD { get; private set; }

	public static event OnDeath OnDeath = delegate { };

	public event OnVisibilityUpdated OnVisibilityUpdated = delegate { };
	private bool isVisible = true;

	private float attackCooldown = 0f;
	private bool isQuitting = false;

	private Entity entity;
	private NavMeshAgent agent;
	private NavMeshObstacle obstacle;
	private Vision vision;
	private Animator animator;
	private AudioSource audioSource;
	private ClientEntityManager clientEntityManager;

	protected virtual void Awake() {
		entity = GetComponent<Entity>();
		agent = GetComponent<NavMeshAgent>();
		obstacle = GetComponent<NavMeshObstacle>();
		vision = GetComponentInChildren<Vision>();
		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();

		State = AIState.IDLE;
		AttackTrigger = false;
		DamagedTrigger = false;
		UpdateVisibility = true;
	}

	private void Start() {
		clientEntityManager = ClientEntityManager.Instance;
		
		VisibilityManager.VisibilityTargetDispatch += VisibilityTargetDispatch;

		if (NetworkStatus.Instance.IsClient && unitHUDPrefab != null) {
			UnitHUD = Instantiate<GameObject>(unitHUDPrefab.gameObject, FindObjectOfType<Canvas>().transform).GetComponent<UnitHUD>();
			UnitHUD.Entity = entity;
		}
	}

	protected virtual void Update() {
		if (NetworkStatus.Instance.IsServer) {
			if (attackCooldown > 0f) {
				attackCooldown -= Time.deltaTime;
			}

			if (State == AIState.IDLE) {
				HandleIdleState();
			} else if (State == AIState.MOVING) {
				HandleMovingState();
			} else if (State == AIState.ATTACKING) {
				HandleCombatState();
			}

			CurrentVelocity = transform.InverseTransformVector(agent.velocity);
		} else {
			UpdateEffects();
		}
	}

	private void HandleIdleState() {
		AttackTarget = CheckForTargets();
		if (AttackTarget != null) {
			State = AIState.ATTACKING;
			//HandleCombatState(); call immediately or wait a frame?
		}
	}

	private void HandleMovingState() {
		/* for now, can't attack if moving
		attackTarget = CheckForTargets();
		if (attackTarget != null) {
			Stop();
			State = AIState.COMBAT;
			HandleCombatState();
		} */

		// Check if we've reached the destination
		if (!agent.pathPending) {
			if (agent.remainingDistance <= agent.stoppingDistance) {
				if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) {
					DisableMovement();
					State = AIState.IDLE;
				}
			}
		}
	}

	private void HandleCombatState() {
		if (AttackTarget == null) {
			//target disappeared or died
			State = AIState.IDLE;
		} else {
			//look at target's x/z position
			Vector3 lookTarget = new Vector3(AttackTarget.transform.position.x,
									   transform.position.y,
									   AttackTarget.transform.position.z);
			transform.LookAt(lookTarget);

			if (attackCooldown <= 0f) {
				if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > entity.properties.attackRange) {
					//out of range
					AttackTarget = null;
					State = AIState.IDLE;
				} else {
					//do the attack
					bool targetDied = AttackTarget.GetComponent<EntityController>().TakeDamage(entity.properties.attackDamage);
					if (targetDied) {
						AttackTarget = null;
						State = AIState.IDLE;
					}
					attackCooldown = entity.properties.attackSpeed;
					AttackTrigger = true;
				}
			}
		}
	}

	private Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where target.TeamId != entity.TeamId && distance <= entity.properties.attackRange
				orderby distance descending
				select target).FirstOrDefault();
	}

	public void MoveTo(Vector3 target, Vector3? groupMovementCenter = null) {
		AttackTarget = null;
		EnableMovement();
		agent.destination = FindMovementDestination(target, groupMovementCenter);
		State = AIState.MOVING;
	}

	private void EnableMovement() {
		obstacle.enabled = false;
		agent.enabled = true;
		agent.isStopped = false;
	}

	private Vector3 FindMovementDestination(Vector3 target, Vector3? groupMovementCenter = null) {
		Vector3 destination = target;

		if (groupMovementCenter != null) {
			//maintain an offset from the center of the group if using group movement
			destination += (transform.position - (Vector3)groupMovementCenter).normalized * agent.radius * 2;
		}

		NavMeshHit navMeshHit;
		if (NavMesh.SamplePosition(destination, out navMeshHit, 1f, NavMesh.AllAreas)) {
			return navMeshHit.position;
		}

		return destination;
	}

	public void Stop() {
		AttackTarget = null;
		DisableMovement();
		State = AIState.IDLE;
	}

	private void DisableMovement() {
		if (agent.enabled) {
			agent.isStopped = true;
			agent.enabled = false;
			obstacle.enabled = true;
		}
	}

	public void Attack(Entity attackTarget) {
		if (attackTarget != null) {
			AttackTarget = attackTarget;
			DisableMovement();
			State = AIState.ATTACKING;
		}
	}

	public virtual bool TakeDamage(int amount) {
		int resultHealth = entity.properties.AdjustHealth(-amount);
		if (resultHealth <= 0) {
			OnDeath(entity);
			return true;
		}
		DamagedTrigger = true;
		return false;
	}

	public void Die() {
		if (UnitHUD != null) {
			Destroy(UnitHUD.gameObject);
		}
		if (animator != null) {
			animator.SetTrigger("die");
		}
		if (audioSource != null) {
			audioSource.PlayOneShot(deathSound);
		}
	}

	private void OnApplicationQuit() {
		isQuitting = true;
	}

	private void OnDestroy() {
		VisibilityManager.VisibilityTargetDispatch -= VisibilityTargetDispatch;

		if (!isQuitting) {
			if (deathEffectPrefab != null) {
				Instantiate<GameObject>(deathEffectPrefab.gameObject, transform.position, transform.rotation);
			}
		}
	}

	private void VisibilityTargetDispatch(ICollection<Entity> targets) {
		if (UpdateVisibility) {
			bool isVisibleNow = targets.Contains(entity);
			if (isVisibleNow != isVisible) {
				isVisible = isVisibleNow;
				OnVisibilityUpdated(isVisible);
			}
		}
	}

	private void UpdateEffects() {
		if (AttackTrigger) {
			if (animator != null) {
				animator.SetTrigger("attack");
			}
			if (audioSource != null) {
				audioSource.PlayOneShot(attackSound);
			}
			if (projectilePrefab != null) {
				Projectile projectile = Instantiate<GameObject>(projectilePrefab.gameObject, transform.position, transform.rotation).GetComponent<Projectile>();
				projectile.target = AttackTarget.transform;
			}
			AttackTrigger = false;
		}

		if (DamagedTrigger) {
			if (animator != null) {
				animator.SetTrigger("damaged");
			}
			if (audioSource != null) {
				audioSource.PlayOneShot(damagedSound);
			}
			DamagedTrigger = false;
		}

		if (animator != null) {
			animator.SetBool("isAttacking", State == AIState.ATTACKING);
			animator.SetFloat("velocityX", CurrentVelocity.x);
			animator.SetFloat("velocityY", CurrentVelocity.y);
			animator.SetFloat("velocityZ", CurrentVelocity.z);
		}
	}

	public virtual CommandType GetCommandTypeFromInput() {
		return CommandType.NONE;
	}

	public virtual void ExecuteCommand(CommandType commandType) {
		//do nothing, just be virtual
	}

	public virtual void Deserialize(DeserializeEvent e) {
		State = (AIState)e.Reader.ReadInt32();
		AttackTrigger = e.Reader.ReadBoolean();
		DamagedTrigger = e.Reader.ReadBoolean();
		string attackTargetId = e.Reader.ReadString();
		if (attackTargetId != "") {
			AttackTarget = clientEntityManager.GetEntity(attackTargetId);
		}
		CurrentVelocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write((int)State);
		e.Writer.Write(AttackTrigger);
		e.Writer.Write(DamagedTrigger);
		e.Writer.Write(AttackTarget != null ? AttackTarget.ID : "");
		e.Writer.Write(CurrentVelocity.x); e.Writer.Write(CurrentVelocity.y); e.Writer.Write(CurrentVelocity.z);

		AttackTrigger = false;
		DamagedTrigger = false;
	}
}

public enum AIState {
	IDLE,
	MOVING,
	ATTACKING
}
