﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMinimapOutline : MonoBehaviour {

	public Transform topLine;
	public Transform bottomLine;
	public Transform leftLine;
	public Transform rightLine;
	public float lineWidth = 1f;
	public LayerMask groundMask = -1; //same mask as RTSCamera, move into something shared
	
	private Camera cam;
	private Quaternion leftLineRotOffset;
	private Quaternion rightLineRotOffset;

	void Start () {
		cam = Camera.main;
		leftLineRotOffset = leftLine.rotation;
		rightLineRotOffset = rightLine.rotation;
	}
	
	void Update () {
		Vector3 topLeftPos = GetGroundPos(cam.ViewportPointToRay(new Vector3(0, 1, 0)));
		Vector3 topRightPos = GetGroundPos(cam.ViewportPointToRay(new Vector3(1, 1, 0)));
		Vector3 bottomRightPos = GetGroundPos(cam.ViewportPointToRay(new Vector3(1, 0, 0)));
		Vector3 bottomLeftPos = GetGroundPos(cam.ViewportPointToRay(new Vector3(0, 0, 0)));

		topLine.localScale = new Vector3(topRightPos.x - topLeftPos.x, lineWidth, lineWidth);
		topLine.position = (topRightPos + topLeftPos) / 2;

		bottomLine.localScale = new Vector3(bottomRightPos.x - bottomLeftPos.x, lineWidth, lineWidth);
		bottomLine.position = (bottomRightPos + bottomLeftPos) / 2;

		leftLine.localScale = new Vector3(lineWidth, topLeftPos.z - bottomLeftPos.z, lineWidth);
		leftLine.position = (topLeftPos + bottomLeftPos) / 2;
		leftLine.LookAt(topLeftPos);
		leftLine.rotation *= leftLineRotOffset;

		rightLine.localScale = new Vector3(lineWidth, topRightPos.z - bottomRightPos.z, lineWidth);
		rightLine.position = (topRightPos + bottomRightPos) / 2;
		rightLine.LookAt(topRightPos);
		rightLine.rotation *= rightLineRotOffset;
	}

	private Vector3 GetGroundPos(Ray ray) {
		/* using the ground makes weird things happen, maybe we can just skip it?
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundMask.value)) {
			return hit.point;
		}
		*/

		//no ground here, simulate a plane at y=0
		Plane groundPlane = new Plane(Vector3.up, 0);
		float intersection = 0f;
		if (groundPlane.Raycast(ray, out intersection)) {
			return ray.GetPoint(intersection);
		}

		return Vector3.zero;
	}
}
