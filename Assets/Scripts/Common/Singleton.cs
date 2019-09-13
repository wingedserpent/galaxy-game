using UnityEngine;
using System.Collections;

public class Singleton<Clazz> : MonoBehaviour where Clazz : Singleton<Clazz> {

	public static Clazz Instance;
	public bool persistent;

	private bool isDuplicate = false;
	private bool isQuitting = false;

	protected virtual void Awake() {
		if (!isQuitting) {
			if (persistent) {
				if (!Instance) {
					Instance = this as Clazz;
				} else {
					isDuplicate = true; //mark as duplicate so we don't assume destruction means we're quitting
					Destroy(gameObject);
				}
				DontDestroyOnLoad(gameObject);
			} else {
				Instance = this as Clazz;
			}
		}
	}

	private void OnDestroy() {
		if (!isDuplicate) {
			//primary instance was destroyed, we must be quitting
			isQuitting = true;
		}
	}
}