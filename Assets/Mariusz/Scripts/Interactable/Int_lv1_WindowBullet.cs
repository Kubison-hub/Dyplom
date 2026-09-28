using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_WindowBullet : Lvl3InteractionDialogueBase
{
    [Header("Window Clue")]
    [SerializeField] private GameObject clueToFind;
    [SerializeField] private DetectiveIdeaPoint windowBulletIdeaPoint;
    [Header("Window Examination Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] windowExaminationDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Kula utkwiła w oknie. Jej ślad cierpliwie zdradza kierunek strzału.",
            duration = 3f
        }
    };

    [Header("First Use Tutorial Popup")]
    [SerializeField] private bool showTutorialPopupOnFirstUse = true;
    [SerializeField] private string tutorialPopupTitle;
    [SerializeField, TextArea] private string tutorialPopupText;
    [SerializeField] private VideoClip tutorialPopupVideoClip;

    [Header("Examination Zone")]
    [SerializeField, Min(0.1f)] private float examinationZoneRange = 1.5f;
    [SerializeField] private Vector3 examinationZoneOffset;

    private Interactable interactable;
    private Collider interactionCollider;
    private PlayerController examiningPlayer;
    private GameObject examinationZone;
    private bool isExamining;
    private bool isInsideExaminationZone;
    private bool clueFound;
    private bool completed;
    private bool tutorialPopupShown;
    private bool mainInteractionDisabled;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => windowExaminationDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_WindowBullet);

        if (windowBulletIdeaPoint != null)
            windowBulletIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;

        if (clueToFind != null)
            clueToFind.SetActive(true);

        if (interactable.IsCompleted)
            DisableMainInteraction();
    }

    private void Update()
    {
        if (!mainInteractionDisabled && interactable != null && interactable.IsCompleted)
            DisableMainInteraction();

        if (!isExamining || completed || examiningPlayer == null)
            return;

        if (clueFound)
        {
            CompleteInteraction();
            return;
        }

        bool playerIsNowInside = IsPlayerInsideExaminationZone();
        if (playerIsNowInside == isInsideExaminationZone)
            return;

        isInsideExaminationZone = playerIsNowInside;
        if (!isInsideExaminationZone)
            ResumeInteractionAfterExit();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed || isExamining || player == null || interactable != null && interactable.IsCompleted)
            return;

        examiningPlayer = player;
        isExamining = true;
        clueFound = false;

        PlayDialogue(player, windowExaminationDialogue);
        if (windowExaminationDialogue == null || windowExaminationDialogue.Length == 0)
            ShowFirstUseTutorialPopup();

        interactable?.MarkCompleted();
        DisableMainInteraction();

        CreateExaminationZone();
        isInsideExaminationZone = IsPlayerInsideExaminationZone();
    }

    public void RegisterWindowClue()
    {
        if (completed)
            return;

        clueFound = true;
        windowBulletIdeaPoint?.RevealFromExternalSource();
        CompleteInteraction();
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (lines == windowExaminationDialogue)
            ShowFirstUseTutorialPopup();
    }

    private void ShowFirstUseTutorialPopup()
    {
        if (!showTutorialPopupOnFirstUse || tutorialPopupShown)
            return;

        TutorialTimeline tutorialTimeline = TutorialTimeline.Instance;
        if (tutorialTimeline == null)
            return;

        tutorialPopupShown = true;
        tutorialTimeline.ShowGameplayTutorialPopup(
            tutorialPopupTitle,
            tutorialPopupText,
            tutorialPopupVideoClip);
    }

    private void ResumeInteractionAfterExit()
    {
        isExamining = false;
        examiningPlayer = null;
        isInsideExaminationZone = false;

        if (examinationZone != null)
        {
            Destroy(examinationZone);
            examinationZone = null;
        }

        DisableMainInteraction();
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
        interactable?.MarkCompleted();
        isExamining = false;
        examiningPlayer = null;

        if (examinationZone != null)
        {
            Destroy(examinationZone);
            examinationZone = null;
        }

        DisableMainInteraction();
        if (interactable != null)
            interactable.interactiveShader = null;
    }

    private void DisableMainInteraction()
    {
        mainInteractionDisabled = true;

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
    }

    private void CreateExaminationZone()
    {
        if (examinationZone != null)
            Destroy(examinationZone);

        Transform zoneAnchor = interactable != null && interactable.interactabePoint != null
            ? interactable.interactabePoint
            : transform;

        examinationZone = new GameObject("WindowBullet_ExaminationZone");
        examinationZone.transform.SetParent(zoneAnchor, false);
        examinationZone.transform.localPosition = examinationZoneOffset;
        examinationZone.layer = LayerMask.NameToLayer("Ignore Raycast");

        SphereCollider zoneCollider = examinationZone.AddComponent<SphereCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.radius = examinationZoneRange;
    }

    private bool IsPlayerInsideExaminationZone()
    {
        if (examiningPlayer == null || examinationZone == null)
            return false;

        return Vector3.Distance(examiningPlayer.transform.position, examinationZone.transform.position) <= examinationZoneRange;
    }
}
