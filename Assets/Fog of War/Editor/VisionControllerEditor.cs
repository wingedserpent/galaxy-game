using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (Vision))]
public class VisionControllerEditor : Editor {

	void OnSceneGUI() {
		Vision fow = (Vision)target;
		Handles.color = Color.white;
		Handles.DrawWireArc (fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadius);
		Vector3 viewAngleA = fow.DirFromAngle (-fow.viewAngle / 2, false);
		Vector3 viewAngleB = fow.DirFromAngle (fow.viewAngle / 2, false);

		Handles.DrawLine (fow.transform.position, fow.transform.position + viewAngleA * fow.viewRadius);
		Handles.DrawLine (fow.transform.position, fow.transform.position + viewAngleB * fow.viewRadius);

		Handles.color = Color.red;
		foreach (Entity visibleTarget in fow.VisibleTargets) {
			Handles.DrawLine (fow.transform.position, visibleTarget.transform.position);
		}
	}

}
