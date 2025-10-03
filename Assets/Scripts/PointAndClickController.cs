// PointAndClickController.cs
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                       // assign Main Camera in inspector (or leave null to use Camera.main)

    [Header("Click Settings")]
    public LayerMask clickableLayers = ~0;   // set this to ground/interactive layers
    public float navMeshSampleRadius = 1f;   // how far to search for nearest navmesh point

    private NavMeshAgent agent;
    private bool inputEnabled = true;        // controlled externally by PlayerSwitcher or FollowerAI

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (cam == null) cam = Camera.main;
        if (cam == null) Debug.LogWarning("No Camera assigned and Camera.main is null.");
    }

    void Update()
    {
        if (!inputEnabled) return; // inactive or AI controlling

        // ignore clicks over UI
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                Vector3 target = hit.point;

                // Try to snap to nearest NavMesh point
                if (NavMesh.SamplePosition(target, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    SetAgentDestination(navHit.position);
                }
                else
                {
                    SetAgentDestination(target); // fallback
                }
            }
        }
    }

    public void SetAgentDestination(Vector3 position)
    {
        if (agent == null) return;
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}'s NavMeshAgent is not on a NavMesh.");
            return;
        }

        agent.SetDestination(position);
    }

    public bool HasReachedDestination()
    {
        if (agent == null) return true;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance &&
               (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
    }

    // === External control ===
    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }
}
