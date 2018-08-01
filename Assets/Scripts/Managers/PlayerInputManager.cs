using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using RTSCam;
using UnityEngine.AI;

public class PlayerInputManager : MonoBehaviour {

	public Color selectionBoxInnerColor;
	public Color selectionBoxBorderColor;
	public LayerMask clickableLayers;
	public LayerMask groundLayers;
	public LayerMask constructionOverlapLayers;

	private List<Entity> SelectedEntities { get; set; }
	private Entity LastFocusedEntity;
	private bool isSelecting = false;
	private Vector3 mouseStartPosition;
	private GameObject currentConstruction;
	private Collider currentConstructionCollider;
	private string currentConstructionTypeId;
	private bool isConstructionValid = false;

	private RTSCamera rtsCamera;
	private ClientNetworkManager clientNetworkManager;
	private ClientGameManager clientGameManager;
	private ClientEntityManager clientEntityManager;

	static Texture2D _whiteTexture;
	public static Texture2D WhiteTexture {
		get {
			if (_whiteTexture == null) {
				_whiteTexture = new Texture2D(1, 1);
				_whiteTexture.SetPixel(0, 0, Color.white);
				_whiteTexture.Apply();
			}

			return _whiteTexture;
		}
	}

	private void Awake() {
		SelectedEntities = new List<Entity>();
	}

	void Start () {
		clientNetworkManager = ClientNetworkManager.Instance;
		clientGameManager = ClientGameManager.Instance;
		clientEntityManager = ClientEntityManager.Instance;

		rtsCamera = FindObjectOfType<RTSCamera>();
	}
	
	void Update () {
		if (clientGameManager.IsAcceptingGameInput) {
			SelectedEntities.RemoveAll(x => x == null);

			if (Input.GetButtonDown("BuildTurret") && currentConstruction == null) {
				DeselectAll();
				currentConstructionTypeId = "turret";
				currentConstruction = clientEntityManager.SpawnConstruction(currentConstructionTypeId);
				currentConstructionCollider = currentConstruction.GetComponentInChildren<Collider>();
			}

			if (Input.GetButtonDown("Cancel")) {
				DeselectAll();
				LastFocusedEntity = null;
				rtsCamera.ClearFollowTarget();
			}

			if (!EventSystem.current.IsPointerOverGameObject()) { //if not over UI element
				if (currentConstruction != null) {
					isSelecting = false;

					if (Input.GetMouseButtonDown(0) && isConstructionValid) {
						IssueConstructionRequest(currentConstructionTypeId, currentConstruction.transform.position);
						Destroy(currentConstruction);
					} else if (Input.GetMouseButtonDown(1) || Input.GetButtonDown("Cancel")) {
						Destroy(currentConstruction);
					} else {
						bool isValidPlacement = false;

						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
						RaycastHit hit;
						if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayers)) {
							currentConstruction.transform.position = hit.point;

							//navmesh check
							NavMeshHit navHit;
							if (NavMesh.SamplePosition(hit.point, out navHit, 0.1f, NavMesh.AllAreas)) {
								//collision/overlap check
								Collider[] colliders = Physics.OverlapBox(currentConstructionCollider.bounds.center,
									currentConstructionCollider.bounds.extents, currentConstructionCollider.transform.rotation, constructionOverlapLayers);
								if (colliders.Count(x => x != currentConstructionCollider) == 0) {
									isValidPlacement = true;
								}
							}
						}

						SetConstructionValid(isValidPlacement);
					}
				} else {
					if (Input.GetMouseButtonDown(0)) {
						isSelecting = true;
						mouseStartPosition = Input.mousePosition;
					} else if (Input.GetMouseButtonUp(0)) {
						isSelecting = false;
						bool isMultiselecting = Input.GetButton("Multiselect");

						if (!isMultiselecting) {
							DeselectAll();
						}

						Bounds bounds = GetViewportBounds(mouseStartPosition, Input.mousePosition);
						if (bounds.size.x > 0.01 && bounds.size.y > 0.01) {
							//selection box is large enough, select entities inside
							IEnumerable<Entity> entitiesToConsider;
							if (clientGameManager.IsOfflineTest) {
								entitiesToConsider = FindObjectsOfType<Entity>();
							} else {
								entitiesToConsider = clientEntityManager.MySquad.Values;
							}

							foreach (Entity hitEntity in entitiesToConsider.Where(x => IsWithinBounds(bounds, x.transform.position))) {
								if (!SelectedEntities.Contains(hitEntity)) {
									SelectEntity(hitEntity);
								} else if (isMultiselecting) {
									DeselectEntity(hitEntity, true);
								}
							}
						} else {
							//selection box is too small, do point selection
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							RaycastHit hit;
							if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayers)) {
								Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
								if (hitEntity != null && (clientGameManager.IsOfflineTest || hitEntity.PlayerId == clientGameManager.MyPlayer.ID)) {
									if (!SelectedEntities.Contains(hitEntity)) {
										SelectEntity(hitEntity);
									} else if (isMultiselecting) {
										DeselectEntity(hitEntity, true);
									}
								}
							}
						}
					}

					if (Input.GetMouseButtonUp(1)) {
						if (SelectedEntities.Count > 0) {
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							RaycastHit hit;

							if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayers)) {
								Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
								if (hitEntity != null && hitEntity.TeamId != clientGameManager.MyPlayer.TeamId) {
									IssueAttackCommand(SelectedEntities.Where(x => x.CanAttack).Select(x => x.ID).ToList(), hitEntity.ID);
								} else {
									IssueMoveCommand(SelectedEntities.Where(x => x.CanMove).Select(x => x.ID).ToList(), hit.point);
								}
							}
						}
					}
				}
			} //end UI element check

			//other selection-based inputs
			if (SelectedEntities.Count > 0) {
				if (Input.GetButtonDown("Stop")) {
					IssueStopCommand(SelectedEntities.Select(x => x.ID).ToList());
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

			if (SelectedEntities.Count == 0 || !SelectedEntities[0].Equals(LastFocusedEntity)) {
				LastFocusedEntity = null;
				rtsCamera.ClearFollowTarget();
			}
		}
	}

	private void SetConstructionValid(bool newIsValid) {
		if (newIsValid && !isConstructionValid) {
			isConstructionValid = newIsValid;
			foreach (Renderer renderer in currentConstruction.GetComponentsInChildren<Renderer>()) {
				Color color = renderer.material.color;
				color.b = color.g = 1f;
				renderer.material.color = color;
			}
		} else if (!newIsValid && isConstructionValid) {
			isConstructionValid = newIsValid;
			foreach (Renderer renderer in currentConstruction.GetComponentsInChildren<Renderer>()) {
				Color color = renderer.material.color;
				color.b = color.g = 0f;
				renderer.material.color = color;
			}
		}
	}

	private void SelectEntity(Entity entity) {
		SelectedEntities.Add(entity);

		SelectionMarker selectionMarker = entity.GetComponentInChildren<SelectionMarker>(true);
		if (selectionMarker != null) {
			selectionMarker.ToggleRendering(true);
		}
	}

	private void DeselectEntity(Entity entity, bool removeFromSelection) {
		SelectionMarker selectionMarker = entity.GetComponentInChildren<SelectionMarker>();
		if (selectionMarker != null) {
			selectionMarker.ToggleRendering(false);
		}

		if (removeFromSelection) {
			SelectedEntities.Remove(entity);
		}
	}

	private void DeselectAll() {
		foreach (Entity entity in SelectedEntities) {
			if (entity != null) {
				DeselectEntity(entity, false);
			}
		}
		SelectedEntities.Clear();
	}

	private void IssueMoveCommand(List<string> entityIds, Vector3 target) {
		if (entityIds.Count > 0) {
			Command moveCommand = new Command(CommandType.MOVE, entityIds);
			moveCommand.Point = target;
			clientNetworkManager.SendCommand(moveCommand);
		}
	}

	private void IssueStopCommand(List<string> entityIds) {
		if (entityIds.Count > 0) {
			Command stopCommand = new Command(CommandType.STOP, entityIds);
			clientNetworkManager.SendCommand(stopCommand);
		}
	}

	private void IssueAttackCommand(List<string> entityIds, string targetEntityId) {
		if (entityIds.Count > 0) {
			Command attackCommand = new Command(CommandType.ATTACK, entityIds);
			attackCommand.TargetEntityId = targetEntityId;
			clientNetworkManager.SendCommand(attackCommand);
		}
	}

	private void IssueAbilityCommand(List<string> entityIds, CommandType commandType) {
		if (entityIds.Count > 0) {
			Command abilityCommand = new Command(commandType, entityIds);
			clientNetworkManager.SendCommand(abilityCommand);
		}
	}

	private void IssueConstructionRequest(string structureTypeId, Vector3 position) {
		clientNetworkManager.SendConstruction(structureTypeId, position);
	}

	// Start box selection functions

	private void OnGUI() {
		if (isSelecting) {
			Rect rect = GetScreenRect(mouseStartPosition, Input.mousePosition);
			DrawScreenRect(rect, selectionBoxInnerColor);
			DrawScreenRectBorder(rect, 1, selectionBoxBorderColor);
		}
	}

	public Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2) {
		// Move origin from bottom left to top left
		screenPosition1.y = Screen.height - screenPosition1.y;
		screenPosition2.y = Screen.height - screenPosition2.y;
		// Calculate corners
		Vector3 topLeft = Vector3.Min(screenPosition1, screenPosition2);
		Vector3 bottomRight = Vector3.Max(screenPosition1, screenPosition2);
		// Create Rect
		return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
	}

	public void DrawScreenRect(Rect rect, Color color) {
		GUI.color = color;
		GUI.DrawTexture(rect, WhiteTexture);
		GUI.color = Color.white;
	}

	public void DrawScreenRectBorder(Rect rect, float thickness, Color color) {
		DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
		DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
		DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
		DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
	}

	public Bounds GetViewportBounds(Vector3 screenPosition1, Vector3 screenPosition2) {
		Vector3 v1 = Camera.main.ScreenToViewportPoint(screenPosition1);
		Vector3 v2 = Camera.main.ScreenToViewportPoint(screenPosition2);
		Vector3 min = Vector3.Min(v1, v2);
		Vector3 max = Vector3.Max(v1, v2);
		min.z = Camera.main.nearClipPlane;
		max.z = Camera.main.farClipPlane;

		Bounds bounds = new Bounds();
		bounds.SetMinMax(min, max);
		return bounds;
	}

	public bool IsWithinBounds(Bounds viewportBounds, Vector3 position) {
		return viewportBounds.Contains(Camera.main.WorldToViewportPoint(position));
	}
}
