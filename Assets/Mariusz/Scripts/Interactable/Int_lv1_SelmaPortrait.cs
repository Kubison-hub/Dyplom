using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SelmaPortrait : Lvl3InteractionDialogueBase
{
    [Header("Character Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine sherlockDialogueLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        duration = 3f
    };

    [SerializeField] private Lvl3DialogueLine watsonDialogueLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Watson,
        duration = 3f
    };

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();
    private void Awake() => Setup();

    public void PerformInteraction(PlayerController player)
    {
        bool isWatson = IsWatson(player);
        Lvl3DialogueLine selectedLine = isWatson
            ? watsonDialogueLine
            : sherlockDialogueLine;

        if (!isWatson)
            GetComponent<Interactable>()?.AddAllDatabaseNotes();

        PlayDialogue(player, new[] { selectedLine });
    }

    public void CompleteInteraction()
    {
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.MarkCompleted();
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        foreach (Collider interactionCollider in GetComponents<Collider>())
            interactionCollider.enabled = false;
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv1_SelmaPortrait);
        Interactable interactable = GetComponent<Interactable>();
        if (interactable == null)
            return;

        interactable.SetWatsonInteractionAllowed(true);
        interactable.addDatabaseNotesAutomatically = false;
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson ||
                player.CompareTag("PlayerB") ||
                (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1));
    }
}
