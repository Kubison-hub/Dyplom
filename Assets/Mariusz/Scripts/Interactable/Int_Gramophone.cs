using UnityEngine;

[RequireComponent(typeof(Interactable))]
public sealed class Int_Gramophone : Lvl3InteractionDialogueBase
{
    [Header("Sherlock Dialogue")]
    [SerializeField] private Lvl3DialogueLine sherlockFirstDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Piękny gramofon",
        duration = 3f
    };

    [Header("Gramophone Clue Point")]
    [Tooltip("Root containing the separate Int_FakeRecord interaction. It starts hidden.")]
    [SerializeField] private GameObject cluePointRoot;

    [Header("Watson Dialogue")]
    [SerializeField] private Lvl3DialogueLine watsonDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Watson,
        text = "Piękny, nowoczesny gramofon",
        duration = 3f
    };

    private Lvl3DialogueLine[] sherlockFirstLines;
    private Lvl3DialogueLine[] watsonLines;
    private bool cluePointActivated;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();

    private void Awake()
    {
        Setup();
        sherlockFirstLines = new[] { sherlockFirstDialogue };
        watsonLines = new[] { watsonDialogue };

        if (cluePointRoot != null)
            cluePointRoot.SetActive(false);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (IsWatson(player))
        {
            PlayDialogue(player, watsonLines);
            return;
        }

        if (!cluePointActivated)
        {
            cluePointActivated = true;
            if (cluePointRoot != null)
                cluePointRoot.SetActive(true);
        }

        PlayDialogue(player, sherlockFirstLines);
    }

    public bool CanPlayerUse(PlayerController player)
    {
        return IsWatson(player) || !cluePointActivated;
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_Gramophone);
        GetComponent<Interactable>()?.SetWatsonInteractionAllowed(true);
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson ||
                player.CompareTag("PlayerB") ||
                SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1);
    }
}
