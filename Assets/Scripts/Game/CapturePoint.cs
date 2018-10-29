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
	public Colorable colorable;

	public CaptureState CaptureState { get; set; }
	public ushort CapturingTeamId { get; set; }
	public ushort OwningTeamId { get; set; }

	protected float captureTimer = 0f;

	protected ServerGameManager serverGameManager;
	protected ClientGameManager clientGameManager;

	private void Awake() {
		CaptureState = CaptureState.UNCAPTURED;
		CapturingTeamId = 999;
		OwningTeamId = 999;
	}

	private void Start() {
		serverGameManager = ServerGameManager.Instance;
		clientGameManager = ClientGameManager.Instance;
	}

	private void Update() {
		if (NetworkStatus.Instance.IsServer) {
			CheckUnitsInside();

			if (CaptureState == CaptureState.CAPTURING) {
				captureTimer += Time.deltaTime;
				if (captureTimer >= captureTime) {
					OwningTeamId = CapturingTeamId;
					CaptureState = CaptureState.CAPTURED;
					Debug.Log(name + " captured by team " + OwningTeamId + "!");
					InvokeRepeating("UpdateGain", gainFrequency, gainFrequency);
				}
			}
		}
	}

	private void UpdateGain() {
		if (gainType == GainType.SCORE) {
			serverGameManager.IncreaseScore((ushort)OwningTeamId, gainAmount);
		} else if (gainType == GainType.RESOURCE) {
			serverGameManager.IncreaseResources((ushort)OwningTeamId, gainAmount);
		}
	}

	private void CheckUnitsInside() {
		ushort? teamIdInside = null;
		foreach (Collider collider in Physics.OverlapSphere(transform.position, captureRadius, LayerManager.Instance.unitMask)) {
			Entity entity = collider.GetComponentInParent<Entity>();
			if (entity != null) {
				if (teamIdInside == null) {
					teamIdInside = entity.TeamId;
				} else if (teamIdInside != entity.TeamId) {
					//more than one team inside, mark as contested and stop scoring
					CaptureState = CaptureState.CONTESTED;
					CancelInvoke("UpdateGain");
					return;
				}
			}
		}

		if (teamIdInside != null) {
			//only one team is inside, handle multiple potential scenarios
			if (CaptureState == CaptureState.CONTESTED) {
				if (teamIdInside == OwningTeamId) {
					//point was owned before it was contested, so return to captured and resume scoring
					CaptureState = CaptureState.CAPTURED;
					InvokeRepeating("UpdateGain", gainFrequency, gainFrequency);
				} else if (teamIdInside == CapturingTeamId) {
					//team was capturing before it was contested, so continue without resetting timer
					CaptureState = CaptureState.CAPTURING;
				} else {
					//new team is now capturing after contested state
					CapturingTeamId = (ushort)teamIdInside;
					captureTimer = 0f;
					CaptureState = CaptureState.CAPTURING;
					CancelInvoke("UpdateGain");
				}
			} else if (CaptureState == CaptureState.UNCAPTURED
					|| (CaptureState == CaptureState.CAPTURING && teamIdInside != CapturingTeamId)
					|| ((CaptureState == CaptureState.CAPTURED || CaptureState == CaptureState.CONTESTED) && teamIdInside != OwningTeamId)) {
				CapturingTeamId = (ushort)teamIdInside;
				captureTimer = 0f;
				CaptureState = CaptureState.CAPTURING;
				CancelInvoke("UpdateGain");
			}
		} else {
			//nobody is inside
			CapturingTeamId = 999;
			captureTimer = 0f;
		}
	}

	public void Deserialize(DeserializeEvent e) {
		ushort originalOwningTeamId = OwningTeamId;
		CaptureState = (CaptureState)e.Reader.ReadInt32();
		CapturingTeamId = e.Reader.ReadUInt16();
		OwningTeamId = e.Reader.ReadUInt16();

		if (colorable != null && OwningTeamId != originalOwningTeamId) {
			colorable.SetColor(clientGameManager.GameState.Teams[OwningTeamId].Color);
		}
	}

	public void Serialize(SerializeEvent e) {
		e.Writer.Write((int)CaptureState);
		e.Writer.Write(CapturingTeamId);
		e.Writer.Write(OwningTeamId);
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
