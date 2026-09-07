using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_lamp : Lvl3InteractionDialogueBase
{
    [SerializeField] private DetectiveIdeaPoint lampIdeaPoint;
    [Header("Lamp Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] lampDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ta lampa mogla oswietlic wiecej niz tylko zakurzony pokoj.",
            duration = 3f
        }
    };

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;
    private bool completed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => lampDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_lamp);

        if (lampIdeaPoint != null)
        {
            lampIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.MagnifierOrExternal;
            lampIdeaPoint.OnDiscovered += HandleIdeaPointDiscovered;

            if (lampIdeaPoint.IsDiscovered)
                CompleteInteraction();
        }
    }

    private void OnDestroy()
    {
        if (lampIdeaPoint != null)
            lampIdeaPoint.OnDiscovered -= HandleIdeaPointDiscovered;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed)
            return;

        performed = true;
        PlayDialogue(player, lampDialogue);

        lampIdeaPoint?.RevealFromExternalSource();
        CompleteInteraction();
    }

    private void HandleIdeaPointDiscovered(DetectiveIdeaPoint discoveredPoint)
    {
        performed = true;
        CompleteInteraction();
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
        performed = true;
        interactable?.MarkCompleted();

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;

    }
}
