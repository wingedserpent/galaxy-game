using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PercentageRadius : MonoBehaviour
{
	public Transform fillTransform;
	private float savedAmount = Mathf.Infinity;

	public void SetDisplayAmounts(float currentAmount, float maxAmount) {
		if (savedAmount != currentAmount && maxAmount != 0) {
			Vector3 scale = fillTransform.localScale;
			scale.x = scale.y = currentAmount / maxAmount;
			fillTransform.localScale = scale;
			savedAmount = currentAmount;
		}
	}
}
