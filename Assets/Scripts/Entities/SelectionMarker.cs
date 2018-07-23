using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionMarker : MonoBehaviour {

	public void ToggleRendering(bool on) {
		gameObject.SetActive(on);
	}
}
