using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityToggle : MonoBehaviour {
	
	public EntityController EntityController { get; set; }

	private void Awake() {
		EntityController = GetComponentInParent<EntityController>();
	}

	private void Start() {
		EntityController.OnVisibilityUpdated += OnVisibilityUpdated;
	}

	private void OnDestroy() {
		if (EntityController != null) {
			EntityController.OnVisibilityUpdated -= OnVisibilityUpdated;
		}
	}

	private void OnVisibilityUpdated(bool isVisible) {
		gameObject.SetActive(isVisible);
	}
}
