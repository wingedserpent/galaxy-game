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

	public EntityHUD unitHUDPrefab;

	public AIState State { get; set; }
	public Entity AttackTarget { get; set; }
	public bool AttackTrigger { get; set; }
	public bool DamagedTrigger { get; set; }
	public bool UpdateVisibility { get; set; }
	public bool IsVisible { get; private set; }
	public EntityHUD EntityHUD { get; private set; }

	public static event OnDeath OnDeath = delegate { };

	public event OnVisibilityUpdated OnVisibilityUpdated = delegate { };

	protected float attackCooldown = 0f;
	protected bool isQuitting = false;
	protected bool isRespondingToAttackCommand = false;

	protected Entity entity;
	protected Vision vision;
	protected Animator animator;
	protected AudioSource audioSource;
	protected ClientEntityManager clientEntityManager;

	protected virtual void Awake() {
		entity = GetComponent<Entity>();
		vision = GetComponentInChildren<Vision>();
		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();

		State = AIState.IDLE;
		AttackTrigger = false;
		DamagedTrigger = false;
		UpdateVisibility = true;
		IsVisible = true;
	}

	protected void Start() {
		clientEntityManager = ClientEntityManager.Instance;
		
		VisibilityManager.VisibilityTargetDispatch += VisibilityTargetDispatch;

		if (NetworkStatus.Instance.IsClient && unitHUDPrefab != null) {
			EntityHUD = Instantiate<GameObject>(unitHUDPrefab.gameObject, FindObjectOfType<Canvas>().transform).GetComponent<EntityHUD>();
			EntityHUD.Entity = entity;
		}
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

	protected virtual void HandleAIStates() {
		if (State == AIState.IDLE) {
			HandleIdleState();
			return;
		} else if (State == AIState.ATTACKING) {
			HandleCombatState();
			return;
		}
	}

	protected void HandleIdleState() {
		isRespondingToAttackCommand = false;

		AttackTarget = CheckForTargets();
		if (AttackTarget != null) {
			State = AIState.ATTACKING;
		}
	}

	protected Entity CheckForTargets() {
		return (from target in vision.VisibleTargets.Where(x => x != null)
				let distance = Vector3.Distance(target.transform.position, entity.transform.position)
				where target.TeamId != entity.TeamId && distance <= entity.attackRange
				orderby distance descending
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

			if (Vector3.Distance(AttackTarget.transform.position, entity.transform.position) > entity.attackRange) {
				//out of range, give up
				AttackTarget = null;
				State = AIState.IDLE;
			} else {
				if (attackCooldown <= 0f) {
					//do the attack
					bool targetDied = AttackTarget.EntityController.TakeDamage(entity.attackDamage);
					if (targetDied) {
						AttackTarget = null;
						State = AIState.IDLE;
					}
					attackCooldown = entity.attackSpeed;
					AttackTrigger = true;
				}
			}
		}
	}

	public virtual void Stop() {
		AttackTarget = null;
		State = AIState.IDLE;
	}

	public virtual void Attack(Entity attackTarget) {
		if (attackTarget != null) {
			isRespondingToAttackCommand = true;
			AttackTarget = attackTarget;
			State = AIState.ATTACKING;
		}
	}

	public virtual bool TakeDamage(int amount) {
		int resultHealth = entity.AdjustHealth(-amount);
		if (resultHealth <= 0) {
			OnDeath(entity);
			return true;
		}
		DamagedTrigger = true;
		return false;
	}

	public void Die() {
		if (EntityHUD != null) {
			Destroy(EntityHUD.gameObject);
		}
		if (animator != null) {
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
				OnVisibilityUpdated(false);
			}
		}
	}

	protected virtual void UpdateEffects() {
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
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write((int)State);
		e.Writer.Write(AttackTrigger);
		e.Writer.Write(DamagedTrigger);
		e.Writer.Write(AttackTarget != null ? AttackTarget.ID : "");

		AttackTrigger = false;
		DamagedTrigger = false;
	}
}

public enum AIState {
	IDLE,
	MOVING,
	ATTACKING
}
