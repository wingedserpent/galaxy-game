using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBeam : MonoBehaviour {

	public Transform start;
	public Transform target;
	public float totalEffectTime;
	public List<WeaponBeamStage> stages;

	private LineRenderer lineRenderer;
	private float elapsedTime = 0f;
	private int currentStageIndex = 0;

	private void Awake() {
		lineRenderer = GetComponent<LineRenderer>();
	}

	private void Start() {
		lineRenderer.positionCount = 3;
	}

	private void Update() {
		if (start == null || target == null) {
			Destroy(gameObject);
		} else {
			elapsedTime += Time.deltaTime;

			lineRenderer.SetPosition(0, start.position);
			lineRenderer.SetPosition(1, (target.position + start.position) / 2);
			lineRenderer.SetPosition(2, target.position);

			if (currentStageIndex < stages.Count && (elapsedTime / totalEffectTime) >= stages[currentStageIndex].percentage) {
				lineRenderer.widthMultiplier = stages[currentStageIndex].widthMultiplier;
				currentStageIndex++;
			}
		}
	}
}
