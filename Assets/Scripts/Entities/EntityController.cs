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
	private bool isRespondingToAttackCommand = false;
	private Vector3 lastMoveDestination = Vector3.zero;

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
		isRespondingToAttackCommand = false;

		AttackTarget = CheckForTargets();
		if (AttackTarget != null) {
			State = AIState.ATTACKING;
		}
	}

	private Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where target.TeamId != entity.TeamId && distance <= entity.properties.attackRange
				orderby distance descending
				select target).FirstOrDefault();
	}

	private void HandleMovingState() {
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

	private void HandleCombatState() {
		if (AttackTarget == null || !AttackTarget.GetComponent<EntityController>().isVisible) {
			//target disappeared or died
			Stop();
		} else {
			//look at target's x/z position
			Vector3 lookTarget = new Vector3(AttackTarget.transform.position.x,
									   transform.position.y,
									   AttackTarget.transform.position.z);
			transform.LookAt(lookTarget);

			if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > entity.properties.attackRange) {
				//out of range, move into range if responding to an explicit attack command
				if (isRespondingToAttackCommand) {
					SetMoveDestination(FindTrueMoveDestination(AttackTarget.transform.position));
				}
			} else {
				//in range, stop moving
				StopMovement();

				if (attackCooldown <= 0f) {
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

	public void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		AttackTarget = null;
		if (SetMoveDestination(FindTrueMoveDestination(target, groupMovementCenter))) {
			State = AIState.MOVING;
		}
	}

	public void Stop() {
		AttackTarget = null;
		StopMovement();
		State = AIState.IDLE;
	}

	public void Attack(Entity attackTarget) {
		if (attackTarget != null) {
			isRespondingToAttackCommand = true;
			AttackTarget = attackTarget;
			StopMovement();
			State = AIState.ATTACKING;
		}
	}

	private Vector3 FindTrueMoveDestination(Vector3 target, Vector3? groupMovementCenter = null) {
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

	private bool SetMoveDestination(Vector3 destination) {
		if (agent.destination != destination) {
			StartCoroutine(DelayedMove(destination));
			return true;
		} else {
			return false;
		}
	}

	private IEnumerator DelayedMove(Vector3 destination) {
		if (obstacle.enabled) {
			obstacle.enabled = false;
			yield return 1; //wait one frame before moving to let the nav mesh update
		}

		agent.enabled = true;
		agent.destination = destination;
		agent.isStopped = false;
		lastMoveDestination = destination;
	}

	private void StopMovement() {
		if (agent.enabled) {
			agent.isStopped = true;
			agent.enabled = false;
			obstacle.enabled = true;
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

	public void VisibilityTargetDispatch(ICollection<Entity> targets) {
		if (UpdateVisibility) {
			if (targets != null) {
				bool isVisibleNow = targets.Contains(entity);
				if (isVisibleNow != isVisible) {
					isVisible = isVisibleNow;
					OnVisibilityUpdated(isVisible);
				}
			} else {
				OnVisibilityUpdated(false);
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
