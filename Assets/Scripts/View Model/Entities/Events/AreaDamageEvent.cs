using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using System.Linq;

public class AreaDamageEvent : PlayerEvent {
	
	public int damage;
	public float attackRate;

	protected void Start() {
		if (NetworkStatus.Instance.IsServer) {
			InvokeRepeating("DealDamage", attackRate, attackRate);
		}
	}

	protected void DealDamage() {
		foreach (Entity target in FindDamageTargets()) {
			target.TakeDamage(damage);
		}
	}

	protected IEnumerable<Entity> FindDamageTargets() {
		Collider[] overlaps = Physics.OverlapSphere(transform.position, radius, LayerManager.Instance.damageableMask);
		return (from target in overlaps.Select(x => x.GetComponentInParent<Entity>())
				where target.teamId != teamId
				select target);
	}
}
