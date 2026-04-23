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
    public SmartNPC npcDoOdwroceniaUwagi;

    private Interactable interactable;
    private PlayerInput playerInput;
    private Animator gramophoneAnimator;
    public bool isPlaying = false;

    public Transform interactionPoint;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        gramophoneAnimator = GetComponent<Animator>();
    }

    public void Interact(PlayerController player)
    {
        player.currentInteractable = interactable;
        player.currentInteractionPoint = interactionPoint;

        player.MoveToInteractable();
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
            PlayerTopText.Instance.ShowTopText("Wygl¹da na to, ¿e duchy znowu nawiedzaj¹ dom", "Porozmawiajmy z Madame Selma");
        }

        // 3. Odblokowanie notatki
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.UnlockNote(noteIDToUnlock);
        }

        // 4. KLUCZOWE: Uruchomienie NPC (Tego brakowa³o w Twoim wklejonym kodzie)
        //if (npcDoOdwroceniaUwagi != null)
        //{
        //    npcDoOdwroceniaUwagi.GoToPoint();
        //}
        //else
        //{
        //    Debug.LogWarning("Gramophone: Nie przypisano NPC do pola 'npcDoOdwroceniaUwagi'!");
        //}
    }

    
}