using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tweener : MonoBehaviour {
	
	public float speed = 0f;
	public float time = 0f;
	public float midPointHeightDelta = 0f;
	public bool orientToPath = false;
	public AnimationCurve easingCurve;
	public Transform target;
	public Vector3 TargetPos { get; set; }
	public LeanTweenType easeType = LeanTweenType.notUsed;
	public bool resetOnComplete = false;
	public bool destroyOnComplete = false;
	public GameObject spawnOnComplete;

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

		LTDescr ltdescr;
		if (midPointHeightDelta > 0f) {
			List<Vector3> TargetPath = new List<Vector3>();
			TargetPath.Add(transform.position); //first element indicates initial angle
			TargetPath.Add(transform.position); //first element indicates initial angle
			TargetPath.Add(((TargetPos + transform.position) / 2) + new Vector3(0f, midPointHeightDelta, 0f));
			TargetPath.Add(TargetPos);
			TargetPath.Add(TargetPos); //must be added twice since last element only indicates final angle
			ltdescr = LeanTween.moveSpline(gameObject, TargetPath.ToArray(), time);
		} else {
			ltdescr = LeanTween.move(gameObject, TargetPos, time);
		}

		if (easeType == LeanTweenType.animationCurve) {
			ltdescr.setEase(easingCurve);
			ltdescr.setOrientToPath(orientToPath);
		} else {
			ltdescr.setEase(easeType);
		}

		if (speed > 0f) {
			ltdescr.setSpeed(speed);
		}

		ltdescr.setDestroyOnComplete(destroyOnComplete);
		ltdescr.setOnComplete(OnComplete);
	}

	private void OnComplete() {
		if (spawnOnComplete != null) {
			Instantiate<GameObject>(spawnOnComplete, transform.position, Quaternion.identity);
		}
		if (resetOnComplete) {
			transform.localPosition = startLocalPos;
			enabled = false;
		}
	}
}
