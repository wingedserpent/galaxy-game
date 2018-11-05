using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Entity : OwnedObject {

	public bool isInAir;
	public bool canAttackGround;
	public bool canAttackAir;
	public float shieldRechargeCooldown = 5f;

	public virtual bool CanMove { get { return false; } }
	public virtual bool CanAttackTarget { get { return false; } }
	public virtual bool CanAttackLocation { get { return false; } }
	
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

	protected override void Awake() {
		base.Awake();

		EntityController = GetComponent<EntityController>();

		Equipment = new List<Equipment>();
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
			//TODO apply abilities
		}
	}

	public override void Deserialize(DeserializeEvent e) {
		base.Deserialize(e);
		
		MaxHealth = e.Reader.ReadInt32();
		CurrentHealth = e.Reader.ReadInt32();
		MaxShield = e.Reader.ReadInt32();
		CurrentShield = e.Reader.ReadInt32();
		ShieldRechargeRate = e.Reader.ReadInt32();
		MoveSpeed = e.Reader.ReadSingle();
		VisionRange = e.Reader.ReadSingle();
		
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

	public override void Serialize(SerializeEvent e) {
		base.Serialize(e);
		
		e.Writer.Write(MaxHealth);
		e.Writer.Write(CurrentHealth);
		e.Writer.Write(MaxShield);
		e.Writer.Write(CurrentShield);
		e.Writer.Write(ShieldRechargeRate);
		e.Writer.Write(MoveSpeed);
		e.Writer.Write(VisionRange);

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
}
