using DarkRift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Ability : MonoBehaviour, IDarkRiftSerializable {
	
	public InputCommand inputCommand;

	public virtual string abilityTypeId { get { return null; } }

	protected Entity entity { get; private set; }
	protected bool isActive { get; private set; }

	protected virtual void Awake() {
		entity = GetComponentInParent<Entity>();
	}

	public void Activate() {
		if (!isActive) {
			isActive = OnActivate();
		}
	}

	protected void Deactivate() {
		if (isActive) {
			isActive = !OnDeactivate();
		}
	}

	protected abstract bool OnActivate();
	protected abstract bool OnDeactivate();

	public virtual void Deserialize(DeserializeEvent e) {
		if (e.Reader.ReadBoolean()) {
			Activate();
		} else {
			Deactivate();
		}
	}

	public virtual void Serialize(SerializeEvent e) {
		e.Writer.Write(isActive);
	}
}
