using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SecretDoor : Lvl3InteractionDialogueBase
{
    [Header("Secret Door Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] secretDoorDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "W jaki sposób otworzyć to tajne przejście?",
            duration = 3f
        }
    };

    [Header("Footsteps Tutorial")]
    [SerializeField, Min(0f)] private float tutorialDelay = 1f;
    [SerializeField] private string tutorialPopupTitle = "ŚLADY";
    [SerializeField, TextArea] private string tutorialText =
        "Sherlock potrafi rozpoznawać ślady. Przytrzymaj Lewy Shift, aby wejść w tryb skupienia.";
    [SerializeField] private VideoClip tutorialPopupVideoClip;
    [SerializeField] private EagleVisionScanner scanner;
    [SerializeField] private GameObject[] footprintSplines;

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;
    private bool tutorialStarted;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => secretDoorDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_SecretDoor);

        SetFootprintSplinesActive(false);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
        {
            PlayDialogue(player, secretDoorDialogue);
            return;
        }

        performed = true;
        interactable?.MarkCompleted();
        player.currentInteractable = null;

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;

        if (secretDoorDialogue != null && secretDoorDialogue.Length > 0)
            PlayDialogue(player, secretDoorDialogue);
        else
            StartFootstepsTutorial();
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (lines == secretDoorDialogue)
            StartFootstepsTutorial();
    }

    private void StartFootstepsTutorial()
    {
        if (tutorialStarted)
            return;

        tutorialStarted = true;
        StartCoroutine(RunFootstepsTutorial());
    }

    private IEnumerator RunFootstepsTutorial()
    {
        if (tutorialDelay > 0f)
            yield return new WaitForSecondsRealtime(tutorialDelay);

        TutorialTimeline tutorialTimeline = TutorialTimeline.Instance;
        if (tutorialTimeline != null)
        {
            tutorialTimeline.ShowGameplayTutorialPopup(
                tutorialPopupTitle,
                tutorialText,
                tutorialPopupVideoClip);

            while (tutorialTimeline.BlocksWorldInput)
                yield return null;
        }

        if (scanner != null)
            scanner.footPrints = true;

        SetFootprintSplinesActive(true);

        EnableRepeatableInteraction();
    }

    private void EnableRepeatableInteraction()
    {
        if (interactable != null)
            interactable.isInteractableActive = true;

        if (interactionCollider != null)
            interactionCollider.enabled = true;
    }

    private void SetFootprintSplinesActive(bool active)
    {
        foreach (GameObject footprintSpline in footprintSplines)
        {
            if (footprintSpline != null)
                footprintSpline.SetActive(active);
        }
    }
}
