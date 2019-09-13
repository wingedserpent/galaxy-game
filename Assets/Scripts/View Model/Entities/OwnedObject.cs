using System;
using UnityEngine;
using DarkRift;
using System.Collections.Generic;

public class OwnedObject : MonoBehaviour, IDarkRiftSerializable {

	public string typeId;
	public string displayName;

	public string uniqueId { get; set; }
	public string playerId { get; set; }
	public ushort teamId { get; set; }

	protected virtual void Awake() {
		uniqueId = Guid.NewGuid().ToString();
		playerId = "ZZZZ";
		teamId = 999;
	}

	public void SetPlayer(Player player) {
		playerId = player.id;
		teamId = player.teamId;
	}
	
	public virtual void Deserialize(DeserializeEvent e) {
		typeId = e.Reader.ReadString();
		displayName = e.Reader.ReadString();
		uniqueId = e.Reader.ReadString();
		playerId = e.Reader.ReadString();
		teamId = e.Reader.ReadUInt16();
		transform.position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
		transform.rotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(typeId);
		e.Writer.Write(displayName);
		e.Writer.Write(uniqueId);
		e.Writer.Write(playerId);
		e.Writer.Write(teamId);
		e.Writer.Write(transform.position.x); e.Writer.Write(transform.position.y); e.Writer.Write(transform.position.z);
		e.Writer.Write(transform.rotation.x); e.Writer.Write(transform.rotation.y); e.Writer.Write(transform.rotation.z); e.Writer.Write(transform.rotation.w);
	}

	public override bool Equals(object obj) {
		var ownedObject = obj as OwnedObject;
		return ownedObject != null &&
			   base.Equals(obj) &&
			   uniqueId == ownedObject.uniqueId;
	}

	public override int GetHashCode() {
		var hashCode = -160907283;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(uniqueId);
		return hashCode;
	}
}
