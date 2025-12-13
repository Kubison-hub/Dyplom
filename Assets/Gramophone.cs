using UnityEngine;
using UnityEngine.InputSystem;

public class Gramophone : MonoBehaviour
{
    [Header("Ustawienia NPC/Interakcji")]
    public float interactionPointOffset = 1f;

    [Header("Ustawienia Dziennika (NOWE)")]
    // Numer notatki w JournalManager, która ma siê odkryæ po w³¹czeniu muzyki.
    // Domyœlnie 0 (czyli pierwszy element z listy Unlockable Notes).
    public int noteIDToUnlock = 0;

    private Interactable interactable;
    private PlayerInput playerInput;
    private Animator gramophoneAnimator;
    public bool isPlaying = false;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        gramophoneAnimator = GetComponent<Animator>();
    }

    // Wywo³ywane przez Interactable (Faza 1: PodejdŸ)
    public void Interact(PlayerController player)
    {
        MovePlayerToInteractionPoint(player);
        playerInput = player.GetComponent<PlayerInput>();
    }

    // Wywo³ywane przez Interactable (Faza 2: Wykonaj akcjê)
    public void PerformInteraction()
    {
        Debug.Log("Perform Interaction " + gameObject.name);
        PlayGram();
    }

    private void PlayGram()
    {
        if (isPlaying) return;

        // 1. Oryginalna logika (Animacja + Tekst u góry)
        if (gramophoneAnimator != null)
        {
            gramophoneAnimator.SetTrigger("Play");
        }

        isPlaying = true;

        if (PlayerTopText.Instance != null)
        {
            PlayerTopText.Instance.ShotTopText("Wygl¹da na to, ¿e duchy znowu nawiedzaj¹ dom", "Porozmawiajmy z Madame Selma");
        }

        // 2. NOWOŒÆ: Odblokowanie notatki w dzienniku
        if (JournalManager.Instance != null)
        {
            // Odkrywamy notatkê o numerze wskazanym w Inspectorze (domyœlnie 0)
            JournalManager.Instance.UnlockNote(noteIDToUnlock);
        }
        else
        {
            Debug.LogWarning("Gramophone: Brak JournalManager na scenie! Notatka nie zosta³a dodana.");
        }
    }

    // --- PONI¯EJ ORYGINALNY KOD RUCHU (BEZ ZMIAN) ---

    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Vector3 npcPos = transform.position;
        Vector3 playerPos = player.transform.position;

        // kierunek od gracza do NPC (na p³aszczyŸnie)
        Vector3 dirToNpc = npcPos - playerPos;
        dirToNpc.y = 0f;

        float dist = dirToNpc.magnitude;
        if (dist < 0.001f)
            dirToNpc = transform.forward;      // awaryjnie
        else
            dirToNpc /= dist;                  // normalize

        float stopDistance = interactionPointOffset;

        // punkt: "na linii do NPC, ale w odleg³oœci stopDistance od NPC"
        Vector3 targetPosition = npcPos - dirToNpc * stopDistance;
        targetPosition.y = playerPos.y;

        // jeœli punkt jest zajêty, spróbuj bokiem wzglêdem kierunku podejœcia
        if (!CheckPositionEmpty(targetPosition, 0.5f, player.gameObject))
        {
            Vector3 right = Vector3.Cross(Vector3.up, dirToNpc);  // prostopadle do kierunku podejœcia

            Vector3 t1 = targetPosition + right * 1f;
            Vector3 t2 = targetPosition - right * 1f;

            if (CheckPositionEmpty(t1, 0.5f, player.gameObject)) targetPosition = t1;
            else if (CheckPositionEmpty(t2, 0.5f, player.gameObject)) targetPosition = t2;
            // else zostaje oryginalny target
        }

        player.currentInteractable = interactable;
        player.targetPosition = targetPosition;

        // jeœli ju¿ jest wystarczaj¹co blisko, nie ka¿ mu iœæ
        player.isWalking = (dist > stopDistance + 0.05f);
        Debug.Log("Done");
    }

    private bool CheckPositionEmpty(Vector3 position, float radius, GameObject ignoreObject = null)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius);

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;

            if (ignoreObject != null && hit.gameObject == ignoreObject) continue;

            if (hit.GetComponent<PlayerController>() != null)
            {
                return false;
            }
        }

        return true;
    }
}