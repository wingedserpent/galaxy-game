using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using RTSCam;

public class PlayerInputManager : MonoBehaviour {

	public Color selectionBoxInnerColor;
	public Color selectionBoxBorderColor;
	public LayerMask ignoreLayers;

	private List<Entity> SelectedEntities { get; set; }
	private Entity LastFocusedEntity;

	private bool isSelecting = false;
	private Vector3 mouseStartPosition;

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
		rtsCamera = FindObjectOfType<RTSCamera>();
		clientNetworkManager = ClientNetworkManager.Instance;
		clientGameManager = ClientGameManager.Instance;
		clientEntityManager = ClientEntityManager.Instance;
	}
	
	void Update () {
		SelectedEntities.RemoveAll(x => x == null);

		if (!EventSystem.current.IsPointerOverGameObject()) { //if not over UI element
			if (Input.GetMouseButtonDown(0)) {
				isSelecting = true;
				mouseStartPosition = Input.mousePosition;
			}

			if (Input.GetMouseButtonUp(0)) {
				isSelecting = false;
				bool isMultiselecting = Input.GetButton("Multiselect");

				if (!isMultiselecting) {
					foreach (Entity entity in SelectedEntities) {
						if (entity != null) {
							DeselectEntity(entity, false);
						}
					}
					SelectedEntities.Clear();
				}

				Bounds bounds = GetViewportBounds(mouseStartPosition, Input.mousePosition);
				if (bounds.size.x > 0.01 && bounds.size.y > 0.01) {
					//selection box is large enough, select entities inside
					IEnumerable<Entity> entitiesToConsider;
					if (clientNetworkManager.offlineTest) {
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
					if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayers)) {
						Entity hitEntity = hit.transform.gameObject.GetComponentInParent<Entity>();
						if (hitEntity != null && (clientNetworkManager.offlineTest || hitEntity.PlayerId == clientGameManager.MyPlayer.ID)) {
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
