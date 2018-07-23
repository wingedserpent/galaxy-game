using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using RTSCam;

public class PlayerInputManager : MonoBehaviour {

	public LayerMask ignoreLayers;

	private List<Entity> SelectedEntities { get; set; }
	private Entity LastFocusedEntity;

	private RTSCamera rtsCamera;
	private ClientNetworkManager clientNetworkManager;
	private ClientGameManager clientGameManager;

	private void Awake() {
		SelectedEntities = new List<Entity>();
	}

	void Start () {
		rtsCamera = FindObjectOfType<RTSCamera>();
		clientNetworkManager = ClientNetworkManager.Instance;
		clientGameManager = ClientGameManager.Instance;
	}
	
	void Update () {
		SelectedEntities.RemoveAll(x => x == null);

		if (!EventSystem.current.IsPointerOverGameObject()) { //if not over UI element
			if (Input.GetMouseButtonUp(0)) {
				if (!Input.GetButton("Multiselect")) {
					foreach (Entity entity in SelectedEntities) {
						if (entity != null) {
							SelectionMarker selectionMarker = entity.GetComponentInChildren<SelectionMarker>();
							if (selectionMarker != null) {
								selectionMarker.ToggleRendering(false);
							}
						}
					}
					SelectedEntities.Clear();
				}

				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;

				if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayers)) {
					Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
					if (hitEntity != null && (clientNetworkManager.offlineTest || hitEntity.PlayerId == clientGameManager.MyPlayer.ID) && !SelectedEntities.Contains(hitEntity)) {
						SelectedEntities.Add(hitEntity);

						SelectionMarker selectionMarker = hitEntity.GetComponentInChildren<SelectionMarker>(true);
						if (selectionMarker != null) {
							selectionMarker.ToggleRendering(true);
						}
					}
				}
			} else if (Input.GetMouseButtonUp(1)) {
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;

				if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayers)) {
					Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
					if (hitEntity != null && (clientNetworkManager.offlineTest || hitEntity.TeamId != clientGameManager.MyPlayer.TeamId)) {
						IssueAttackCommand(SelectedEntities.Select(x => x.ID).ToList(), hitEntity.ID);
					} else {
						IssueMoveCommand(SelectedEntities.Select(x => x.ID).ToList(), hit.point);
					}
				}
			}
		}

		if (Input.anyKeyDown) {
			Dictionary<CommandType, List<Entity>> commandEntites = new Dictionary<CommandType, List<Entity>>();

			foreach (Entity selectedEntity in SelectedEntities) {
				CommandType resultCommandType = selectedEntity.EntityController.GetCommandTypeFromInput();
				if (resultCommandType != CommandType.NONE) {
					if (!commandEntites.ContainsKey(resultCommandType)) {
						commandEntites.Add(resultCommandType, new List<Entity>());
					}
					commandEntites[resultCommandType].Add(selectedEntity);
				}
			}

			foreach (KeyValuePair<CommandType, List<Entity>> commandEntity in commandEntites) {
				IssueAbilityCommand(commandEntity.Value.Select(x => x.ID).ToList(), commandEntity.Key);
			}
		}

		if (SelectedEntities.Count == 0 || !SelectedEntities[0].Equals(LastFocusedEntity)) {
			LastFocusedEntity = null;
			rtsCamera.ClearFollowTarget();
		}

		if (Input.GetButtonDown("CameraFocus")) {
			if (SelectedEntities.Count > 0) {
				if (SelectedEntities[0].Equals(LastFocusedEntity)) {
					LastFocusedEntity = SelectedEntities[0];
					rtsCamera.SetFollowTarget(LastFocusedEntity.transform);
				} else {
					LastFocusedEntity = SelectedEntities[0];
					rtsCamera.RefocusOn(LastFocusedEntity.transform.position);
				}
			}
		}
	}

	private void IssueMoveCommand(List<string> entityIds, Vector3 target) {
		Command moveCommand = new Command(CommandType.MOVE, entityIds);
		moveCommand.Point = target;
		clientNetworkManager.SendCommand(moveCommand);
	}

	private void IssueAttackCommand(List<string> entityIds, string targetEntityId) {
		Command attackCommand = new Command(CommandType.ATTACK, entityIds);
		attackCommand.TargetEntityId = targetEntityId;
		clientNetworkManager.SendCommand(attackCommand);
	}

	private void IssueAbilityCommand(List<string> entityIds, CommandType commandType) {
		Command abilityCommand = new Command(commandType, entityIds);
		clientNetworkManager.SendCommand(abilityCommand);
	}
}
