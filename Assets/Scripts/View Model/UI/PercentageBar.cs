using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PercentageBar : MonoBehaviour {

	public RectTransform foreground;
	public Text displayText;
	public string displayTextPrefix;

	public void SetDisplayAmounts(int currentAmount, int maxAmount) {
		if (maxAmount != 0) {
			Vector3 scale = foreground.localScale;
			scale.x = (float)currentAmount / maxAmount;
			foreground.localScale = scale;

			if (displayText) {
				displayText.text = displayTextPrefix + currentAmount + " / " + maxAmount;
			}
		}
	}
}
