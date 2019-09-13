using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LayerManager : Singleton<LayerManager> {

	public LayerMask unitMask;
	public LayerMask damageableMask;
	public LayerMask clickableMask;
	public LayerMask groundMask;
	public LayerMask constructionOverlapMask;
	public LayerMask visionTargetMask;
	public LayerMask visionBlockerMask;
}
