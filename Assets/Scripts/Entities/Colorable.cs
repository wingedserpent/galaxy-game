using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Colorable : MonoBehaviour {

	public bool recursive = false;

	private List<Material> materials = new List<Material>();

	private void Awake() {
		if (recursive) {
			materials.AddRange(GetComponentsInChildren<Renderer>(true).Select(x => x.material));
		} else {
			materials.Add(GetComponent<Renderer>().material);
		}
	}

	public void SetColor(Color color) {
		foreach (Material material in materials) {
			material.color = color;
		}
	}
}
