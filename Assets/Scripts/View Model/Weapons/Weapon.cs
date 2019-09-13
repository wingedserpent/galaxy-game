using DarkRift;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Weapon : MonoBehaviour, IDarkRiftSerializable {

	public string typeId;
	public bool canAttackGround = false;
	public bool canAttackAir = false;
	public GameObject projectilePrefab;
	public List<Transform> projectileSpawnPoints = new List<Transform>();
	public List<GameObject> existingProjectiles = new List<GameObject>();
	public float attackEffectTime = 0f;
	public AudioClip attackSound;

	public WeaponData weaponData { get; set; }

	public WeaponState state { get; protected set; }
	protected float attackCooldown { get; set; }
	protected bool isAttackingAndInRange { get; set; }
	protected bool isDelayingAttack { get; set; }

	protected bool attackStartTrigger { get; set; } //e.g. start a beam
	protected bool attackEffectTrigger { get; set; } //e.g. launch a missile
	protected bool attackStopTrigger { get; set; } //e.g. stop a beam

	public Entity entity { get; private set; }

	protected virtual void Awake() {
		entity = GetComponentInParent<Entity>();

		attackCooldown = 0f;

		enabled = false;
	}

	protected void OnEnable() {
		entity.OnSelected += OnEntitySelected;
	}

	protected void OnDisable() {
		entity.OnDeselected -= OnEntityDeselected;
	}

	public virtual bool Attack(object target) {
		//intended to be overridden
		return false;
	}

	public void Stop() {
		SetState(WeaponState.IDLE);
	}

	public bool CanHitTarget(Entity target) {
		return target != null && target.teamId != entity.teamId && (target.isInAir ? canAttackAir : canAttackGround);
	}

	protected virtual void Update() {
		if (NetworkStatus.Instance.IsServer) {
			if (attackCooldown > 0f) {
				attackCooldown -= Time.deltaTime;
			}

			HandleAIStates();
		} else {
			UpdateEffects();
		}
	}

	protected virtual void SetState(WeaponState newState) {
		if (newState != state) {
			if (newState == WeaponState.IDLE) {
				PauseAttacking();
				ClearTargets();
			}

			state = newState;
		}
	}

	protected void HandleAIStates() {
		if (state == WeaponState.IDLE) {
			HandleIdleState();
			return;
		} else if (state == WeaponState.ATTACKING_COMMAND || state == WeaponState.ATTACKING_AUTO) {
			HandleCombatState();
			return;
		}
	}

	protected virtual void HandleIdleState() {
		//does nothing
	}

	protected virtual void HandleCombatState() {
		//does nothing
	}

	protected virtual void ClearTargets() { }

	protected virtual void StartAttacking() {
		isAttackingAndInRange = true;
		attackStartTrigger = true;
	}

	protected virtual void PauseAttacking() {
		isAttackingAndInRange = false;
		attackStopTrigger = true;
	}

	protected virtual bool DealDamage(Entity target) {
		return target.TakeDamage(weaponData.damage);
	}

	protected void UpdateEffects() {
		if (entity.animator != null && entity.animator.enabled) {
			entity.animator.SetBool("isAttacking", state != WeaponState.IDLE);
		}

		if (attackStartTrigger) {
			HandleAttackStartTrigger();
		}

		if (attackEffectTrigger) {
			HandleAttackEffectTrigger();
		}

		if (attackStopTrigger) {
			HandleAttackStopTrigger();
		}
	}

	protected virtual void HandleAttackStartTrigger() {
		attackStartTrigger = false;
	}

	protected virtual void HandleAttackEffectTrigger() {
		if (entity.animator != null && entity.animator.enabled) {
			entity.animator.SetTrigger("attack");
		}

		if (entity.audioSource != null) {
			entity.audioSource.PlayOneShot(attackSound);
		}

		attackEffectTrigger = false;
	}

	protected virtual void HandleAttackStopTrigger() {
		attackStopTrigger = false;
	}

	protected virtual void OnEntitySelected() { }

	protected virtual void OnEntityDeselected() { }

	public virtual void Deserialize(DeserializeEvent e) {
		attackStartTrigger = e.Reader.ReadBoolean();
		attackEffectTrigger = e.Reader.ReadBoolean();
		attackStopTrigger = e.Reader.ReadBoolean();
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(attackStartTrigger);
		e.Writer.Write(attackEffectTrigger);
		e.Writer.Write(attackStopTrigger);

		attackStartTrigger = false;
		attackEffectTrigger = false;
		attackStopTrigger = false;
	}
}

public enum WeaponState {
	IDLE,
	ATTACKING_COMMAND,
	ATTACKING_AUTO
}