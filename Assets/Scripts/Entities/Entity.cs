using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Entity : MonoBehaviour, IDarkRiftSerializable {

	public string typeId;
	public int currentHealth;
	public int maxHealth;
	public float attackRange;
	public float attackSpeed;
	public int attackDamage;

	public string ID { get; set; }
	public string PlayerId { get; set; }
	public ushort TeamId { get; set; }

	private EntityController _entityController;
	public EntityController EntityController {
		get {
			return _entityController;
		}
		private set {
			_entityController = value;
		}
	}

	private void Awake() {
		EntityController = GetComponent<EntityController>();

		ID = Guid.NewGuid().ToString();
		PlayerId = "ZZZZ";
		TeamId = 999;
	}

	public void SetPlayer(Player player) {
		PlayerId = player.ID;
		TeamId = player.TeamId;
	}

	public int AdjustHealth(int adjustment) {
		currentHealth = Mathf.Clamp(currentHealth + adjustment, 0, maxHealth);
		return currentHealth;
	}

	public virtual void Deserialize(DeserializeEvent e) {
		typeId = e.Reader.ReadString();
		ID = e.Reader.ReadString();
		PlayerId = e.Reader.ReadString();
		TeamId = e.Reader.ReadUInt16();
		currentHealth = e.Reader.ReadInt32();
		maxHealth = e.Reader.ReadInt32();
		attackSpeed = e.Reader.ReadSingle();
		attackDamage = e.Reader.ReadInt32();
		transform.position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		transform.rotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		if (EntityController != null) {
			e.Reader.ReadSerializableInto(ref _entityController);
		}
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(typeId);
		e.Writer.Write(ID);
		e.Writer.Write(PlayerId);
		e.Writer.Write(TeamId);
		e.Writer.Write(currentHealth);
		e.Writer.Write(maxHealth);
		e.Writer.Write(attackSpeed);
		e.Writer.Write(attackDamage);
		e.Writer.Write(transform.position.x); e.Writer.Write(transform.position.y); e.Writer.Write(transform.position.z);
		e.Writer.Write(transform.rotation.x); e.Writer.Write(transform.rotation.y); e.Writer.Write(transform.rotation.z); e.Writer.Write(transform.rotation.w);
		if (EntityController != null) {
			e.Writer.Write(EntityController);
		}
	}

	public override bool Equals(object obj) {
		var entity = obj as Entity;
		return entity != null &&
			   base.Equals(obj) &&
			   ID == entity.ID;
	}

	public override int GetHashCode() {
		var hashCode = -160907283;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ID);
		return hashCode;
	}
}
