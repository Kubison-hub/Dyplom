using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;

    [Header("Click Settings")]
    public LayerMask clickableLayers = ~0;
    public float navMeshSampleRadius = 1f;

    private NavMeshAgent agent;
    private bool inputEnabled = true; // Prywatna zmienna

    // --- TO JEST BRAKUJ¥CA CZÊŒÆ, KTÓRA NAPRAWI B£¥D ---
    public bool IsInputEnabled
    {
        get { return inputEnabled; }
    }
    // ----------------------------------------------------

    // Przechowuje stan obrotu postaci (zak³adamy, ¿e domyœlnie patrzy w LEWO)
    private bool isFacingRight = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        if (cam == null) cam = Camera.main;
        if (cam == null) Debug.LogWarning("No Camera assigned and Camera.main is null.");
    }

    void Update()
    {
        if (!inputEnabled) return;

        // Ignoruj klikniêcia na UI
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                // 1. SprawdŸ, czy klikniêty obiekt to dŸwignia (Lever)
                Lever lever = hit.collider.GetComponent<Lever>();

                if (lever != null)
                {
                    // Próba interakcji z dŸwigni¹ zamiast ruchu
                    lever.AttemptInteraction(transform);
                }
                else
                {
                    // 2. Standardowe poruszanie siê
                    Vector3 target = hit.point;

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
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // --- Obracanie Sprite'a (Billboard + Flip) ---
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
                isFacingRight = true; // Idzie w prawo
            else if (dot < -0.1f)
                isFacingRight = false; // Idzie w lewo
        }

        // Ustawienie rotacji
        transform.rotation = baseRotation;

        // Odwrócenie sprite'a (zak³adaj¹c, ¿e domyœlny sprite patrzy w lewo)
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