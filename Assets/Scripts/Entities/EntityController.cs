using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public delegate void OnDeath(Entity entity);
public delegate void OnDespawn(Entity entity);
public delegate void OnVisibilityUpdated(bool isVisible);

public class EntityController : MonoBehaviour, IDarkRiftSerializable {

	public EntityHUD unitHUDPrefab;
	public GameObject targetingPrefab;
	public GameObject deathEffectPrefab;
	public AudioClip damagedSound;
	public AudioClip deathSound;
	public GameObject targetedAreaPrefab;

	public List<WeaponEffect> weaponEffects;

	public List<InputCommand> AvailableCommands { get; protected set; }

	public AIState State { get; set; }
	public bool DamagedTrigger { get; set; }
	public bool UpdateVisibility { get; set; }
	public bool IsVisible { get; protected set; }
	public EntityHUD EntityHUD { get; protected set; }
	public Vector3 CurrentVelocity { get; set; }
	public Entity AttackTarget { get; protected set; }
	public Vector3 AttackLocation { get; protected set; }
	public bool AttackStartTrigger { get; set; }
	public bool AttackEffectTrigger { get; set; }
	public bool AttackEndTrigger { get; set; }
	public bool IsSelected { get; protected set; }
	public bool IsRetreating { get; protected set; }

	protected bool isAttackingAndInRange = false;
	protected float attackCooldown = 0f;
	protected bool isDelayingAttack = false;
	protected bool isRespondingToAttackCommand = false;
	protected string lastAttackTargetId;
	protected WeaponEffect activeWeaponEffect;
	protected int currentDamage = 0;
	protected float damageIncreaseTimer = 0f;
	protected GameObject targetedAreaMarker;

	protected const float retreatMaxDistance = 1f;
	protected TeamSpawn retreatDestination;

	public static event OnDeath OnDeath = delegate { };
	public static event OnDespawn OnDespawn = delegate { };

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
		AttackStartTrigger = false;
		AttackEffectTrigger = false;
		AttackEndTrigger = false;
		UpdateVisibility = true;
		IsVisible = true;
		IsRetreating = false;

		AvailableCommands = new List<InputCommand>();
		AvailableCommands.Add(new InputCommand(KeyCode.Q, "Stop"));
		AvailableCommands.Add(new InputCommand(KeyCode.R, "Retreat"));
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
			EntityHUD = Instantiate<GameObject>(unitHUDPrefab.gameObject, UIManager.Instance.entityHudContainer).GetComponent<EntityHUD>();
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

	protected virtual void SetState(AIState newState) {
		if (newState != State) {
			if (newState == AIState.IDLE) {
				StopMovement();
				StopAttacking();
				AttackTarget = null;
				isRespondingToAttackCommand = false;
			} else if (newState == AIState.MOVING) {
				StopAttacking();
				AttackTarget = null;
				isRespondingToAttackCommand = false;
			} else if (newState == AIState.ATTACKING) {
				StopMovement();
				SetCurrentDamage(entity.Weapon.Damage);
			}
			IsRetreating = false;

			State = newState;
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
		if (entity.CanAttackTarget) {
			AttackTarget = CheckForTargets();
			if (AttackTarget != null) {
				SetState(AIState.ATTACKING);
			}
		} else if (entity.CanAttackLocation) {
			if (GetTargetsAtAttackLocation(AttackLocation).Count > 0) {
				SetState(AIState.ATTACKING);
			}
		}
	}

	protected virtual Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where CanHitTarget(target) && distance <= entity.Weapon.Range
				orderby distance ascending
				select target).FirstOrDefault();
	}

	protected virtual List<Entity> GetTargetsAtAttackLocation(Vector3 attackLocation) {
		Collider[] overlaps = Physics.OverlapSphere(attackLocation, entity.Weapon.SplashRadius, LayerManager.Instance.damageableMask);
		return (from target in overlaps.Select(x => x.GetComponentInParent<Entity>())
				where CanHitTarget(target)
				select target).ToList();
	}

	protected virtual void HandleMovingState() {
		// Check if we've reached the destination
		if (agent.enabled && !agent.pathPending) {
			if (agent.remainingDistance <= agent.stoppingDistance) {
				if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) {
					if (IsRetreating) {
						Vector3 posXZ = transform.position;
						posXZ.y = 0f;
						Vector3 retreatXZ = retreatDestination.transform.position;
						retreatXZ.y = 0f;
						if (Vector3.Distance(posXZ, retreatXZ) <= retreatMaxDistance) {
							OnDespawn(entity);
						}
					}
					SetState(AIState.IDLE);
				}
			}
		}
	}

	protected virtual void HandleCombatState() {
		if (entity.CanAttackTarget) {
			if (AttackTarget == null || !AttackTarget.EntityController.IsVisible) {
				//target disappeared or died
				Stop();
			} else {
				if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > entity.Weapon.Range) {
					//out of range, stop attacking and reset weapon damage
					StopAttacking();

					//try to move into range if responding to an explicit attack command
					if (entity.CanMove && isRespondingToAttackCommand) {
						SetMoveDestination(FindTrueMoveDestination(AttackTarget.transform.position));
					} else {
						//out of range and either can't move or not allowed to chase, so give up
						SetState(AIState.IDLE);
					}
				} else {
					//set initial attacking variables
					if (!isAttackingAndInRange) {
						isAttackingAndInRange = true;
						AttackStartTrigger = true;
					}

					//in range, make sure we're not moving
					StopMovement();

					//increment damage timer
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

					if (!isDelayingAttack && attackCooldown <= 0f) {
						//do the attack after a delay (for visual effects)
						StartCoroutine(DelayedAttack(AttackTarget));
						AttackEffectTrigger = true;
					}
				}
			}
		} else if (entity.CanAttackLocation) {
			if (GetTargetsAtAttackLocation(AttackLocation).Count == 0) {
				Stop();
			} else {
				//set initial attacking variables
				if (!isAttackingAndInRange) {
					isAttackingAndInRange = true;
					AttackStartTrigger = true;
				}

				if (!isDelayingAttack && attackCooldown <= 0f) {
					//do the attack after a delay (for visual effects)
					StartCoroutine(DelayedLocationAttack(AttackLocation));
					AttackEffectTrigger = true;
				}
			}
		}
	}

	protected IEnumerator DelayedAttack(Entity attackTarget) {
		if (activeWeaponEffect.attackEffectTime > 0f) {
			isDelayingAttack = true;
			yield return new WaitForSeconds(activeWeaponEffect.attackEffectTime);
		}
		isDelayingAttack = false;

		if (attackTarget != null) {
			bool targetDied = attackTarget.EntityController.TakeDamage(currentDamage);
			DoSplashAttack(attackTarget);

			if (targetDied) {
				SetState(AIState.IDLE);
			}
		}

		attackCooldown = entity.Weapon.AttackRate - activeWeaponEffect.attackEffectTime;
	}

	protected virtual void DoSplashAttack(Entity attackTarget) {
		if (entity.Weapon.SplashRadius > 0f) {
			Collider[] overlaps = Physics.OverlapSphere(attackTarget.transform.position, entity.Weapon.SplashRadius, LayerManager.Instance.damageableMask);
			IEnumerable<Entity> damagedTargets = (from target in overlaps.Select(x => x.GetComponentInParent<Entity>())
												  where target != attackTarget && CanSplashHitTarget(target, attackTarget)
												  select target);
			foreach (Entity entity in damagedTargets) {
				entity.EntityController.TakeDamage(currentDamage);
			}
		}
	}

	protected virtual IEnumerator DelayedLocationAttack(Vector3 attackLocation) {
		if (activeWeaponEffect.attackEffectTime > 0f) {
			isDelayingAttack = true;
			yield return new WaitForSeconds(activeWeaponEffect.attackEffectTime);
		}
		isDelayingAttack = false;

		DoLocationAttack(attackLocation);

		attackCooldown = entity.Weapon.AttackRate - activeWeaponEffect.attackEffectTime;
	}

	protected virtual void DoLocationAttack(Vector3 attackLocation) {
		foreach (Entity entity in GetTargetsAtAttackLocation(attackLocation)) {
			entity.EntityController.TakeDamage(currentDamage);
		}
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
			if (SetMoveDestination(FindTrueMoveDestination(target, groupMovementCenter))) {
				SetState(AIState.MOVING);
			}
		}
	}

	public virtual void Retreat(TeamSpawn myTeamSpawn) {
		if (entity.CanMove) {
			retreatDestination = myTeamSpawn;
			Move(retreatDestination.transform.position);
			IsRetreating = true;
		}
	}

	public virtual void Stop() {
		SetState(AIState.IDLE);
	}

	public virtual void Attack(Entity attackTarget) {
		if (CanHitTarget(attackTarget)) {
			isRespondingToAttackCommand = true;
			AttackTarget = attackTarget;
			SetState(AIState.ATTACKING);
		}
	}

	public virtual void Attack(Vector3 attackTarget) {
		//intended to be overridden
	}

	protected bool CanHitTarget(Entity target) {
		return target != null && target.TeamId != entity.TeamId && (target.isInAir ? entity.canAttackAir : entity.canAttackGround);
	}

	protected bool CanSplashHitTarget(Entity target, Entity originalTarget) {
		return target != null && target.TeamId != entity.TeamId && originalTarget.isInAir == target.isInAir;
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

	protected virtual void StopAttacking() {
		if (isAttackingAndInRange) {
			isAttackingAndInRange = false;
			AttackEndTrigger = true;
			SetCurrentDamage(entity.Weapon.Damage);
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

	public void CleanUpForDespawn() {
		if (EntityHUD != null) {
			Destroy(EntityHUD.gameObject);
		}
	}

	public IEnumerator Die(bool destroy, float destroyDelay = 0f) {
		if (animator != null && animator.enabled) {
			animator.SetTrigger("die");
		}

		if (destroy) {
			if (destroyDelay > 0f) {
				yield return new WaitForSeconds(destroyDelay);
			}

			//check not needed anymore?
			//if (!isQuitting && (OverallStateManager.Instance == null || !OverallStateManager.Instance.IsInSceneTransition)) {
			if (deathEffectPrefab != null) {
				Instantiate<GameObject>(deathEffectPrefab.gameObject, transform.position, transform.rotation);
			}
			//}

			if (audioSource != null) {
				audioSource.PlayOneShot(deathSound);
			}

			CleanUpForDespawn();

			Destroy(gameObject);
		}
	}

	protected void OnApplicationQuit() {
		isQuitting = true;
	}

	protected virtual void OnDestroy() {
		VisibilityManager.VisibilityTargetDispatch -= VisibilityTargetDispatch;
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

	public virtual void OnSelected() {
		IsSelected = true;
	}

	public virtual void OnDeselected() {
		IsSelected = false;
		DestroyTargetedAreaMarker();
	}

	protected void CreateTargetedAreaMarker() {
		if (targetedAreaMarker == null) {
			targetedAreaMarker = Instantiate<GameObject>(targetedAreaPrefab, AttackLocation, Quaternion.identity);
		}
	}

	protected void DestroyTargetedAreaMarker() {
		if (targetedAreaMarker != null) {
			Destroy(targetedAreaMarker);
			targetedAreaMarker = null;
		}
	}

	protected void SetActiveWeaponEffect() {
		if (activeWeaponEffect == null && entity.Weapon != null) {
			activeWeaponEffect = weaponEffects.Where(x => x.weaponType.Equals(entity.Weapon.WeaponType)).FirstOrDefault();
			if (activeWeaponEffect != null && activeWeaponEffect.projectileSpawnPoints.Count == 0) {
				activeWeaponEffect.projectileSpawnPoints.Add(transform);
			}
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

		if (AttackStartTrigger) {
			if (AttackTarget != null || entity.CanAttackLocation) {
				if (activeWeaponEffect.isContinuousEffect) {
					foreach (Transform spawnPoint in activeWeaponEffect.projectileSpawnPoints) {
						WeaponBeam beam = Instantiate<GameObject>(activeWeaponEffect.projectilePrefab.gameObject, spawnPoint).GetComponent<WeaponBeam>();
						beam.transform.localPosition = Vector3.zero;
						if (beam != null) {
							beam.start = transform;
							beam.target = AttackTarget != null ? AttackTarget.transform : null;
							beam.targetLocation = AttackLocation;
							beam.totalEffectTime = entity.Weapon.DamageIncreaseTime;
						}
					}
				}
			}

			AttackStartTrigger = false;
		}

		if (AttackEffectTrigger) {
			if (animator != null && animator.enabled) {
				animator.SetTrigger("attack");
			}

			if (audioSource != null) {
				audioSource.PlayOneShot(activeWeaponEffect.attackSound);
			}

			if (AttackTarget != null || entity.CanAttackLocation) {
				if (!activeWeaponEffect.isContinuousEffect) {
					if (activeWeaponEffect.projectilePrefab != null) {
						foreach (Transform spawnPoint in activeWeaponEffect.projectileSpawnPoints) {
							Tweener tweener = Instantiate<GameObject>(activeWeaponEffect.projectilePrefab.gameObject, spawnPoint.position, spawnPoint.rotation).GetComponent<Tweener>();
							if (tweener != null) {
								tweener.target = AttackTarget != null ? AttackTarget.transform : null;
								tweener.TargetPos = AttackLocation;
								tweener.time = activeWeaponEffect.attackEffectTime;
								tweener.enabled = true;
							}
						}
					}

					if (activeWeaponEffect.existingProjectiles != null) {
						foreach (GameObject projectileGO in activeWeaponEffect.existingProjectiles) {
							Tweener projectile = projectileGO.GetComponent<Tweener>();
							projectile.target = AttackTarget != null ? AttackTarget.transform : null;
							projectile.TargetPos = AttackLocation;
							projectile.time = activeWeaponEffect.attackEffectTime;
							projectile.enabled = true;
						}
					}
				}
			}

			AttackEffectTrigger = false;
		}

		if (AttackEndTrigger) {
			foreach (Transform spawnPoint in activeWeaponEffect.projectileSpawnPoints) {
				foreach (WeaponBeam beam in spawnPoint.GetComponentsInChildren<WeaponBeam>()) {
					Destroy(beam.gameObject);
				}
			}

			AttackEndTrigger = false;
		}

		if (animator != null && animator.enabled) {
			if (entity.CanMove) {
				animator.SetFloat("velocityX", CurrentVelocity.x);
				animator.SetFloat("velocityY", CurrentVelocity.y);
				animator.SetFloat("velocityZ", CurrentVelocity.z);
			}

			if (entity.CanAttackTarget || entity.CanAttackLocation) {
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

		if (entity.CanAttackTarget || entity.CanAttackLocation) {
			AttackStartTrigger = e.Reader.ReadBoolean();
			AttackEffectTrigger = e.Reader.ReadBoolean();
			AttackEndTrigger = e.Reader.ReadBoolean();

			string attackTargetId = e.Reader.ReadString();
			if (!attackTargetId.Equals(lastAttackTargetId)) {
				AttackTarget = ClientEntityManager.Instance.GetEntity(attackTargetId);
				lastAttackTargetId = attackTargetId;
			}

			if (entity.CanAttackLocation) {
				AttackLocation = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
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

		if (entity.CanAttackTarget || entity.CanAttackLocation) {
			e.Writer.Write(AttackStartTrigger);
			e.Writer.Write(AttackEffectTrigger);
			e.Writer.Write(AttackEndTrigger);
			e.Writer.Write(AttackTarget != null ? AttackTarget.ID : "");
			
			if (entity.CanAttackLocation) {
				e.Writer.Write(AttackLocation.x); e.Writer.Write(AttackLocation.y); e.Writer.Write(AttackLocation.z);
			}

			AttackStartTrigger = false;
			AttackEffectTrigger = false;
			AttackEndTrigger = false;
		}

		DamagedTrigger = false;
	}
}

public enum AIState {
	IDLE,
	MOVING,
	ATTACKING
}
