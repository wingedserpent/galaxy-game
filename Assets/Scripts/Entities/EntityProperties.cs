using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

[Serializable]
public class EntityProperties : IDarkRiftSerializable {

	public int currentHealth;
	public int maxHealth;
	public float attackRange;
	public float attackSpeed;
	public int attackDamage;

	public EntityProperties() { }

	public int AdjustHealth(int adjustment) {
		currentHealth = Mathf.Clamp(currentHealth + adjustment, 0, maxHealth);
		return currentHealth;
	}

	public virtual void Deserialize(DeserializeEvent e) {
		currentHealth = e.Reader.ReadInt32();
		maxHealth = e.Reader.ReadInt32();
		attackSpeed = e.Reader.ReadSingle();
		attackDamage = e.Reader.ReadInt32();
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(currentHealth);
		e.Writer.Write(maxHealth);
		e.Writer.Write(attackSpeed);
		e.Writer.Write(attackDamage);
	}
}
