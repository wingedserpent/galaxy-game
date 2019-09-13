using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePoint : MonoBehaviour, IDarkRiftSerializable {

	public ushort ID;
	public float captureTime;
	public float captureRadius;
	public GainType gainType;
	public int gainAmount;
	public float gainFrequency;
	public Colorable outlineColorable;
	public Colorable fillColorable;
	public PercentageRadius percentageRadius;

	public CaptureState captureState { get; set; }
	public ushort capturingTeamId { get; set; }
	public ushort owningTeamId { get; set; }

	protected float captureTimer = 0f;

	protected ServerGameManager serverGameManager;
	protected ClientGameManager clientGameManager;

	private void Awake() {
		captureState = CaptureState.UNCAPTURED;
		capturingTeamId = 999;
		owningTeamId = 999;
	}

	private void Start() {
		serverGameManager = ServerGameManager.Instance;
		clientGameManager = ClientGameManager.Instance;
	}

	private void Update() {
		if (NetworkStatus.Instance.IsServer) {
			CheckUnitsInside();

			if (captureState == CaptureState.CAPTURING) {
				captureTimer += Time.deltaTime;

				if (captureTimer >= captureTime) {
					owningTeamId = capturingTeamId;
					captureState = CaptureState.CAPTURED;
					Debug.Log(name + " captured by team " + owningTeamId + "!");
					InvokeRepeating("UpdateGain", gainFrequency, gainFrequency);
				}
			}
		}
	}

	private void UpdateGain() {
		if (gainType == GainType.SCORE) {
			serverGameManager.IncreaseScore((ushort)owningTeamId, gainAmount);
		} else if (gainType == GainType.RESOURCE) {
			serverGameManager.IncreaseResources((ushort)owningTeamId, gainAmount);
		}
	}

	private void CheckUnitsInside() {
		ushort? teamIdInside = null;
		foreach (Collider collider in Physics.OverlapSphere(transform.position, captureRadius, LayerManager.Instance.unitMask)) {
			Entity entity = collider.GetComponentInParent<Entity>();
			if (entity != null) {
				if (teamIdInside == null) {
					teamIdInside = entity.teamId;
				} else if (teamIdInside != entity.teamId) {
					//more than one team inside, mark as contested and stop scoring
					captureState = CaptureState.CONTESTED;
					CancelInvoke("UpdateGain");
					return;
				}
			}
		}

		if (teamIdInside != null) {
			//only one team is inside, handle multiple potential scenarios
			if (captureState == CaptureState.CONTESTED) {
				if (teamIdInside == owningTeamId) {
					//point was owned before it was contested, so return to captured and resume scoring
					captureState = CaptureState.CAPTURED;
					InvokeRepeating("UpdateGain", gainFrequency, gainFrequency);
				} else if (teamIdInside == capturingTeamId) {
					//team was capturing before it was contested, so continue without resetting timer
					captureState = CaptureState.CAPTURING;
				} else {
					//new team is now capturing after contested state
					capturingTeamId = (ushort)teamIdInside;
					captureTimer = 0f;
					captureState = CaptureState.CAPTURING;
					CancelInvoke("UpdateGain");
				}
			} else if (captureState == CaptureState.UNCAPTURED
					|| (captureState == CaptureState.CAPTURING && teamIdInside != capturingTeamId)
					|| ((captureState == CaptureState.CAPTURED || captureState == CaptureState.CONTESTED) && teamIdInside != owningTeamId)) {
				capturingTeamId = (ushort)teamIdInside;
				captureTimer = 0f;
				captureState = CaptureState.CAPTURING;
				CancelInvoke("UpdateGain");
			}
		} else {
			//nobody is inside
			capturingTeamId = 999;
		}
	}

	public void Deserialize(DeserializeEvent e) {
		CaptureState oldCaptureState = captureState;
		float oldCaptureTimer = captureTimer;

		captureState = (CaptureState)e.Reader.ReadInt32();
		capturingTeamId = e.Reader.ReadUInt16();
		owningTeamId = e.Reader.ReadUInt16();
		captureTimer = e.Reader.ReadSingle();

		if (captureTimer != oldCaptureTimer) {
			percentageRadius.SetDisplayAmounts(captureTimer, captureTime);
		}

		if (captureState != oldCaptureState) {
			if (captureState == CaptureState.CAPTURED) {
				outlineColorable.SetColor(clientGameManager.GameState.teams[owningTeamId].color);
				fillColorable.SetColor(clientGameManager.GameState.teams[owningTeamId].color);
			} else if (captureState == CaptureState.CAPTURING) {
				fillColorable.SetColor(clientGameManager.GameState.teams[capturingTeamId].color);
			}
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)captureState);
		e.Writer.Write(capturingTeamId);
		e.Writer.Write(owningTeamId);
		e.Writer.Write(captureTimer);
	}
}

public enum GainType {
	SCORE,
	RESOURCE
}

public enum CaptureState {
	UNCAPTURED,
	CAPTURING,
	CONTESTED,
	CAPTURED
}
