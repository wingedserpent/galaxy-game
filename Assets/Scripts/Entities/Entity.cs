using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Entity : MonoBehaviour, IDarkRiftSerializable {

	public string typeId;
	public bool isInAir;
	public bool canAttackGround;
	public bool canAttackAir;
	public float shieldRechargeCooldown = 5f;

	public virtual bool CanMove { get { return false; } }
	public virtual bool CanAttack { get { return false; } }

	public string ID { get; set; }
	public string PlayerId { get; set; }
	public ushort TeamId { get; set; }
	public Weapon Weapon { get; set; }
	public List<Equipment> Equipment { get; set; }
	public int MaxHealth { get; set; }
	public int CurrentHealth { get; set; }
	public int MaxShield { get; set; }
	public int CurrentShield { get; set; }
	public int ShieldRechargeRate { get; set; }
	public float MoveSpeed { get; set; }
	public float VisionRange { get; set; }

	private float shieldRechargeTimer = 0f;

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

		Equipment = new List<Equipment>();
		
		ID = Guid.NewGuid().ToString();
		PlayerId = "ZZZZ";
		TeamId = 999;
	}

	private void Update() {
		if (shieldRechargeTimer > 0f) {
			shieldRechargeTimer -= Time.deltaTime;
			if (shieldRechargeTimer <= 0f) {
				AdjustShield(ShieldRechargeRate);
				shieldRechargeTimer = 1f;
			}
		}
	}

	public void SetPlayer(Player player) {
		PlayerId = player.ID;
		TeamId = player.TeamId;
	}

	public int AdjustHealth(int adjustment) {
		CurrentHealth = Mathf.Clamp(CurrentHealth + adjustment, 0, MaxHealth);
		return CurrentHealth;
	}

	public int AdjustShield(int adjustment) {
		CurrentShield = Mathf.Clamp(CurrentShield + adjustment, 0, MaxShield);
		if (adjustment < 0) {
			shieldRechargeTimer = shieldRechargeCooldown;
		}
		return CurrentShield;
	}

	public void ApplyEquipment() {
		foreach (Equipment equipment in Equipment) {
			MaxHealth += equipment.Health;
			CurrentHealth += equipment.Health;
			MaxShield += equipment.Shield;
			CurrentShield += equipment.Shield;
			ShieldRechargeRate += equipment.ShieldRechargeRate;
			MoveSpeed += equipment.MoveSpeed;
			VisionRange += equipment.VisionRange;
			//TODO apply move speed, vision range, abilities
		}
	}

	public virtual void Deserialize(DeserializeEvent e) {
		typeId = e.Reader.ReadString();
		ID = e.Reader.ReadString();
		PlayerId = e.Reader.ReadString();
		TeamId = e.Reader.ReadUInt16();
		MaxHealth = e.Reader.ReadInt32();
		CurrentHealth = e.Reader.ReadInt32();
		MaxShield = e.Reader.ReadInt32();
		CurrentShield = e.Reader.ReadInt32();
		ShieldRechargeRate = e.Reader.ReadInt32();
		MoveSpeed = e.Reader.ReadSingle();
		VisionRange = e.Reader.ReadSingle();
		transform.position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		transform.rotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		
		if (e.Reader.ReadBoolean()) {
			Weapon = e.Reader.ReadSerializable<Weapon>();
		}
		int numEquipments = e.Reader.ReadInt32();
		for (int i = 0; i < numEquipments; i++) {
			Equipment.Add(e.Reader.ReadSerializable<Equipment>());
		}

		if (EntityController != null) {
			e.Reader.ReadSerializableInto(ref _entityController);
		}
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(typeId);
		e.Writer.Write(ID);
		e.Writer.Write(PlayerId);
		e.Writer.Write(TeamId);
		e.Writer.Write(MaxHealth);
		e.Writer.Write(CurrentHealth);
		e.Writer.Write(MaxShield);
		e.Writer.Write(CurrentShield);
		e.Writer.Write(ShieldRechargeRate);
		e.Writer.Write(MoveSpeed);
		e.Writer.Write(VisionRange);
		e.Writer.Write(transform.position.x); e.Writer.Write(transform.position.y); e.Writer.Write(transform.position.z);
		e.Writer.Write(transform.rotation.x); e.Writer.Write(transform.rotation.y); e.Writer.Write(transform.rotation.z); e.Writer.Write(transform.rotation.w);

		//TODO determine how to write certain things only on first write to client
		e.Writer.Write(Weapon != null);
		if (Weapon != null) {
			e.Writer.Write(Weapon);
		}
		e.Writer.Write(Equipment.Count);
		foreach (Equipment equipment in Equipment) {
			e.Writer.Write(equipment);
		}

		//controller must be written last since it can depend on other data (e.g. entity.Weapon)
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
