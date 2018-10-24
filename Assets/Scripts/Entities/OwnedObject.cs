using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class OwnedObject : MonoBehaviour, IDarkRiftSerializable {

	public string typeId;
	public string displayName;

	public string ID { get; set; }
	public string PlayerId { get; set; }
	public ushort TeamId { get; set; }

	protected virtual void Awake() {
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
		displayName = e.Reader.ReadString();
		ID = e.Reader.ReadString();
		PlayerId = e.Reader.ReadString();
		TeamId = e.Reader.ReadUInt16();
		transform.position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		transform.rotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(typeId);
		e.Writer.Write(displayName);
		e.Writer.Write(ID);
		e.Writer.Write(PlayerId);
		e.Writer.Write(TeamId);
		e.Writer.Write(transform.position.x); e.Writer.Write(transform.position.y); e.Writer.Write(transform.position.z);
		e.Writer.Write(transform.rotation.x); e.Writer.Write(transform.rotation.y); e.Writer.Write(transform.rotation.z); e.Writer.Write(transform.rotation.w);
	}

	public override bool Equals(object obj) {
		var playerEvent = obj as PlayerEvent;
		return playerEvent != null &&
			   base.Equals(obj) &&
			   ID == playerEvent.ID;
	}

	public override int GetHashCode() {
		var hashCode = -160907283;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ID);
		return hashCode;
	}
}
