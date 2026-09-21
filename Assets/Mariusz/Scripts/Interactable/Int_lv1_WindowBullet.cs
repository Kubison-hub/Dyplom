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

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    private const string ExaminationPresetName = "Narrow";

    private Interactable interactable;
    private Collider interactionCollider;
    private PlayerController examiningPlayer;
    private GameObject examinationZone;
    private bool isExamining;
    private bool isInsideExaminationZone;
    private bool isCameraInExaminationMode;
    private bool clueFound;
    private bool completed;
    private bool tutorialPopupShown;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => windowExaminationDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_WindowBullet);

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        if (windowBulletIdeaPoint != null)
            windowBulletIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;

        if (clueToFind != null)
            clueToFind.SetActive(true);
    }

    private void Update()
    {
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
        if (isInsideExaminationZone)
            EnterExaminationCamera();
        else
            ResumeInteractionAfterExit();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed || isExamining || player == null)
            return;

        examiningPlayer = player;
        isExamining = true;
        clueFound = false;

        PlayDialogue(player, windowExaminationDialogue);
        if (windowExaminationDialogue == null || windowExaminationDialogue.Length == 0)
            ShowFirstUseTutorialPopup();

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

        CreateExaminationZone();
        isInsideExaminationZone = IsPlayerInsideExaminationZone();
        if (isInsideExaminationZone)
            EnterExaminationCamera();

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

    private void EnterExaminationCamera()
    {
        if (isCameraInExaminationMode)
            return;

        if (cameraController == null)
            return;

        isCameraInExaminationMode = cameraController.SetZoomPreset(ExaminationPresetName);
    }

    private void ResumeInteractionAfterExit()
    {
        isExamining = false;
        examiningPlayer = null;
        isInsideExaminationZone = false;

        if (isCameraInExaminationMode)
            cameraController?.ReturnToPreviousZoomState();

        isCameraInExaminationMode = false;

        if (examinationZone != null)
        {
            Destroy(examinationZone);
            examinationZone = null;
        }

        if (interactable != null)
        {
            interactable.isInteractableActive = true;

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(true);

            bool eagleVisionActive = EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
            interactable.SetQuestionFXEagleVisionState(eagleVisionActive);
        }

        if (interactionCollider != null)
            interactionCollider.enabled = true;
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
        interactable?.MarkCompleted();
        isExamining = false;
        examiningPlayer = null;

        if (isCameraInExaminationMode)
            cameraController?.ReturnToPreviousZoomState();

        isCameraInExaminationMode = false;

        if (examinationZone != null)
        {
            Destroy(examinationZone);
            examinationZone = null;
        }

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
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
