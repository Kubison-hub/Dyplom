using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_EthelPassageDoor : Lvl3InteractionDialogueBase
{
    [Header("Passage Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] firstPassageDialogueLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Tutaj zamyka sie ten rozdzial, Watsonie. Musze isc na gore poszukac Malej Ethel. Czuje, ze ona jest kluczem do zagadki.",
            duration = 5f
        }
    };

    [SerializeField] private Lvl3DialogueLine[] repeatedPassageDialogueLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Tedy nie przejde. Musze poszukac Malej Ethel na gorze.",
            duration = 3f
        }
    };

    [Header("After Gramophone Dialogue")]
    [SerializeField] private Int_GramophoneController gramophoneController;
    [SerializeField] private Lvl3DialogueLine[] afterGramophoneDialogueLines;

    [Header("Stairs Progress")]
    [SerializeField] private Int_StairsUp stairsUp;

    private bool hasBeenUsed;

    [Header("Next Interactions")]
    [Tooltip("Whole GameObjects enabled after the first use of this passage door.")]
    [FormerlySerializedAs("nextInteraction")]
    [SerializeField] private GameObject[] nextInteractions;

    [Tooltip("Escort components enabled after the first use. Their GameObjects may remain active.")]
    [SerializeField] private WatsonEscortNPC[] nextWatsonEscortComponents;


    protected override Lvl3DialogueLine[] DefaultDialogueLines => firstPassageDialogueLines;

    private void Start()
    {
        SetupInteractable(InteractionType.Int_lv1_EthelPassageDoor);

        if (stairsUp == null)
            stairsUp = FindFirstObjectByType<Int_StairsUp>();

        if (gramophoneController == null)
            gramophoneController = FindFirstObjectByType<Int_GramophoneController>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (!hasBeenUsed)
        {
            hasBeenUsed = true;
            FindFirstObjectByType<SherlockWatsonHintConditions>(FindObjectsInactive.Include)?
                .MarkEthelPassageDoorUsed();
            GetComponent<Interactable>()?.MarkCompleted();
            ActivateStairsGoal();
            CluesLog.Instance?.SetFindEthelUpstairsObjective();
            PlayDialogue(player, GetAvailableDialogue(firstPassageDialogueLines));
            ActivateNextInteractions();
            return;
        }

        PlayDialogue(player, GetAvailableDialogue(repeatedPassageDialogueLines));
    }

    private Lvl3DialogueLine[] GetAvailableDialogue(Lvl3DialogueLine[] fallback)
    {
        bool gramophoneDialogueCompleted = gramophoneController != null &&
                                           gramophoneController.ConversationCompleted;
        return gramophoneDialogueCompleted && afterGramophoneDialogueLines != null &&
               afterGramophoneDialogueLines.Length > 0
            ? afterGramophoneDialogueLines
            : fallback;
    }

    private void ActivateStairsGoal()
    {
        if (stairsUp == null)
        {
            Debug.LogWarning($"{name}: Int_StairsUp is not assigned.", this);
            return;
        }

        stairsUp.isSherlockWantToGoUpstairs = true;
    }

    private void ActivateNextInteractions()
    {
        foreach (GameObject nextInteraction in nextInteractions)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }

        foreach (WatsonEscortNPC escortComponent in nextWatsonEscortComponents)
        {
            if (escortComponent != null)
                escortComponent.enabled = true;
        }
    }
}
