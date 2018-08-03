using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tweener : MonoBehaviour {
	
	public float speed = 0f;
	public float time = 0f;
	public Transform target;
	public Vector3 TargetPos { get; set; }
	public LeanTweenType easeType = LeanTweenType.notUsed;
	public bool resetOnComplete = false;
	public bool destroyOnComplete = false;

	private bool isStarted = false;
	private Vector3 startLocalPos;

	private void Start() {
		isStarted = true;
		Init();
	}

	private void OnEnable() {
		//onEnable normally runs before start, but start must run first because target may not yet be assigned
		if (isStarted) {
			Init();
		}
	}

	private void Init() {
		startLocalPos = transform.localPosition;

		if (target != null) {
			TargetPos = target.position;
		}

		LTDescr ltdescr = LeanTween.move(gameObject, TargetPos, time).setEase(easeType);

		if (speed > 0f) {
			ltdescr.setSpeed(speed);
		}

		ltdescr.setDestroyOnComplete(destroyOnComplete);
		ltdescr.setOnComplete(OnComplete);
	}

	private void OnComplete() {
		if (resetOnComplete) {
			transform.localPosition = startLocalPos;
			enabled = false;
		}
	}
}
