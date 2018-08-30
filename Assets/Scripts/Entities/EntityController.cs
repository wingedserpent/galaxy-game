using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public delegate void OnDeath(Entity entity);
public delegate void OnVisibilityUpdated(bool isVisible);

public class EntityController : MonoBehaviour, IDarkRiftSerializable {

	public GameObject deathEffectPrefab;
	public AudioClip damagedSound;
	public AudioClip deathSound;
	public EntityHUD unitHUDPrefab;
	
	public List<WeaponEffect> weaponEffects;

	public AIState State { get; set; }
	public bool DamagedTrigger { get; set; }
	public bool UpdateVisibility { get; set; }
	public bool IsVisible { get; private set; }
	public EntityHUD EntityHUD { get; private set; }
	public Vector3 CurrentVelocity { get; set; }
	public Entity AttackTarget { get; set; }
	public bool AttackTrigger { get; set; }

	protected float attackCooldown = 0f;
	protected bool isDelayingAttack = false;
	protected bool isRespondingToAttackCommand = false;
	protected string lastAttackTargetId;
	protected WeaponEffect activeWeaponEffect;
	protected int currentDamage = 0;
	protected float damageIncreaseTimer = 0f;

	public static event OnDeath OnDeath = delegate { };

	public event OnVisibilityUpdated OnVisibilityUpdated = delegate { };

	protected bool isQuitting = false;

	protected Entity entity;
	protected Vision vision;
	protected Animator animator;
	protected AudioSource audioSource;
	protected NavMeshAgent agent;
	protected NavMeshObstacle obstacle;

	protected virtual void Awake() {
		entity = GetComponent<Entity>();
		vision = GetComponentInChildren<Vision>();
		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();
		agent = GetComponent<NavMeshAgent>();
		obstacle = GetComponent<NavMeshObstacle>();

		State = AIState.IDLE;
		DamagedTrigger = false;
		AttackTrigger = false;
		UpdateVisibility = true;
		IsVisible = true;
	}

	protected virtual void Start() {
		VisibilityManager.VisibilityTargetDispatch += VisibilityTargetDispatch;

		if (vision != null) {
			vision.viewRadius = entity.VisionRange;
		}
		if (agent != null) {
			agent.speed = entity.MoveSpeed;
			agent.acceleration = entity.MoveSpeed * entity.MoveSpeed;
		}

		if (NetworkStatus.Instance.IsClient && unitHUDPrefab != null) {
			EntityHUD = Instantiate<GameObject>(unitHUDPrefab.gameObject, FindObjectOfType<Canvas>().transform).GetComponent<EntityHUD>();
			EntityHUD.Entity = entity;
		}

		SetActiveWeaponEffect();
	}

	protected virtual void Update() {
		if (NetworkStatus.Instance.IsServer) {
			if (attackCooldown > 0f) {
				attackCooldown -= Time.deltaTime;
			}

			HandleAIStates();

			if (agent != null) {
				CurrentVelocity = transform.InverseTransformVector(agent.velocity);
			}
		} else {
			UpdateEffects();
		}
	}

	protected virtual void HandleAIStates() {
		if (State == AIState.IDLE) {
			HandleIdleState();
			return;
		} else if (State == AIState.MOVING) {
			HandleMovingState();
			return;
		} else if (State == AIState.ATTACKING) {
			HandleCombatState();
			return;
		}
	}

	protected virtual void HandleIdleState() {
		if (entity.CanAttack) {
			isRespondingToAttackCommand = false;

			AttackTarget = CheckForTargets();
			if (AttackTarget != null) {
				SetCurrentDamage(entity.Weapon.Damage);
				State = AIState.ATTACKING;
			}
		}
	}

	protected Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where CanAttackTarget(target) && distance <= entity.Weapon.Range
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
			if (damageIncreaseTimer > 0f) {
				damageIncreaseTimer -= Time.deltaTime;
				if (damageIncreaseTimer <= 0f) {
					SetCurrentDamage(currentDamage + 1);
				}
			}

			//look at target's x/z position
			Vector3 lookTarget = new Vector3(AttackTarget.transform.position.x,
									   transform.position.y,
									   AttackTarget.transform.position.z);
			transform.LookAt(lookTarget);

			if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > entity.Weapon.Range) {
				//out of range, try to move into range if responding to an explicit attack command
				if (entity.CanMove && isRespondingToAttackCommand) {
					SetMoveDestination(FindTrueMoveDestination(AttackTarget.transform.position));
				} else {
					//out of range and either can't move or not allowed to chase, so give up
					AttackTarget = null;
					State = AIState.IDLE;
				}
			} else {
				//in range, stop moving
				StopMovement();

				if (!isDelayingAttack && attackCooldown <= 0f) {
					//do the attack after a delay (for visual effects)
					StartCoroutine(DelayedAttack(AttackTarget));
					AttackTrigger = true;
				}
			}
		}
	}

	protected IEnumerator DelayedAttack(Entity attackTarget) {
		float attackEffectTime = activeWeaponEffect != null ? activeWeaponEffect.attackEffectTime : 0f;

		if (attackEffectTime > 0f) {
			isDelayingAttack = true;
			yield return new WaitForSeconds(attackEffectTime);
		}

		if (attackTarget != null) {
			bool targetDied = attackTarget.EntityController.TakeDamage(currentDamage);
			if (targetDied) {
				AttackTarget = null;
				State = AIState.IDLE;
			}

			if (entity.Weapon.SplashRadius > 0f) {
				Collider[] overlaps = Physics.OverlapSphere(attackTarget.transform.position, entity.Weapon.SplashRadius, LayerMask.NameToLayer("Unit"));
				IEnumerable<Entity> damagedTargets = (from target in overlaps.Select(x => x.GetComponentInParent<Entity>())
													  where CanSplashAttackTarget(target, attackTarget)
													  select target);
				foreach (Entity entity in damagedTargets) {
					entity.EntityController.TakeDamage(currentDamage);
				}
			}
		}

		attackCooldown = entity.Weapon.AttackRate - attackEffectTime;
		isDelayingAttack = false;
	}

	protected void SetCurrentDamage(int damage) {
		currentDamage = damage;
		if (currentDamage < entity.Weapon.MaxDamage) {
			damageIncreaseTimer = (entity.Weapon.DamageIncreaseTime / (entity.Weapon.MaxDamage - entity.Weapon.Damage));
		} else {
			damageIncreaseTimer = 0f;
		}
	}

	public virtual void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		if (entity.CanMove) {
			AttackTarget = null;
			if (SetMoveDestination(FindTrueMoveDestination(target, groupMovementCenter))) {
				State = AIState.MOVING;
			}
		}
	}

	public virtual void Stop() {
		State = AIState.IDLE;
		AttackTarget = null;
		StopMovement();
	}

	public virtual void Attack(Entity attackTarget) {
		if (CanAttackTarget(attackTarget)) {
			isRespondingToAttackCommand = true;
			StopMovement();
			AttackTarget = attackTarget;
			SetCurrentDamage(entity.Weapon.Damage);
			State = AIState.ATTACKING;
		}
	}

	protected bool CanAttackTarget(Entity target) {
		return target != null && entity.CanAttack && target.TeamId != entity.TeamId
			&& target.isInAir ? entity.canAttackAir : entity.canAttackGround;
	}

	protected bool CanSplashAttackTarget(Entity target, Entity originalTarget) {
		return target != null && target.TeamId != entity.TeamId 
			&& (originalTarget.isInAir == target.isInAir);
	}

	protected Vector3 FindTrueMoveDestination(Vector3 target, Vector3? groupMovementCenter = null) {
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

	protected bool SetMoveDestination(Vector3 destination) {
		if (agent.destination != destination) {
			StartCoroutine(DelayedMove(destination));
			return true;
		} else {
			return false;
		}
	}

	protected IEnumerator DelayedMove(Vector3 destination) {
		if (obstacle != null && obstacle.enabled) {
			obstacle.enabled = false;
			yield return 1; //wait one frame before moving to let the nav mesh update
		}

		agent.enabled = true;
		agent.destination = destination;
		agent.isStopped = false;
	}

	protected void StopMovement() {
		if (agent != null && agent.enabled) {
			agent.isStopped = true;
			agent.enabled = false;
		}
		if (obstacle != null) {
			obstacle.enabled = true;
		}
	}

	public virtual bool TakeDamage(int amount) {
		int startingShield = entity.CurrentShield;
		int resultShield = entity.AdjustShield(-amount);
		if (startingShield - resultShield < amount) {
			int resultHealth = entity.AdjustHealth(-amount);
			if (resultHealth <= 0) {
				OnDeath(entity);
				return true;
			}
			DamagedTrigger = true;
		}
		return false;
	}

	public void Die() {
		if (EntityHUD != null) {
			Destroy(EntityHUD.gameObject);
		}
		if (animator != null && animator.enabled) {
			animator.SetTrigger("die");
		}
		if (audioSource != null) {
			audioSource.PlayOneShot(deathSound);
		}
	}

	protected void OnApplicationQuit() {
		isQuitting = true;
	}

	protected void OnDestroy() {
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
				if (isVisibleNow != IsVisible) {
					IsVisible = isVisibleNow;
					OnVisibilityUpdated(IsVisible);
				}
			} else {
				IsVisible = false;
				OnVisibilityUpdated(false);
			}
		}
	}

	protected void SetActiveWeaponEffect() {
		activeWeaponEffect = weaponEffects.Where(x => x.weaponType.Equals(entity.Weapon.WeaponType)).FirstOrDefault();
		if (activeWeaponEffect != null && activeWeaponEffect.projectileSpawnPoints.Count == 0) {
			activeWeaponEffect.projectileSpawnPoints.Add(transform);
		}
	}

	protected virtual void UpdateEffects() {
		if (DamagedTrigger) {
			if (animator != null && animator.enabled) {
				animator.SetTrigger("damaged");
			}
			if (audioSource != null) {
				audioSource.PlayOneShot(damagedSound);
			}
			DamagedTrigger = false;
		}

		if (entity.CanAttack && AttackTrigger) {
			if (animator != null && animator.enabled) {
				animator.SetTrigger("attack");
			}
			if (audioSource != null) {
				audioSource.PlayOneShot(activeWeaponEffect.attackSound);
			}
			if (AttackTarget != null) {
				if (activeWeaponEffect.projectilePrefab != null) {
					foreach (Transform spawnPoint in activeWeaponEffect.projectileSpawnPoints) {
						Tweener projectile = Instantiate<GameObject>(activeWeaponEffect.projectilePrefab.gameObject, spawnPoint.position, spawnPoint.rotation).GetComponent<Tweener>();
						projectile.target = AttackTarget.transform;
						projectile.time = activeWeaponEffect.attackEffectTime;
						projectile.enabled = true;
					}
				}
				if (activeWeaponEffect.existingProjectiles != null) {
					foreach (GameObject projectileGO in activeWeaponEffect.existingProjectiles) {
						Tweener projectile = projectileGO.GetComponent<Tweener>();
						projectile.target = AttackTarget.transform;
						projectile.time = activeWeaponEffect.attackEffectTime;
						projectile.enabled = true;
					}
				}
			}
			AttackTrigger = false;
		}

		if (animator != null && animator.enabled) {
			if (entity.CanMove) {
				animator.SetFloat("velocityX", CurrentVelocity.x);
				animator.SetFloat("velocityY", CurrentVelocity.y);
				animator.SetFloat("velocityZ", CurrentVelocity.z);
			}
			if (entity.CanAttack) {
				animator.SetBool("isAttacking", State == AIState.ATTACKING);
			}
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
		DamagedTrigger = e.Reader.ReadBoolean();

		if (entity.CanMove) {
			CurrentVelocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		}

		if (entity.CanAttack) {
			AttackTrigger = e.Reader.ReadBoolean();
			string attackTargetId = e.Reader.ReadString();

			if (!attackTargetId.Equals(lastAttackTargetId)) {
				AttackTarget = ClientEntityManager.Instance.GetEntity(attackTargetId);
				lastAttackTargetId = attackTargetId;
			}

			SetActiveWeaponEffect();
		}
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write((int)State);
		e.Writer.Write(DamagedTrigger);

		if (entity.CanMove) {
			e.Writer.Write(CurrentVelocity.x); e.Writer.Write(CurrentVelocity.y); e.Writer.Write(CurrentVelocity.z);
		}

		if (entity.CanAttack) {
			e.Writer.Write(AttackTrigger);
			e.Writer.Write(AttackTarget != null ? AttackTarget.ID : "");
			AttackTrigger = false;
		}

		DamagedTrigger = false;
	}
}

public enum AIState {
	IDLE,
	MOVING,
	ATTACKING
}
