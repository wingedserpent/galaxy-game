using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class Entity : MonoBehaviour, IDarkRiftSerializable {

	public string typeId;
	public EntityProperties properties = new EntityProperties();

	public string ID { get; set; }
	public string PlayerId { get; set; }
	public ushort TeamId { get; set; }
	public int PlayerUnitId { get; set; }

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

	public virtual void Deserialize(DeserializeEvent e) {
		typeId = e.Reader.ReadString();
		ID = e.Reader.ReadString();
		PlayerId = e.Reader.ReadString();
		TeamId = e.Reader.ReadUInt16();
		PlayerUnitId = e.Reader.ReadInt32();
		e.Reader.ReadSerializableInto(ref properties);
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
		e.Writer.Write(PlayerUnitId);
		e.Writer.Write(properties);
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
