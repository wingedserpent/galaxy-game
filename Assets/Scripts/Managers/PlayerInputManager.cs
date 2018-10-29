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
	public List<BuildCommand> buildCommands;

	private List<Entity> SelectedEntities { get; set; }
	private Entity LastFocusedEntity;
	private bool isSelecting = false;
	private Vector3 mouseStartPosition;

	private bool isInBuildMode = false;
	private GameObject currentTargetingObject;
	private OwnedObject currentTargetingReference;
	private int currentTargetingResourceCost;
	private Collider currentTargetingCollider;
	private bool isTargetingValid = false;

	private RTSCamera rtsCamera;
	private ClientNetworkManager clientNetworkManager;
	private ClientGameManager clientGameManager;
	private ClientEntityManager clientEntityManager;
	private UIManager uiManager;

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
		uiManager = UIManager.Instance;

		rtsCamera = FindObjectOfType<RTSCamera>();
	}
	
	void Update () {
		if (Input.GetButtonUp("Scoreboard")) {
			uiManager.ClosePlayerList();
		}

		if (!uiManager.IsUIReceivingInput) {
			//meta-game input
			if (Input.GetButtonDown("Scoreboard")) {
				uiManager.OpenPlayerList();
			} else if (clientGameManager.IsAcceptingGameInput) {
				SelectedEntities.RemoveAll(x => x == null);

				//cancel should override all other inputs
				if (Input.GetButtonDown("Cancel")) {
					if (isInBuildMode) {
						CancelBuildMode();
					} else {
						DeselectAll();
						LastFocusedEntity = null;
						rtsCamera.ClearFollowTarget();
					}
				} else {
					if (!EventSystem.current.IsPointerOverGameObject()) { //if not over UI element
						if (currentTargetingObject != null) {
							//when targeting object exists, some inputs are handled differently
							if (Input.GetMouseButtonUp(0) && isTargetingValid) {
								//check resource cost
								if (clientGameManager.MyPlayer.Resources >= currentTargetingResourceCost) {
									if (currentTargetingReference is Structure) {
										IssueConstructionRequest(currentTargetingReference.typeId, currentTargetingObject.transform.position);
									} else {
										IssuePlayerEventRequest(currentTargetingReference.typeId, currentTargetingObject.transform.position);
									}
								}
								Destroy(currentTargetingObject);
								CancelBuildMode();
							} else if (Input.GetMouseButtonDown(1)) {
								Destroy(currentTargetingObject);
							} else {
								bool isValidPlacement = false;

								Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
								RaycastHit hit;
								if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerManager.Instance.groundMask)) {
									currentTargetingObject.transform.position = hit.point;

									if (currentTargetingReference is Structure) {
										//navmesh check
										NavMeshHit navHit;
										if (NavMesh.SamplePosition(hit.point, out navHit, 0.1f, NavMesh.AllAreas)) {
											//collision/overlap check
											Collider[] colliders = Physics.OverlapBox(currentTargetingCollider.bounds.center,
												currentTargetingCollider.bounds.extents, currentTargetingCollider.transform.rotation, LayerManager.Instance.constructionOverlapMask);
											if (colliders.Count(x => x != currentTargetingCollider) == 0) {
												isValidPlacement = true;
											}
										}
									} else {
										isValidPlacement = true;
									}
								}

								SetTargetingValid(isValidPlacement);
							}
						} else {
							//mouse-related inputs
							if (Input.GetMouseButtonDown(0)) {
								CancelBuildMode();
								isSelecting = true;
								mouseStartPosition = Input.mousePosition;
							}

							if (Input.GetMouseButtonUp(0)) {
								bool isMultiselecting = Input.GetButton("Multiselect");

								if (!isMultiselecting) {
									DeselectAll(false);
								}

								if (isSelecting) {
									Bounds bounds = GetViewportBounds(mouseStartPosition, Input.mousePosition);
									if (bounds.size.x > 0.01 && bounds.size.y > 0.01) {
										//selection box is large enough, select entities inside
										IEnumerable<Entity> entitiesToConsider;
										if (OverallStateManager.Instance.IsOfflineTest) {
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

										uiManager.UpdateSelectedEntities(SelectedEntities);
									} else {
										//selection box is too small, do point selection
										Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
										RaycastHit hit;
										if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerManager.Instance.clickableMask)) {
											Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
											if (hitEntity != null && hitEntity.PlayerId == clientGameManager.MyPlayer.ID) {
												if (!SelectedEntities.Contains(hitEntity)) {
													SelectEntity(hitEntity);
												} else if (isMultiselecting) {
													DeselectEntity(hitEntity, true);
												}
											}
										}

										uiManager.UpdateSelectedEntities(SelectedEntities);
									}
								}

								isSelecting = false;
							}

							if (Input.GetMouseButtonUp(1)) {
								if (SelectedEntities.Count > 0) {
									Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
									RaycastHit hit;

									if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerManager.Instance.clickableMask)) {
										Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
										if (hitEntity != null && hitEntity.TeamId != clientGameManager.MyPlayer.TeamId) {
											IssueAttackCommand(SelectedEntities
												.Where(x => x.CanAttack && hitEntity.isInAir ? x.canAttackAir : x.canAttackGround)
												.Select(x => x.ID).ToList(), hitEntity.ID);
										} else {
											IssueMoveCommand(SelectedEntities.Where(x => x.CanMove).Select(x => x.ID).ToList(), hit.point);
										}
									}
								}
							}
						} //end current construction is null
					} //end UI element check

					//inputs that can be accepted even when hovering mouse over ui element
					if (Input.GetButtonDown("Build")) {
						DeselectAll();
						isInBuildMode = true;
						uiManager.OpenBuildMenu(buildCommands);
					} else if (isInBuildMode) {
						foreach (BuildCommand buildCommand in buildCommands) {
							if (Input.GetKeyDown(buildCommand.key)) {
								//check resource cost
								Entity entityRef = clientEntityManager.GetEntityReference(buildCommand.targetTypeId);
								if (entityRef != null) {
									if (clientGameManager.MyPlayer.Resources >= (entityRef as Structure).resourceCost) {
										DeselectAll();
										currentTargetingReference = entityRef;
										currentTargetingResourceCost = (entityRef as Structure).resourceCost;
										currentTargetingObject = clientEntityManager.SpawnStructureTargeting(currentTargetingReference.typeId);
										currentTargetingCollider = currentTargetingObject.GetComponentInChildren<Collider>();
									}
								} else {
									PlayerEvent playerEventRef = clientEntityManager.GetPlayerEventReference(buildCommand.targetTypeId);
									if (playerEventRef != null && clientGameManager.MyPlayer.Resources >= playerEventRef.resourceCost) {
										DeselectAll();
										currentTargetingReference = playerEventRef;
										currentTargetingResourceCost = playerEventRef.resourceCost;
										currentTargetingObject = clientEntityManager.SpawnPlayerEventTargeting(currentTargetingReference.typeId);
										currentTargetingCollider = currentTargetingObject.GetComponentInChildren<Collider>();
									}
								}
							}
						}
					}

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
				} //end non-cancel
			}
		}
	}

	private void SetTargetingValid(bool newIsValid) {
		if (newIsValid && !isTargetingValid) {
			isTargetingValid = newIsValid;
			foreach (Renderer renderer in currentTargetingObject.GetComponentsInChildren<Renderer>()) {
				Color color = renderer.material.color;
				color.b = color.g = 1f;
				renderer.material.color = color;
			}
		} else if (!newIsValid && isTargetingValid) {
			isTargetingValid = newIsValid;
			foreach (Renderer renderer in currentTargetingObject.GetComponentsInChildren<Renderer>()) {
				Color color = renderer.material.color;
				color.b = color.g = 0f;
				renderer.material.color = color;
			}
		}
	}

	private void CancelBuildMode() {
		isInBuildMode = false;
		uiManager.CloseBuildMenu();
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

	private void DeselectAll(bool alsoStopSelecting=true) {
		foreach (Entity entity in SelectedEntities) {
			if (entity != null) {
				DeselectEntity(entity, false);
			}
		}
		SelectedEntities.Clear();
		if (alsoStopSelecting) {
			isSelecting = false;
		}

		if (currentTargetingObject != null) {
			Destroy(currentTargetingObject);
		}

		uiManager.UpdateSelectedEntities(SelectedEntities);
	}

	public void ForceSelectedEntity(Entity entity) {
		DeselectAll(true);
		SelectEntity(entity);

		uiManager.UpdateSelectedEntities(SelectedEntities);
	}

	private void IssueMoveCommand(List<string> entityIds, Vector3 target) {
		if (entityIds.Count > 0) {
			EntityCommand moveCommand = new EntityCommand(CommandType.MOVE, entityIds);
			moveCommand.Point = target;
			clientNetworkManager.SendCommand(moveCommand);
		}
	}

	private void IssueStopCommand(List<string> entityIds) {
		if (entityIds.Count > 0) {
			EntityCommand stopCommand = new EntityCommand(CommandType.STOP, entityIds);
			clientNetworkManager.SendCommand(stopCommand);
		}
	}

	private void IssueAttackCommand(List<string> entityIds, string targetEntityId) {
		if (entityIds.Count > 0) {
			EntityCommand attackCommand = new EntityCommand(CommandType.ATTACK, entityIds);
			attackCommand.TargetEntityId = targetEntityId;
			clientNetworkManager.SendCommand(attackCommand);
		}
	}

	private void IssueAbilityCommand(List<string> entityIds, CommandType commandType) {
		if (entityIds.Count > 0) {
			EntityCommand abilityCommand = new EntityCommand(commandType, entityIds);
			clientNetworkManager.SendCommand(abilityCommand);
		}
	}

	private void IssueConstructionRequest(string structureTypeId, Vector3 position) {
		clientNetworkManager.SendConstruction(structureTypeId, position);
	}

	private void IssuePlayerEventRequest(string playerEventTypeId, Vector3 position) {
		clientNetworkManager.SendPlayerEvent(playerEventTypeId, position);
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
