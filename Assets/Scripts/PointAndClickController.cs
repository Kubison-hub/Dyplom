using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickController : MonoBehaviour
{
    [Header("Character Settings")]
    [Tooltip("Zaznacz TO TYLKO dla postaci 2D (Sprite). Dla kostki 3D odznacz!")]
    public bool is2DCharacter = false; // <--- NOWY PRZE£¥CZNIK

    [Header("References")]
    public Camera cam;

    [Header("Click Settings")]
    public LayerMask clickableLayers = ~0;
    public float navMeshSampleRadius = 1f;

    private NavMeshAgent agent;
    private bool inputEnabled = true;

    public bool IsInputEnabled
    {
        get { return inputEnabled; }
    }

    // Przechowuje stan obrotu postaci (zak³adamy, ¿e domyœlnie patrzy w LEWO)
    private bool isFacingRight = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // <--- ZMIANA: Wy³¹czamy rotacjê agenta TYLKO jeœli to postaæ 2D
        if (is2DCharacter)
        {
            agent.updateRotation = false;
        }
        else
        {
            agent.updateRotation = true; // Postaæ 3D niech obraca siê sama
        }

        if (cam == null) cam = Camera.main;
        if (cam == null) Debug.LogWarning("No Camera assigned and Camera.main is null.");
    }

    void Update()
    {
        if (!inputEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                // 1. SprawdŸ dŸwigniê
                Lever lever = hit.collider.GetComponent<Lever>();

                if (lever != null)
                {
                    lever.AttemptInteraction(transform);
                }
                else
                {
                    // 2. Ruch
                    Vector3 target = hit.point;
                    if (NavMesh.SamplePosition(target, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
                    {
                        SetAgentDestination(navHit.position);
                    }
                    else
                    {
                        SetAgentDestination(target);
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        // <--- ZMIANA: Jeœli to postaæ 3D, w ogóle nie ruszaj rotacji w skrypcie!
        // Niech NavMeshAgent robi swoj¹ robotê.
        if (!is2DCharacter) return;

        if (cam == null) return;

        // --- PONI¯EJ KOD TYLKO DLA POSTACI 2D ---
        Vector3 cameraForward = cam.transform.forward;
        cameraForward.y = 0;
        Quaternion baseRotation = Quaternion.LookRotation(cameraForward.normalized);

        Vector3 velocity = agent.velocity;
        velocity.y = 0;

        if (velocity.sqrMagnitude > 0.1f)
        {
            Vector3 cameraRight = cam.transform.right;
            cameraRight.y = 0;
            float dot = Vector3.Dot(velocity.normalized, cameraRight.normalized);

            if (dot > 0.1f)
                isFacingRight = true;
            else if (dot < -0.1f)
                isFacingRight = false;
        }

        transform.rotation = baseRotation;

        if (isFacingRight)
        {
            transform.rotation *= Quaternion.Euler(0, 180, 0);
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

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }
}