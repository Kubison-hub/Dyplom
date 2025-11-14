using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickController : MonoBehaviour
{
    [Header("Character Settings")]
    [Tooltip("Zaznacz dla Sprite'a 2D. Odznacz dla Cube 3D.")]
    public bool is2DCharacter = false;

    [Header("References")]
    public Camera cam;

    [Header("Click Settings")]
    public LayerMask clickableLayers = ~0;
    public float navMeshSampleRadius = 1f;

    private NavMeshAgent agent;
    private bool inputEnabled = true;

    // W≥aúciwoúÊ potrzebna dla PlayerSwitchera/Followera
    public bool IsInputEnabled => inputEnabled;

    private bool isFacingRight = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (is2DCharacter)
            agent.updateRotation = false;
        else
            agent.updateRotation = true;

        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (!inputEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Ignoruj UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                // --- 1. SPRAWDè CZY TO DèWIGNIA ---
                // Szukamy skryptu Lever na obiekcie lub jego rodzicu
                Lever lever = hit.collider.GetComponent<Lever>();
                if (lever == null) lever = hit.collider.GetComponentInParent<Lever>();

                if (lever != null)
                {
                    // To düwignia -> prÛbuj uøyÊ
                    lever.AttemptInteraction(transform);

                    // Zatrzymaj postaÊ (opcjonalnie)
                    if (agent.hasPath) agent.ResetPath();
                    return;
                }

                // --- 2. JEåLI NIE DèWIGNIA -> IDè TAM ---
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

    void LateUpdate()
    {
        // Logika obracania Sprite'a 2D
        if (!is2DCharacter) return;
        if (cam == null) return;

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

            if (dot > 0.1f) isFacingRight = true;
            else if (dot < -0.1f) isFacingRight = false;
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
        if (!agent.isOnNavMesh) return;
        agent.SetDestination(position);
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
        if (!enable && agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }
}