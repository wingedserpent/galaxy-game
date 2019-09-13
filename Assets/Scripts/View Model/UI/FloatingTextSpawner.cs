using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextSpawner : MonoBehaviour {

	public float lifeSpan = 1f;

	public FloatingText textPrefab;

	public void CreateFloatingText(string text, Transform parent) {
		FloatingText floatingText = Instantiate<GameObject>(textPrefab.gameObject, parent).GetComponent<FloatingText>();
		floatingText.Text = text;
		floatingText.Lifespan = lifeSpan;
	}
}
