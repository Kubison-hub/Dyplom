using UnityEngine;
using UnityEngine.AI;

public class PlayerFollower : MonoBehaviour
{
    public enum FollowMode { Stay, Follow }
    public FollowMode mode = FollowMode.Stay;

    [Header("References")]
    public Transform target;            // ustawiany przez PlayerSwitcher
    private NavMeshAgent agent;
    private PointAndClickController clickController;

    [Header("Settings")]
    public float followDistance = 2f;     // jak blisko pod¹¿a

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        clickController = GetComponent<PointAndClickController>();
    }

    void Update()
    {
        // --- LOGIKA KORYGUJ¥CA ---

        // <--- POPRAWIONA LINIA ---
        // U¿ywamy publicznej w³aœciwoœci 'IsInputEnabled' zamiast 'inputEnabled'
        if (clickController != null && clickController.IsInputEnabled)
        // --- KONIEC POPRAWKI ---
        {
            // Jeœli sterowanie jest w³¹czone, A my wci¹¿ myœlimy, ¿e jesteœmy w trybie Follow...
            if (mode == FollowMode.Follow)
            {
                // To jest b³¹d! Natychmiast prze³¹cz na Stay i zatrzymaj agenta.
                mode = FollowMode.Stay;
                if (agent != null && agent.isOnNavMesh)
                    agent.ResetPath();

                Debug.LogWarning($"{name} zosta³ aktywowany przez Switcher bêd¹c w trybie Follow. Wymuszam tryb STAY.");
            }
        }

        // Twoja dotychczasowa logika
        if (mode == FollowMode.Follow && target != null)
        {
            // wy³¹cz klik sterowanie (bo ta postaæ NIE jest aktywna)
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
            // Pozwól na klik sterowanie. 
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