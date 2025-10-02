using UnityEngine;
using UnityEngine.AI;

public class PlayerFollower : MonoBehaviour
{
    public enum BehaviorMode { Stay, Follow }
    [Header("AI Behavior Settings")]
    public BehaviorMode behaviorMode = BehaviorMode.Stay;

    [Tooltip("Key to toggle behavior mode (Stay/Follow).")]
    public KeyCode toggleKey = KeyCode.B;

    [Tooltip("How close the follower should stay to target.")]
    public float followDistance = 2f;

    private NavMeshAgent agent;
    private Transform followTarget;
    private bool isActivePlayer = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("PlayerFollower requires a NavMeshAgent component!");
        }
    }

    private void Update()
    {
        if (isActivePlayer)
        {
            // If this player is currently controlled, disable AI completely
            if (agent != null) agent.enabled = false;
            return;
        }

        // Toggle Stay/Follow mode
        if (Input.GetKeyDown(toggleKey))
        {
            behaviorMode = (behaviorMode == BehaviorMode.Stay) ? BehaviorMode.Follow : BehaviorMode.Stay;
        }

        if (behaviorMode == BehaviorMode.Follow && followTarget != null && agent != null)
        {
            agent.enabled = true;

            float distance = Vector3.Distance(transform.position, followTarget.position);
            if (distance > followDistance)
            {
                agent.SetDestination(followTarget.position);
            }
            else
            {
                agent.ResetPath();
            }
        }
        else if (agent != null && agent.enabled)
        {
            agent.ResetPath();
        }
    }

    public void SetActive(bool active, Transform newTarget = null)
    {
        isActivePlayer = active;
        followTarget = newTarget;

        if (active && agent != null)
        {
            agent.ResetPath();
            agent.enabled = false; // controlled by player, not AI
        }
    }
}
