// PointAndClickController.cs
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickController : MonoBehaviour
{
    public Camera cam;                      // assign Main Camera in inspector (or leave null to use Camera.main)
    public LayerMask clickableLayers = ~0;   // set this to ground/interactive layers
    public float navMeshSampleRadius = 1f;   // how far to search for nearest navmesh point
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (cam == null) cam = Camera.main;
        if (cam == null) Debug.LogWarning("No Camera assigned and Camera.main is null.");
    }

    void Update()
    {
        // ignore clicks over UI
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                Vector3 target = hit.point;

                // Try to sample a point on the NavMesh near the clicked position.
                // This prevents SetDestination throwing errors for off-mesh clicks.
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(target, out navHit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    SetAgentDestination(navHit.position);
                }
                else
                {
                    // fallback: try SetDestination directly (agent may try to get as close as possible)
                    SetAgentDestination(target);
                }
            }
        }
    }

    void SetAgentDestination(Vector3 position)
    {
        if (agent == null) return;
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("NavMeshAgent is not on a NavMesh. Ensure NavMesh is baked and agent is placed correctly.");
            return;
        }

        // Option A: simple
        agent.SetDestination(position);

        // Option B (safer): pre-calculate path to make sure it's reachable
        // NavMeshPath path = new NavMeshPath();
        // if (NavMesh.CalculatePath(agent.transform.position, position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
        //     agent.SetPath(path);
    }

    // helper to check arrival (call from elsewhere)
    public bool HasReachedDestination()
    {
        if (agent == null) return true;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
    }
}
