using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PercentageRadius : MonoBehaviour
{
	public Transform fillTransform;

	public void SetDisplayAmounts(float currentAmount, float maxAmount) {
		if (maxAmount != 0) {
			Vector3 scale = fillTransform.localScale;
			scale.x = scale.y = currentAmount / maxAmount;
			fillTransform.localScale = scale;
		}
	}
}
