using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Linq;
using System.Collections;

//local
public delegate void OnSelected();
public delegate void OnDeselected();
public delegate void OnModifyDamageTaken(int originalAmount, ref int modifiedAmount);
public delegate void OnVisibilityUpdated(bool isVisible);

//static
public delegate void OnDeath(Entity entity);
public delegate void OnDespawn(Entity entity);

public class Entity : OwnedObject {

	public bool canMove = false;
	public bool isInAir = false;

	public float shieldRechargeCooldown = 5f;

	public EntityHUD unitHUDPrefab;
	public GameObject deathEffectPrefab;
	public AudioClip damagedSound;
	public AudioClip deathSound;

	protected InputCommand attackInput = new InputCommand(KeyCode.E, "Attack");
	protected const float retreatMaxDistance = 1f;

	public List<Weapon> weapons { get; protected set; }
	public Dictionary<string, Ability> abilityMap { get; protected set; }
	public List<InputCommand> availableCommands { get; protected set; }

	public Weapon equippedWeapon { get; set; }
	public List<EquipmentData> equipment { get; set; }

	public int maxHealth { get; set; }
	public int currentHealth { get; set; }
	public int maxShield { get; set; }
	public int currentShield { get; set; }
	protected int shieldRechargeRate { get; set; }
	public float moveSpeed { get; set; }
	public float visionRange { get; set; }

	protected AIState state { get; set; }
	protected bool damagedTrigger { get; set; }
	public bool updateVisibility { get; set; }
	public bool isVisible { get; protected set; }
	protected EntityHUD entityHUD { get; set; }
	protected Vector3 currentVelocity { get; set; }
	protected bool isSelected { get; set; }
	
	protected Entity LookTarget { get; set; }
	protected Entity AttackTarget { get; set; }
	
	protected TeamSpawn retreatDestination;

	protected bool isQuitting = false;

	private float shieldRechargeTimer = 0f;

	public event OnSelected OnSelected = delegate { };
	public event OnDeselected OnDeselected = delegate { };
	public event OnModifyDamageTaken OnModifyDamageTaken = delegate { };
	public event OnVisibilityUpdated OnVisibilityUpdated = delegate { };

	public static event OnDeath OnDeath = delegate { };
	public static event OnDespawn OnDespawn = delegate { };

	public Vision vision { get; private set; }
	public Animator animator { get; private set; }
	public AudioSource audioSource { get; private set; }
	public NavMeshAgent agent { get; private set; }
	public NavMeshObstacle obstacle { get; private set; }

	protected override void Awake() {
		base.Awake();

		vision = GetComponentInChildren<Vision>();
		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();
		agent = GetComponent<NavMeshAgent>();
		obstacle = GetComponent<NavMeshObstacle>();

		state = AIState.IDLE;
		damagedTrigger = false;
		updateVisibility = true;
		isVisible = true;
		equipment = new List<EquipmentData>();

		availableCommands = new List<InputCommand>();
		if (canMove) {
			availableCommands.Add(new InputCommand(KeyCode.Q, "Stop"));
			availableCommands.Add(new InputCommand(KeyCode.R, "Retreat"));
		}

		weapons = new List<Weapon>();
		foreach (Weapon weapon in GetComponentsInChildren<Weapon>()) {
			weapon.enabled = false;
			weapons.Add(weapon);
		}

		abilityMap = new Dictionary<string, Ability>();
		foreach (Ability ability in GetComponentsInChildren<Ability>()) {
			abilityMap.Add(ability.abilityTypeId, ability);
			availableCommands.Add(new InputCommand(ability.inputCommand.key, ability.inputCommand.command));
		}
	}

	protected virtual void Start() {
		VisibilityManager.VisibilityTargetDispatch += VisibilityTargetDispatch;

		if (vision != null) {
			vision.viewRadius = visionRange;
		}
		if (agent != null) {
			agent.speed = moveSpeed;
			agent.acceleration = moveSpeed * moveSpeed;
		}

		if (NetworkStatus.Instance.IsClient && unitHUDPrefab != null) {
			entityHUD = Instantiate<GameObject>(unitHUDPrefab.gameObject, UIManager.Instance.entityHudContainer).GetComponent<EntityHUD>();
			entityHUD.Entity = this;
		}
	}

	public int AdjustHealth(int adjustment) {
		currentHealth = Mathf.Clamp(currentHealth + adjustment, 0, maxHealth);
		return currentHealth;
	}

	public int AdjustShield(int adjustment) {
		currentShield = Mathf.Clamp(currentShield + adjustment, 0, maxShield);
		if (adjustment < 0) {
			shieldRechargeTimer = shieldRechargeCooldown;
		}
		return currentShield;
	}

	public void EquipWeapon(WeaponData weaponData) {
		equippedWeapon = weapons.Where(x => x.typeId == weaponData.typeId).FirstOrDefault();
		if (equippedWeapon != null) {
			equippedWeapon.weaponData = weaponData;

			if (equippedWeapon.projectileSpawnPoints.Count == 0) {
				equippedWeapon.projectileSpawnPoints.Add(transform);
			}

			equippedWeapon.enabled = true;

			if (equippedWeapon is LocationWeapon) {
				availableCommands.Add(attackInput);
			}
		}
	}

	public void UnequipWeapon() {
		if (equippedWeapon != null) {
			equippedWeapon.projectileSpawnPoints.Remove(transform);
			equippedWeapon.enabled = false;
			if (equippedWeapon is LocationWeapon) {
				availableCommands.Remove(attackInput);
			}
			equippedWeapon = null;
		}
	}

	public void ApplyEquipment() {
		foreach (EquipmentData equipment in equipment) {
			maxHealth += equipment.health;
			currentHealth += equipment.health;
			maxShield += equipment.shield;
			currentShield += equipment.shield;
			shieldRechargeRate += equipment.shieldRechargeRate;
			moveSpeed += equipment.moveSpeed;
			visionRange += equipment.visionRange;
		}
	}

	public virtual void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		if (canMove) {
			if (SetMoveDestination(FindTrueMoveDestination(target, groupMovementCenter))) {
				SetState(AIState.MOVING);
			}
		}
	}

	public virtual void Retreat(TeamSpawn myTeamSpawn) {
		if (canMove) {
			retreatDestination = myTeamSpawn;
			if (SetMoveDestination(FindTrueMoveDestination(retreatDestination.transform.position))) {
				SetState(AIState.RETREATING);
			}
		}
	}

	public void Attack(Entity attackTarget) {
		if (equippedWeapon != null && equippedWeapon.Attack(attackTarget)) {
			AttackTarget = attackTarget;

			if (LookTarget == null) {
				LookTarget = attackTarget;
			}

			SetState(AIState.ATTACKING);
		}
	}

	public virtual void Attack(Vector3 attackLocation) {
		if (equippedWeapon != null && equippedWeapon.Attack(attackLocation)) {
			AttackTarget = null;

			//look at target's x/z position
			Vector3 lookTarget = new Vector3(attackLocation.x,
										transform.position.y,
										attackLocation.z);
			transform.LookAt(lookTarget);

			SetState(AIState.ATTACKING);
		}
	}

	public virtual void Stop() {
		SetState(AIState.IDLE);
	}

	public virtual bool CanAttackTarget(Entity target) {
		return equippedWeapon != null && equippedWeapon.CanHitTarget(target);
	}

	public virtual bool CanAttackLocations() {
		return equippedWeapon != null && equippedWeapon is LocationWeapon;
	}

	public virtual bool CanAutoAttack() {
		return state == AIState.IDLE;
	}

	public void OnAutoTargetAcquired(Entity target) {
		SetState(AIState.ATTACKING);
		if (LookTarget == null) {
			LookTarget = target;
		}
	}

	public void OnAutoTargetsInLocation() {
		SetState(AIState.ATTACKING);
	}

	public void OnTargetLost(Entity target) {
		if (target == AttackTarget) {
			SetState(AIState.IDLE);
		}
	}

	public virtual bool MoveToAttack(Entity target) {
		if (canMove && target == AttackTarget) {
			SetMoveDestination(FindTrueMoveDestination(target.transform.position));
			return true;
		}
		return false;
	}

	public void OnReachedAttackRange(Entity target) {
		if (target == AttackTarget) {
			StopMovement();
		}
	}

	public virtual string GetAbilityTypeIdFromInput() {
		foreach (KeyValuePair<string, Ability> abilityByTypeId in abilityMap) {
			if (Input.GetKeyDown(abilityByTypeId.Value.inputCommand.key)) {
				return abilityByTypeId.Key;
			}
		}
		return null;
	}

	public virtual void TriggerAbility(string abilityTypeId) {
		foreach (KeyValuePair<string, Ability> abilityByTypeId in abilityMap) {
			if (abilityByTypeId.Key.Equals(abilityTypeId)) {
				abilityByTypeId.Value.Activate();
			}
		}
	}

	public void VisibilityTargetDispatch(ICollection<Entity> targets) {
		if (updateVisibility) {
			if (targets != null) {
				bool isVisibleNow = targets.Contains(this);
				if (isVisibleNow != isVisible) {
					isVisible = isVisibleNow;
					OnVisibilityUpdated(isVisible);
				}
			} else {
				isVisible = false;
				OnVisibilityUpdated(false);
			}
		}
	}

	public virtual bool TakeDamage(int amount) {
		int modifiedAmount = amount;
		OnModifyDamageTaken(amount, ref modifiedAmount);
		if (modifiedAmount > 0) {
			int startingShield = currentShield;
			int resultShield = AdjustShield(-modifiedAmount);
			if (startingShield - resultShield < modifiedAmount) {
				int resultHealth = AdjustHealth(-modifiedAmount);
				if (resultHealth <= 0) {
					OnDeath(this);
					return true;
				}
				damagedTrigger = true;
			}
		}
		return false;
	}

	public virtual void Select() {
		isSelected = true;
		OnSelected();
	}

	public virtual void Deselect() {
		isSelected = false;
		OnDeselected();
	}

	public void CleanUpForDespawn() {
		if (entityHUD != null) {
			Destroy(entityHUD.gameObject);
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

	protected virtual void Update() {
		if (NetworkStatus.Instance.IsServer) {
			if (shieldRechargeTimer > 0f) {
				shieldRechargeTimer -= Time.deltaTime;
				if (shieldRechargeTimer <= 0f) {
					AdjustShield(shieldRechargeRate);
					shieldRechargeTimer = 1f;
				}
			}

			HandleAIStates();

			if (LookTarget != null) {
				//look at target's x/z position
				transform.LookAt(new Vector3(LookTarget.transform.position.x,
										   transform.position.y,
										   LookTarget.transform.position.z));
			}

			if (agent != null) {
				currentVelocity = transform.InverseTransformVector(agent.velocity);
			}
		} else {
			UpdateEffects();
		}
	}

	protected virtual void SetState(AIState newState) {
		if (newState != state) {
			if (newState == AIState.IDLE) {
				StopMovement();
				StopAttacking();
				LookTarget = null;
			} else if (newState == AIState.MOVING || newState == AIState.RETREATING) {
				StopAttacking();
				LookTarget = null;
			} else if (newState == AIState.ATTACKING) {
				StopMovement();
			}

			state = newState;
		}
	}

	protected virtual void HandleAIStates() {
		if (state == AIState.IDLE) {
			HandleIdleState();
			return;
		} else if (state == AIState.MOVING || state == AIState.RETREATING) {
			HandleMovingState();
			return;
		}
		//currently do nothing for attacking states
	}

	protected virtual void HandleIdleState() {
		//do nothing
	}

	protected virtual void HandleMovingState() {
		// Check if we've reached the destination
		if (agent.enabled && !agent.pathPending) {
			if (agent.remainingDistance <= agent.stoppingDistance) {
				if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) {
					if (state == AIState.RETREATING) {
						Vector3 posXZ = transform.position;
						posXZ.y = 0f;
						Vector3 retreatXZ = retreatDestination.transform.position;
						retreatXZ.y = 0f;
						//if close enough to despawn
						if (Vector3.Distance(posXZ, retreatXZ) <= retreatMaxDistance) {
							OnDespawn(this);
							SetState(AIState.IDLE);
						} else {
							Retreat(retreatDestination);
						}
					} else {
						SetState(AIState.IDLE);
					}
				}
			}
		}
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
		if (equippedWeapon != null) {
			equippedWeapon.Stop();
		}
		AttackTarget = null;
	}

	protected void OnApplicationQuit() {
		isQuitting = true;
	}

	protected virtual void OnDestroy() {
		VisibilityManager.VisibilityTargetDispatch -= VisibilityTargetDispatch;
	}

	protected virtual void UpdateEffects() {
		if (damagedTrigger) {
			if (animator != null && animator.enabled) {
				animator.SetTrigger("damaged");
			}

			if (audioSource != null && damagedSound != null) {
				audioSource.PlayOneShot(damagedSound);
			}

			damagedTrigger = false;
		}

		if (animator != null && animator.enabled) {
			if (canMove) {
				animator.SetFloat("velocityX", currentVelocity.x);
				animator.SetFloat("velocityY", currentVelocity.y);
				animator.SetFloat("velocityZ", currentVelocity.z);
			}
		}
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);
		
		maxHealth = e.Reader.ReadInt32();
		currentHealth = e.Reader.ReadInt32();
		maxShield = e.Reader.ReadInt32();
		currentShield = e.Reader.ReadInt32();
		shieldRechargeRate = e.Reader.ReadInt32();
		moveSpeed = e.Reader.ReadSingle();
		visionRange = e.Reader.ReadSingle();
		
		if (e.Reader.ReadBoolean()) {
			WeaponData weaponData = e.Reader.ReadSerializable<WeaponData>();
			if (this.equippedWeapon == null) {
				EquipWeapon(weaponData);
			}
			Weapon equippedWeapon = this.equippedWeapon;
			e.Reader.ReadSerializableInto<Weapon>(ref equippedWeapon);
		}

		bool loadEquipments = equipment.Count == 0;
		int numEquipments = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipments; i++) {
			EquipmentData equipmentData = e.Reader.ReadSerializable<EquipmentData>();
			if (loadEquipments) {
				equipment.Add(equipmentData);
			}
		}

		state = (AIState)e.Reader.ReadInt32();
		damagedTrigger = e.Reader.ReadBoolean();

		if (canMove) {
			currentVelocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		}

		int numAbilities = e.Reader.ReadInt32();
		for (int i = 0; i < numAbilities; i++) {
			Ability ability = abilityMap[e.Reader.ReadString()];
			e.Reader.ReadSerializableInto<Ability>(ref ability);
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);
		
		e.Writer.Write(maxHealth);
		e.Writer.Write(currentHealth);
		e.Writer.Write(maxShield);
		e.Writer.Write(currentShield);
		e.Writer.Write(shieldRechargeRate);
		e.Writer.Write(moveSpeed);
		e.Writer.Write(visionRange);
		
		e.Writer.Write(equippedWeapon != null);
		if (equippedWeapon != null) {
			e.Writer.Write(equippedWeapon.weaponData);
			e.Writer.Write(equippedWeapon);
		}

		e.Writer.Write(equipment.Count);
		foreach (EquipmentData equipment in equipment) {
			e.Writer.Write(equipment);
		}

		e.Writer.Write((int)state);
		e.Writer.Write(damagedTrigger);

		if (canMove) {
			e.Writer.Write(currentVelocity.x); e.Writer.Write(currentVelocity.y); e.Writer.Write(currentVelocity.z);
		}

		e.Writer.Write(abilityMap.Count);
		foreach (KeyValuePair<string, Ability> abilityByTypeId in abilityMap) {
			e.Writer.Write(abilityByTypeId.Key);
			e.Writer.Write(abilityByTypeId.Value);
		}

		damagedTrigger = false;
	}
}

public enum AIState {
	IDLE,
	MOVING,
	RETREATING,
	ATTACKING
}
