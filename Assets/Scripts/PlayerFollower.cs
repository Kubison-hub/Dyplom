using UnityEngine;
using UnityEngine.AI;

public class PlayerFollower : MonoBehaviour
{
    public enum FollowMode { Stay, Follow }
    public FollowMode mode = FollowMode.Stay;

    [Header("References")]
    public Transform target;                 // ustawiany przez PlayerSwitcher
    private NavMeshAgent agent;
    private PointAndClickController clickController;

    [Header("Settings")]
    public float followDistance = 2f;        // jak blisko pod¹¿a

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        clickController = GetComponent<PointAndClickController>();
    }

    void Update()
    {
        if (mode == FollowMode.Follow && target != null)
        {
            // wy³¹cz klik sterowanie
            if (clickController != null)
                clickController.EnableInput(false);

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > followDistance && agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
            }
        }
        else if (mode == FollowMode.Stay)
        {
            // zatrzymaj AI -> pozwól na klik sterowanie jeœli to postaæ aktywna
            if (clickController != null && clickController.enabled == false)
                clickController.EnableInput(false); // NIE aktywujemy sami, robi to Switcher
        }
    }

    public void ToggleMode()
    {
        if (mode == FollowMode.Stay)
        {
            mode = FollowMode.Follow;
            Debug.Log($"{name} switched to FOLLOW mode.");
        }
        else
        {
            mode = FollowMode.Stay;
            if (agent != null && agent.isOnNavMesh)
                agent.ResetPath();
            Debug.Log($"{name} switched to STAY mode.");
        }
    }
}
