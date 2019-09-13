using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityToggle : MonoBehaviour {
	
	public Entity Entity { get; set; }

	private void Awake() {
		Entity = GetComponentInParent<Entity>();
	}

	private void Start() {
		Entity.OnVisibilityUpdated += OnVisibilityUpdated;
		OnVisibilityUpdated(Entity.isVisible); //make one immediate call to ensure my vis is set properly
	}

	private void OnDestroy() {
		if (Entity != null) {
			Entity.OnVisibilityUpdated -= OnVisibilityUpdated;
		}
	}

	private void OnVisibilityUpdated(bool isVisible) {
		gameObject.SetActive(isVisible);
	}
}
