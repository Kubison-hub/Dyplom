using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rejects NavMesh paths that visibly cross colliders on the Walls layer.
/// This is a runtime safeguard; the NavMesh still needs to be baked with Walls included.
/// </summary>
public static class NavMeshWallGuard
{
    private const string WallsLayerName = "Walls";

    public static bool TryGetClearPath(NavMeshAgent agent, Vector3 destination, out NavMeshPath path)
    {
        path = new NavMeshPath();
        if (agent == null || !agent.isOnNavMesh ||
            !agent.CalculatePath(destination, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        int wallsLayer = LayerMask.NameToLayer(WallsLayerName);
        if (wallsLayer < 0 || path.corners == null || path.corners.Length < 2)
            return true;

        int wallsMask = 1 << wallsLayer;
        float castRadius = Mathf.Max(0.03f, agent.radius * 0.8f);

        for (int index = 1; index < path.corners.Length; index++)
        {
            Vector3 start = path.corners[index - 1] + Vector3.up * agent.baseOffset;
            Vector3 end = path.corners[index] + Vector3.up * agent.baseOffset;
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (distance > 0.001f &&
                Physics.SphereCast(
                    start,
                    castRadius,
                    direction / distance,
                    out _,
                    distance,
                    wallsMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }

        return true;
    }
}
