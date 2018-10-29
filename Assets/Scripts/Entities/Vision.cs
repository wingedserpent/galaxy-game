using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Vision : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;
    public float viewDepth;
	
	public List<Entity> VisibleTargets { get; private set; }

    public int meshResolution;
    public int edgeResolveIterations;
    public float edgeDstThreshold;

    public bool debug;

    private LayerMask visionMask;
	private LayerMask obstacleMask;

    public MeshFilter viewMeshFilter; //public so Unity assigns it a default value?
    private Mesh viewMesh;

	private bool isClient = false;

	private VisibilityManager visibilityManager;
	private Entity selfEntity;
	private Collider myVisionCollider;

	void Awake() {
		VisibleTargets = new List<Entity>();
	}

	void Start()
    {
		visibilityManager = VisibilityManager.Instance;
		selfEntity = GetComponentInParent<Entity>();
		myVisionCollider = GetComponentsInChildren<Collider>().Where(x => visionMask == (visionMask | (1 << x.gameObject.layer))).FirstOrDefault();

		visionMask = LayerManager.Instance.visionTargetMask;
		obstacleMask = LayerManager.Instance.visionBlockerMask;

		isClient = NetworkStatus.Instance.IsClient;

		if (isClient) {
			viewMesh = new Mesh { name = "View Mesh" };
			viewMeshFilter.mesh = viewMesh;
		}
		
		StartCoroutine("FindTargetsWithDelay", .2f);
    }

	IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void LateUpdate()
    {
		if (isClient) {
			DrawFieldOfView();
		}
    }

    void FindVisibleTargets()
    {
		VisibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, visionMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
			Entity target = targetsInViewRadius[i].GetComponentInParent<Entity>();
			if (target != null && target.TeamId != selfEntity.TeamId) {
				Collider targetVisionCollider = target.GetComponentsInChildren<Collider>()
					.Where(x => visionMask == (visionMask | (1 << x.gameObject.layer))).FirstOrDefault();
				Vector3 targetPosition = targetVisionCollider != null ? targetVisionCollider.bounds.center : target.transform.position;
				Vector3 myPosition = myVisionCollider != null ? myVisionCollider.bounds.center : transform.position;

				Vector3 dirToTarget = (targetPosition - myPosition).normalized;
				if (Vector3.Angle(transform.forward, dirToTarget) <= viewAngle / 2) {
					float dstToTarget = Vector3.Distance(myPosition, targetPosition);
					if (!Physics.Raycast(myPosition, dirToTarget, dstToTarget, obstacleMask)) {
						VisibleTargets.Add(target);
					}
				}
			}
        }

		if (isClient && VisibleTargets.Count > 0) {
			visibilityManager.AddVisibleTargets(VisibleTargets);
		}
	}

    void DrawFieldOfView()
    {
        float stepAngleSize = viewAngle / meshResolution;
        List<Vector3> viewPoints = new List<Vector3>();
        ObstacleInfo oldObstacle = new ObstacleInfo();
        for (int i = 0; i <= meshResolution; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            ObstacleInfo newObstacle = FindObstacles(angle);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldObstacle.dst - newObstacle.dst) > edgeDstThreshold;
                if (oldObstacle.hit != newObstacle.hit ||
                    oldObstacle.hit && edgeDstThresholdExceeded)
                {
                    EdgeInfo edge = FindEdge(oldObstacle, newObstacle);
                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }


            viewPoints.Add(newObstacle.point);
            oldObstacle = newObstacle;
        }

        int vertexCount = viewPoints.Count + 1;
        var vertices = new Vector3[vertexCount];
        var triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();

        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }


    EdgeInfo FindEdge(ObstacleInfo minObstacle, ObstacleInfo maxObstacle)
    {
        float minAngle = minObstacle.angle;
        float maxAngle = maxObstacle.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ObstacleInfo newObstacle = FindObstacles(angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minObstacle.dst - newObstacle.dst) > edgeDstThreshold;
            if (newObstacle.hit == minObstacle.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newObstacle.point;
            } else
            {
                maxAngle = angle;
                maxPoint = newObstacle.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }


    ObstacleInfo FindObstacles(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if (DebugRayCast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new ObstacleInfo(true, hit.point + hit.normal * -viewDepth, hit.distance, globalAngle);
        }
        return new ObstacleInfo(false, transform.position + dir * (viewRadius - viewDepth), viewRadius, globalAngle);
    }

    bool DebugRayCast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance, int mask)
    {
        if (Physics.Raycast(origin, direction, out hit, maxDistance, mask))
        {
            if (debug)
                Debug.DrawLine(origin, hit.point);
            return true;
        }
        if (debug)
            Debug.DrawLine(origin, origin + direction * maxDistance);
        return false;
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool isGlobal)
    {
        if (!isGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ObstacleInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ObstacleInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
}