using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public class TrebuchetController : UnitController {

	public KeyCode toggleModeKey;
	public float toggleModeDuration;

	protected bool isInAttackMode = false;
	private bool isTogglingMode = false;
	private float toggleModeTimer = 0f;

	private Trebuchet trebuchet;

	protected override void Awake() {
		base.Awake();

		trebuchet = GetComponent<Trebuchet>();
	}

	protected override void Update() {
		if (isTogglingMode) {
			toggleModeTimer -= Time.deltaTime;
			if (toggleModeTimer <= 0f) {
				CompleteModeToggle();
			}
		}

		base.Update();

		if (isInAttackMode && IsSelected) {
			Debug.DrawLine(transform.position, AttackLocation, Color.red);
		}
	}

	protected override void HandleIdleState() {
		if (isInAttackMode) {
			base.HandleIdleState();
		}
	}

	protected override List<Entity> GetTargetsAtAttackLocation(Vector3 attackLocation) {
		if (!isInAttackMode) {
			return new List<Entity>();
		}

		return base.GetTargetsAtAttackLocation(attackLocation);
	}

	protected override void HandleCombatState() {
		if (isInAttackMode) {
			base.HandleCombatState();
		}
	}

	protected override void DoLocationAttack(Vector3 attackLocation) {
		if (isInAttackMode) {
			base.DoLocationAttack(attackLocation);
		}
	}

	public override void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		if (!isInAttackMode && !isTogglingMode) {
			base.Move(target, groupMovementCenter);
		}
	}

	public override void Stop() {
		if (!isTogglingMode) {
			base.Stop();
		}
	}

	public override void Attack(Vector3 attackLocation) {
		if (!isTogglingMode) {
			isRespondingToAttackCommand = true;
			AttackLocation = Vector3.MoveTowards(transform.position, attackLocation, entity.Weapon.Range);

			//look at target's x/z position
			Vector3 lookTarget = new Vector3(AttackLocation.x,
									   transform.position.y,
									   AttackLocation.z);
			transform.LookAt(lookTarget);

			if (!isInAttackMode) {
				BeginModeToggle();
			}
		}
	}

	public override CommandType GetCommandTypeFromInput() {
		if (Input.GetKeyDown(toggleModeKey)) {
			return CommandType.TOGGLE_MODE;
		}
		return CommandType.NONE;
	}

	public override void ExecuteCommand(CommandType commandType) {
		if (commandType == CommandType.TOGGLE_MODE && !isTogglingMode && isInAttackMode) {
			BeginModeToggle();
		}
	}

	protected void BeginModeToggle() {
		if (!isTogglingMode) {
			isTogglingMode = true;
			toggleModeTimer = toggleModeDuration;

			SetState(AIState.IDLE);

			if (animator != null && animator.enabled) {
				animator.SetTrigger("toggleMode");
			}
		}
	}

	protected void CompleteModeToggle() {
		if (isTogglingMode) {
			isTogglingMode = false;
			isInAttackMode = !isInAttackMode;

			SetState(AIState.IDLE);

			if (isInAttackMode) {
				if (IsSelected) {
					CreateTargetedAreaMarker();
				}
				//TODO if no AttackLocation, find one
			} else {
				DestroyTargetedAreaMarker();
			}
		}
	}

	public override void OnSelected() {
		base.OnSelected();

		if (isInAttackMode) {
			CreateTargetedAreaMarker();
		}
	}

	protected override void UpdateEffects() {
		base.UpdateEffects();

		if (animator != null && animator.enabled) {
			animator.SetBool("isInAttackMode", isInAttackMode);
		}
	}

	protected override void OnDestroy() {
		base.OnDestroy();

		DestroyTargetedAreaMarker();
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		bool isNowInAttackMode = e.Reader.ReadBoolean();
		bool isNowTogglingMode = e.Reader.ReadBoolean();

		if (!isTogglingMode) {
			if (isNowInAttackMode != isInAttackMode || isNowTogglingMode) {
				BeginModeToggle();
			}
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(isInAttackMode);
		e.Writer.Write(isTogglingMode);
	}
}
