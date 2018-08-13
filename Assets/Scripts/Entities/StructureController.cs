using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DarkRift;

public class StructureController : EntityController {

	protected float constructionTimer;

	protected Structure structure;

	protected override void Awake() {
		base.Awake();

		structure = GetComponent<Structure>();
		constructionTimer = structure.constructionTime;
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
