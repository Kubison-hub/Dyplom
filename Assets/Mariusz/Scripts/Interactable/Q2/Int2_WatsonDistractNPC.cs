using TMPro;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.AI;

public class Int2_WatsonDistractNPC : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    public bool isOnDistraction = false;

    private Transform cardPosition;

    public GameObject npcGO;
    private NavMeshAgent agent;
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float followUpdateRate = 0.1f;
    [SerializeField] private bool followOnRightSide = true;
    [SerializeField] private float sideDistance = 1.2f;
    public GameObject watsonGO;

    public float NPCRotationSpeed = 5f;

    public bool isNpcFollowing = false;
    private float followTimer;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        npcGO = GameObject.Find("SELMA_NPC");
        agent = npcGO.GetComponent<NavMeshAgent>();

        transform.parent = npcGO.transform;
        transform.position = npcGO.transform.position;
        transform.rotation = npcGO.transform.localRotation;


        watsonGO = GameObject.Find("Watson");

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

    }

    private void Update()
    {
        if (isOnDistraction)
        {
            //NpcFollowWatson();
            LookAtWatson();

        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void PerformInteraction(PlayerController player)
    {
        isOnDistraction = true;
    }


    public void LookAtWatson()
    {
        Vector3 direction = watsonGO.transform.position - npcGO.transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            npcGO.transform.rotation = Quaternion.Lerp(
                npcGO.transform.rotation,
                lookRotation,
                Time.deltaTime * NPCRotationSpeed
            );
        }
    }

    public void NpcFollowWatson()
    {
        if (agent == null || watsonGO == null)
            return;

        followTimer += Time.deltaTime;
        if (followTimer < followUpdateRate)
            return;

        followTimer = 0f;

        Vector3 sideDir = followOnRightSide ? watsonGO.transform.right : -watsonGO.transform.right;
        Vector3 targetPosition = watsonGO.transform.position + sideDir * sideDistance;

        float distance = Vector3.Distance(npcGO.transform.position, targetPosition);

        agent.stoppingDistance = stopDistance;

        if (distance > followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

}

