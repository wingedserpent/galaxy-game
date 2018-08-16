using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponEffect {
	public string weaponType;
	public GameObject projectilePrefab;
	public List<Transform> projectileSpawnPoints = new List<Transform>();
	public List<GameObject> existingProjectiles = new List<GameObject>();
	public float attackEffectTime = 0f;
	public AudioClip attackSound;
}
