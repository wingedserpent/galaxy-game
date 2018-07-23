using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour {

	public float Lifespan { get; set; }
	public string Text { get; set; }

	private TextMesh textComponent;
	private Vector3 targetPos;

	private void Awake() {
		textComponent = GetComponent<TextMesh>();
	}

	private void Start() {
		textComponent.text = Text;
		targetPos = transform.position + new Vector3(0f, 0.5f, 0f);
		Destroy(this.gameObject, Lifespan);
	}

	private void Update() {
		transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime);
	}
}
