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
	
	public AIState State { get; set; }
	public bool DamagedTrigger { get; set; }
	public bool UpdateVisibility { get; set; }
	public bool IsVisible { get; private set; }
	public EntityHUD EntityHUD { get; private set; }

	public static event OnDeath OnDeath = delegate { };

	public event OnVisibilityUpdated OnVisibilityUpdated = delegate { };

	protected bool isQuitting = false;

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
		DamagedTrigger = false;
		UpdateVisibility = true;
		IsVisible = true;
	}

	protected virtual void Start() {
		clientEntityManager = ClientEntityManager.Instance;
		
		VisibilityManager.VisibilityTargetDispatch += VisibilityTargetDispatch;

		if (NetworkStatus.Instance.IsClient && unitHUDPrefab != null) {
			EntityHUD = Instantiate<GameObject>(unitHUDPrefab.gameObject, FindObjectOfType<Canvas>().transform).GetComponent<EntityHUD>();
			EntityHUD.Entity = entity;
		}
	}

	protected virtual void Update() {
		if (NetworkStatus.Instance.IsServer) {
			HandleAIStates();
		} else {
			UpdateEffects();
		}
	}

	protected virtual void HandleAIStates() {
		if (State == AIState.IDLE) {
			HandleIdleState();
			return;
		}
	}

	protected virtual void HandleIdleState() {
		//do nothing
	}

	public virtual void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		//do nothing
	}

	public virtual void Stop() {
		State = AIState.IDLE;
	}

	public virtual void Attack(Entity attackTarget) {
		//do nothing
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
				IsVisible = false;
				OnVisibilityUpdated(false);
			}
		}
	}

	protected virtual void UpdateEffects() {
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
		DamagedTrigger = e.Reader.ReadBoolean();
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write((int)State);
		e.Writer.Write(DamagedTrigger);
		
		DamagedTrigger = false;
	}
}

public enum AIState {
	IDLE,
	MOVING,
	ATTACKING
}
