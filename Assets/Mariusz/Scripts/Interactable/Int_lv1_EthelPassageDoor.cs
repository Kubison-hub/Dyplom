using UnityEngine;

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

    [Header("Stairs Progress")]
    [SerializeField] private Int_StairsUp stairsUp;

    private bool hasBeenUsed;

    [SerializeField] private GameObject nextInteraction;


    protected override Lvl3DialogueLine[] DefaultDialogueLines => firstPassageDialogueLines;

    private void Start()
    {
        SetupInteractable(InteractionType.Int_lv1_EthelPassageDoor);

        if (stairsUp == null)
            stairsUp = FindFirstObjectByType<Int_StairsUp>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (!hasBeenUsed)
        {
            hasBeenUsed = true;
            ActivateStairsGoal();
            PlayDialogue(player, firstPassageDialogueLines);
            nextInteraction.SetActive(true);
            return;
        }

        PlayDialogue(player, repeatedPassageDialogueLines);
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
}
