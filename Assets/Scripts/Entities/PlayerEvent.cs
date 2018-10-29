using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;

public delegate void OnEventEnd(PlayerEvent playerEvent);

public class PlayerEvent : OwnedObject {
	
	public GameObject targetingPrefab;
	public int resourceCost;
	public float radius;
	public float duration;

	public static event OnEventEnd OnEventEnd = delegate { };

	private float durationLeft;

	protected override void Awake() {
		base.Awake();

		durationLeft = duration;
	}

	protected virtual void Update() {
		if (durationLeft > 0f) {
			durationLeft -= Time.deltaTime;
		} else {
			OnEventEnd(this);
		}
	}

	public void End() {
		float maxLifetime = 0f;

		//disable any particle systems and also find the maximum lifetime of any particle in this event
		foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>()) {
			maxLifetime = Mathf.Max(particleSystem.main.startLifetime.constant, maxLifetime);
			EmissionModule emission = particleSystem.emission;
			emission.enabled = false;
		}

		Destroy(gameObject, maxLifetime);
	}
}
