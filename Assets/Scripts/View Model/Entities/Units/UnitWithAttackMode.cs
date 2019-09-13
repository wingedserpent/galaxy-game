using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public class UnitWithAttackMode : Unit {
	
	public InputCommand toggleModeInput;
	public float toggleModeDuration;
	
	private bool isInAttackMode = false;
	private bool isTogglingMode = false;
	private float toggleModeTimer = 0f;
	private bool toggleModeTrigger = false;
	private Vector3? savedAttackLocation = null;

	private const string TOGGLE_MODE_ABILITY_TYPE_ID = "toggleMode";

	protected override void Awake() {
		base.Awake();

		availableCommands.Add(toggleModeInput);
	}

	public override void Move(Vector3 target, Vector3? groupMovementCenter = null) {
		if (!isInAttackMode && !isTogglingMode) {
			base.Move(target, groupMovementCenter);
		}
	}

	public override void Retreat(TeamSpawn myTeamSpawn) {
		if (!isInAttackMode && !isTogglingMode) {
			base.Retreat(myTeamSpawn);
		}
	}

	public override void Attack(Vector3 attackLocation) {
		if (!isTogglingMode) {
			if (!isInAttackMode) {
				BeginModeToggle(attackLocation);
			} else {
				base.Attack(attackLocation);
			}
		}
	}

	public override void Stop() {
		if (!isTogglingMode) {
			base.Stop();
		}
	}

	public override bool CanAutoAttack() {
		return isInAttackMode && !isTogglingMode && base.CanAutoAttack();
	}

	public override bool MoveToAttack(Entity target) {
		return !isInAttackMode && !isTogglingMode && base.MoveToAttack(target);
	}

	public override string GetAbilityTypeIdFromInput() {
		if (Input.GetKeyDown(toggleModeInput.key)) {
			return TOGGLE_MODE_ABILITY_TYPE_ID;
		}

		return base.GetAbilityTypeIdFromInput();
	}

	public override void TriggerAbility(string abilityTypeId) {
		if (TOGGLE_MODE_ABILITY_TYPE_ID.Equals(abilityTypeId)) {
			BeginModeToggle(null);
		} else {
			base.TriggerAbility(abilityTypeId);
		}
	}

	protected override void Update() {
		if (isTogglingMode) {
			toggleModeTimer -= Time.deltaTime;
			if (toggleModeTimer <= 0f) {
				CompleteModeToggle();
			}
		}

		base.Update();
	}

	protected override void HandleIdleState() {
		if (isInAttackMode) {
			base.HandleIdleState();
		}
	}

	protected void BeginModeToggle(Vector3? attackLocation) {
		if (!isTogglingMode) {
			isTogglingMode = true;
			toggleModeTrigger = true;
			toggleModeTimer = toggleModeDuration;
			savedAttackLocation = attackLocation;

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

			if (isInAttackMode) {
				//TODO if no AttackLocation, calculate one automatically so we always go into attacking state
				if (savedAttackLocation.HasValue) {
					Attack(savedAttackLocation.Value);
					savedAttackLocation = null;
				} else {
					SetState(AIState.IDLE);
				}
			} else {
				SetState(AIState.IDLE);
			}
		}
	}

	protected override void UpdateEffects() {
		base.UpdateEffects();

		if (animator != null && animator.enabled) {
			animator.SetBool("isInAttackMode", isInAttackMode);
		}
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);

		bool remoteIsInAttackMode = e.Reader.ReadBoolean();
		toggleModeTrigger = e.Reader.ReadBoolean();

		if (toggleModeTrigger && remoteIsInAttackMode == isInAttackMode) {
			//toggle mode was triggered and we agree with server on current mode, so begin toggle
			BeginModeToggle(null);
		}
	}

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);

		e.Writer.Write(isInAttackMode);
		e.Writer.Write(toggleModeTrigger);

		toggleModeTrigger = false;
	}
}
