using UnityEngine;
using System.Collections;

public class Singleton<Clazz> : MonoBehaviour where Clazz : Singleton<Clazz> {
	public static Clazz Instance;
	public bool persistent;

	protected virtual void Awake() {
		if (persistent) {
			if (!Instance) {
				Instance = this as Clazz;
			} else {
				Destroy(gameObject);
			}
			DontDestroyOnLoad(gameObject);
		} else {
			Instance = this as Clazz;
		}
	}
}