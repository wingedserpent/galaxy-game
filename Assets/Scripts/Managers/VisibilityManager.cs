using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public delegate void VisibilityTargetDispatch(ICollection<Entity> newTargets);

public class VisibilityManager : Singleton<VisibilityManager> {

	public LayerMask visionMask;
	public LayerMask obstacleMask;

	private HashSet<Entity> allVisibleTargets = new HashSet<Entity>();

	public static event VisibilityTargetDispatch VisibilityTargetDispatch = delegate { };

	void Start() {
		if (NetworkStatus.Instance.IsClient) {
			StartCoroutine("DispatchTargetsWithDelay", .2f);
		}
	}

	public void AddVisibleTargets(List<Entity> targets) {
		foreach (Entity target in targets) {
			allVisibleTargets.Add(target);
		}
	}

	IEnumerator DispatchTargetsWithDelay(float delay) {
		while (true) {
			yield return new WaitForSeconds(delay);
			VisibilityTargetDispatch(allVisibleTargets);
			allVisibleTargets.Clear();
		}
	}
}
