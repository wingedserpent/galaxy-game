using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
	
	public float speed = 1f;
	public Transform target;
	public Vector3 TargetPos { get; set; }

	private void Start() {
		if (target != null) {
			LeanTween.move(gameObject, target.position, 10f).setSpeed(speed);
		} else {
			LeanTween.move(gameObject, TargetPos, 10f).setSpeed(speed);
		}
	}
}
