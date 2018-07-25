using UnityEngine;
using System.Collections;

namespace RTSCam
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("RTS Camera")]
    public class RTSCamera : MonoBehaviour
    {

        #region Foldouts

#if UNITY_EDITOR

        public int lastTab = 0;

        public bool movementSettingsFoldout;
        public bool zoomingSettingsFoldout;
        public bool rotationSettingsFoldout;
        public bool heightSettingsFoldout;
        public bool mapLimitSettingsFoldout;
        public bool targetingSettingsFoldout;
        public bool inputSettingsFoldout;

#endif

        #endregion

        private Transform m_Transform; //camera tranform
        public bool useFixedUpdate = false; //use FixedUpdate() or Update()

        #region Movement

        public float keyboardMovementSpeed = 5f; //speed with keyboard movement
        public float screenEdgeMovementSpeed = 3f; //spee with screen edge movement
        public float followingSpeed = 5f; //speed when following a target
        public float rotationSpeed = 3f;
        public float panningSpeed = 10f;
        public float mouseRotationSpeed = 10f;

        #endregion

        #region Height

        public bool autoHeight = true;
        public LayerMask groundMask = -1; //layermask of ground or other objects that affect height

        public float minHeight = 10f; //minimum height
        public float maxHeight = 15f; //maximum height
		public float minZoom = 10f; //minimum zoom
		public float maxZoom = 15f; //maximum zoom
		public float autoHeightDampening = 5f;
        public float keyboardZoomingSensitivity = 2f;
        public float scrollWheelZoomingSensitivity = 25f;

		private float heightZoomPos = 0; //value in range (0, 1) used as t in Mathf.Lerp
		private float zoomPos = 0; //value in range (0, 1) used as t in Mathf.Lerp

        #endregion

        #region MapLimits

        public bool limitMap = true;
        public float limitX = 50f; //x limit of map
        public float limitY = 50f; //z limit of map

        #endregion

        #region Targeting

        public Transform targetToFollow; //target to follow
        public Vector3 targetOffset;

        /// <summary>
        /// are we following target
        /// </summary>
        public bool FollowingTarget
        {
            get
            {
                return targetToFollow != null;
            }
        }

        #endregion

        #region Input

        public bool useScreenEdgeInput = true;
        public float screenEdgeBorder = 25f;

        public bool useKeyboardInput = true;
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";

        public bool usePanning = true;
        public KeyCode panningKey = KeyCode.Mouse2;

        public bool useKeyboardZooming = true;
        public KeyCode zoomInKey = KeyCode.E;
        public KeyCode zoomOutKey = KeyCode.Q;

        public bool useScrollwheelZooming = true;
        public string zoomingAxis = "Mouse ScrollWheel";

        public bool useKeyboardRotation = true;
        public KeyCode rotateRightKey = KeyCode.X;
        public KeyCode rotateLeftKey = KeyCode.Z;

        public bool useMouseRotation = true;
        public KeyCode mouseRotationKey = KeyCode.Mouse1;

        private Vector2 KeyboardInput
        {
            get { return useKeyboardInput ? new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis)) : Vector2.zero; }
        }

        private Vector2 MouseInput
        {
            get { return Input.mousePosition; }
        }

        private float ScrollWheel
        {
            get { return -Input.GetAxis(zoomingAxis); }
        }

        private Vector2 MouseAxis
        {
            get { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        }

        private int ZoomDirection
        {
            get
            {
                bool zoomIn = Input.GetKey(zoomInKey);
                bool zoomOut = Input.GetKey(zoomOutKey);
                if (zoomIn && zoomOut)
                    return 0;
                else if (!zoomIn && zoomOut)
                    return 1;
                else if (zoomIn && !zoomOut)
                    return -1;
                else 
                    return 0;
            }
        }

        private int RotationDirection
        {
            get
            {
                bool rotateRight = Input.GetKey(rotateRightKey);
                bool rotateLeft = Input.GetKey(rotateLeftKey);
                if(rotateLeft && rotateRight)
                    return 0;
                else if(rotateLeft && !rotateRight)
                    return -1;
                else if(!rotateLeft && rotateRight)
                    return 1;
                else 
                    return 0;
            }
        }

        #endregion

        #region Unity_Methods

        private void Start()
        {
            m_Transform = transform;
        }

        private void Update()
        {
            if (!useFixedUpdate)
                CameraUpdate();
        }

        private void FixedUpdate()
        {
            if (useFixedUpdate)
                CameraUpdate();
        }

        #endregion

        #region RTSCamera_Methods

        /// <summary>
        /// update camera movement and rotation
        /// </summary>
        private void CameraUpdate()
        {
            if (FollowingTarget)
                FollowTarget();
            else
                Move();

            //HeightCalculation();
			ZoomCalculation();
			//RotationInPlace();
			RotationOrbit();
            LimitPosition();
        }

        /// <summary>
        /// move camera with keyboard or with screen edge
        /// </summary>
        private void Move()
        {
            if (useKeyboardInput)
            {
                Vector3 desiredMove = new Vector3(KeyboardInput.x, 0, KeyboardInput.y);

                desiredMove *= keyboardMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection(desiredMove);

                m_Transform.Translate(desiredMove, Space.Self);
            }

            if (useScreenEdgeInput)
            {
                Vector3 desiredMove = new Vector3();

                Rect leftRect = new Rect(0, 0, screenEdgeBorder, Screen.height);
                Rect rightRect = new Rect(Screen.width - screenEdgeBorder, 0, screenEdgeBorder, Screen.height);
                Rect upRect = new Rect(0, Screen.height - screenEdgeBorder, Screen.width, screenEdgeBorder);
                Rect downRect = new Rect(0, 0, Screen.width, screenEdgeBorder);

                desiredMove.x = leftRect.Contains(MouseInput) ? -1 : rightRect.Contains(MouseInput) ? 1 : 0;
                desiredMove.z = upRect.Contains(MouseInput) ? 1 : downRect.Contains(MouseInput) ? -1 : 0;

                desiredMove *= screenEdgeMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection(desiredMove);

                m_Transform.Translate(desiredMove, Space.Self);
            }       
        
            if(usePanning && Input.GetKey(panningKey) && MouseAxis != Vector2.zero)
            {
                Vector3 desiredMove = new Vector3(-MouseAxis.x, 0, -MouseAxis.y);

                desiredMove *= panningSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection(desiredMove);

                m_Transform.Translate(desiredMove, Space.Self);
            }
        }

		/*
        /// <summary>
        /// calculate height
        /// </summary>
        private void HeightCalculation()
        {
            float distanceToGround = DistanceToGround();
            if(useScrollwheelZooming)
                heightZoomPos += ScrollWheel * Time.deltaTime * scrollWheelZoomingSensitivity;
            if (useKeyboardZooming)
                heightZoomPos += ZoomDirection * Time.deltaTime * keyboardZoomingSensitivity;

            heightZoomPos = Mathf.Clamp01(heightZoomPos);

            float targetHeight = Mathf.Lerp(minHeight, maxHeight, heightZoomPos);
            float difference = 0; 

            if(distanceToGround != targetHeight)
                difference = targetHeight - distanceToGround;

            m_Transform.position = Vector3.Lerp(m_Transform.position, 
                new Vector3(m_Transform.position.x, targetHeight + difference, m_Transform.position.z), Time.deltaTime * autoHeightDampening);
        }
		*/

		/// <summary>
		/// calculate zoom
		/// </summary>
		private void ZoomCalculation()
		{
			float distanceToGround = GetForwardDistanceToGround();
			if (distanceToGround > 0f) {
				if (useScrollwheelZooming)
					zoomPos += ScrollWheel * Time.deltaTime * scrollWheelZoomingSensitivity;
				if (useKeyboardZooming)
					zoomPos += ZoomDirection * Time.deltaTime * keyboardZoomingSensitivity;

				zoomPos = Mathf.Clamp01(zoomPos);

				float targetZoom = Mathf.Lerp(minZoom, maxZoom, zoomPos);
				float difference = distanceToGround - targetZoom;
				
				m_Transform.position = Vector3.Lerp(m_Transform.position, 
					m_Transform.position + (m_Transform.forward * difference), 
					Time.deltaTime * autoHeightDampening);
			}
		}

		/// <summary>
		/// rotate camera without moving it
		/// </summary>
		private void RotationInPlace()
        {
            if(useKeyboardRotation)
                transform.Rotate(Vector3.up, RotationDirection * Time.deltaTime * rotationSpeed, Space.World);

            if (useMouseRotation && Input.GetKey(mouseRotationKey))
                m_Transform.Rotate(Vector3.up, -MouseAxis.x * Time.deltaTime * mouseRotationSpeed, Space.World);
        }

		/// <summary>
		/// rotate camera by orbiting a certain position
		/// </summary>
		private void RotationOrbit() {
			if (useKeyboardRotation)
				transform.RotateAround(FollowingTarget ? targetToFollow.position : GetForwardGroundPos(), Vector3.up, -RotationDirection * Time.deltaTime * rotationSpeed);

			if (useMouseRotation && Input.GetKey(mouseRotationKey))
				m_Transform.RotateAround(FollowingTarget ? targetToFollow.position : GetForwardGroundPos(), Vector3.up, -MouseAxis.x * Time.deltaTime * mouseRotationSpeed);
		}

		/// <summary>
		/// follow target if target != null, keeping position.y constant
		/// </summary>
		private void FollowTarget() {
			m_Transform.position = Vector3.MoveTowards(m_Transform.position, GetRefocusPosition(targetToFollow.position), Time.deltaTime * followingSpeed);
		}

		/// <summary>
		/// refocus camera, keeping position.y constant
		/// </summary>
		public void RefocusOn(Vector3 targetPos) {
			m_Transform.position = GetRefocusPosition(targetPos);
		}

		private Vector3 GetRefocusPosition(Vector3 targetPos) {
			Plane yHeight = new Plane(Vector3.down, m_Transform.position.y);
			Ray cameraBackward = new Ray(targetPos, -m_Transform.forward);
			float intersection = 0f;
			if (yHeight.Raycast(cameraBackward, out intersection)) {
				return cameraBackward.GetPoint(intersection);
			}
			return m_Transform.position;
		}

		/// <summary>
		/// limit camera position
		/// </summary>
		private void LimitPosition()
        {
            if (!limitMap)
                return;
                
            m_Transform.position = new Vector3(Mathf.Clamp(m_Transform.position.x, -limitX, limitX),
                m_Transform.position.y,
                Mathf.Clamp(m_Transform.position.z, -limitY, limitY));
        }

        /// <summary>
        /// set the target
        /// </summary>
        /// <param name="target"></param>
        public void SetFollowTarget(Transform target)
        {
			RefocusOn(target.position);
            targetToFollow = target;
        }

        /// <summary>
        /// reset the target (target is set to null)
        /// </summary>
        public void ClearFollowTarget()
        {
            targetToFollow = null;
        }
		
        /// <summary>
        /// calculate distance to ground
        /// </summary>
        /// <returns></returns>
        private float DistanceToGround()
        {
            Ray ray = new Ray(m_Transform.position, Vector3.down);
            RaycastHit hit;
			if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundMask.value))
                return (hit.point - m_Transform.position).magnitude;

            return 0f;
        }

		/// <summary>
		/// calculate distance to ground from camera forward
		/// </summary>
		/// <returns></returns>
		private float GetForwardDistanceToGround() {
			return (GetForwardGroundPos() - m_Transform.position).magnitude;
		}

		private Vector3 GetForwardGroundPos() {
			Ray forwardRay = new Ray(m_Transform.position, m_Transform.forward);
			RaycastHit hit;
			if (Physics.Raycast(forwardRay, out hit, Mathf.Infinity, groundMask.value)) {
				return hit.point;
			}

			//no ground here, simulate a plane at y=0
			Plane groundPlane = new Plane(Vector3.up, 0);
			float intersection = 0f;
			if (groundPlane.Raycast(forwardRay, out intersection)) {
				return forwardRay.GetPoint(intersection);
			}

			return Vector3.zero;
		}

		#endregion
	}
}