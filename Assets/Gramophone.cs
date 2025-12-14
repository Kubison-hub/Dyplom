using UnityEngine;
using UnityEngine.InputSystem;

public class Gramophone : MonoBehaviour
{
    [Header("Ustawienia NPC/Interakcji")]
    public float interactionPointOffset = 1f;

    [Header("Ustawienia Dziennika")]
    // Numer notatki w JournalManager (domyœlnie 0)
    public int noteIDToUnlock = 0;

    [Header("Odwracanie Uwagi (Ruch po punktach)")]
    // Tutaj musimy mieæ odniesienie do skryptu NPCWaypointMover!
    public NPCWaypointMover npcDoOdwroceniaUwagi;

    private Interactable interactable;
    private PlayerInput playerInput;
    private Animator gramophoneAnimator;
    public bool isPlaying = false;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        gramophoneAnimator = GetComponent<Animator>();
    }

    public void Interact(PlayerController player)
    {
        MovePlayerToInteractionPoint(player);
        playerInput = player.GetComponent<PlayerInput>();
    }

    public void PerformInteraction()
    {
        Debug.Log("Perform Interaction " + gameObject.name);
        PlayGram();
    }

    private void PlayGram()
    {
        if (isPlaying) return;

        // 1. Animacja i dŸwiêk
        if (gramophoneAnimator != null) gramophoneAnimator.SetTrigger("Play");
        isPlaying = true;

        // 2. Tekst u góry ekranu
        if (PlayerTopText.Instance != null)
        {
            PlayerTopText.Instance.ShotTopText("Wygl¹da na to, ¿e duchy znowu nawiedzaj¹ dom", "Porozmawiajmy z Madame Selma");
        }

        // 3. Odblokowanie notatki
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.UnlockNote(noteIDToUnlock);
        }

        // 4. KLUCZOWE: Uruchomienie NPC (Tego brakowa³o w Twoim wklejonym kodzie)
        if (npcDoOdwroceniaUwagi != null)
        {
            npcDoOdwroceniaUwagi.ZacznijWedrowke();
        }
        else
        {
            Debug.LogWarning("Gramophone: Nie przypisano NPC do pola 'npcDoOdwroceniaUwagi'!");
        }
    }

    // --- KOD RUCHU GRACZA (BEZ ZMIAN) ---
    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Vector3 npcPos = transform.position;
        Vector3 playerPos = player.transform.position;
        Vector3 dirToNpc = npcPos - playerPos;
        dirToNpc.y = 0f;
        float dist = dirToNpc.magnitude;
        if (dist < 0.001f) dirToNpc = transform.forward;
        else dirToNpc /= dist;
        float stopDistance = interactionPointOffset;
        Vector3 targetPosition = npcPos - dirToNpc * stopDistance;
        targetPosition.y = playerPos.y;
        if (!CheckPositionEmpty(targetPosition, 0.5f, player.gameObject))
        {
            Vector3 right = Vector3.Cross(Vector3.up, dirToNpc);
            Vector3 t1 = targetPosition + right * 1f;
            Vector3 t2 = targetPosition - right * 1f;
            if (CheckPositionEmpty(t1, 0.5f, player.gameObject)) targetPosition = t1;
            else if (CheckPositionEmpty(t2, 0.5f, player.gameObject)) targetPosition = t2;
        }
        player.currentInteractable = interactable;
        player.targetPosition = targetPosition;
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
            if (hit.GetComponent<PlayerController>() != null) return false;
        }
        return true;
    }
}