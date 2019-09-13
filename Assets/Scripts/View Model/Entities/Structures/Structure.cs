using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Structure : Entity {

	public int resourceCost = 0;
	public float constructionTime = 0f;
	public GameObject targetingPrefab;

	protected float constructionTimer;

	protected override void Awake() {
		base.Awake();
		
		constructionTimer = constructionTime;
	}

	protected override void Update() {
		if (constructionTimer > 0f) {
			constructionTimer -= Time.deltaTime;
		}

		base.Update();
	}

	protected override void HandleAIStates() {
		if (constructionTimer <= 0f) {
			base.HandleAIStates();
		}
	}
}
