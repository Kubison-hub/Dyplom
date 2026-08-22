using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Video;

public class EmptyWall : Lvl3InteractionDialogueBase
{

    private Interactable interactable;

    public bool performed = false;
    public Transform cardPosition;

    [SerializeField] private Int_EdithExamBody edith;
    public GameObject[] nextInteractions;
    [SerializeField] private DetectiveIdeaPoint ideaPoint;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private string wallExamPresetName = "WallExam";
    [Header("Wall Examination Zone")]
    [SerializeField, Min(0.1f)] private float playerZoneRange = 3f;
    [SerializeField] private Vector3 playerZoneOffset;
    [SerializeField, Min(0.1f)] private float wallExamZoomSmoothSpeed = 2.5f;
    [Header("Wall Examination Orbit")]
    [SerializeField] private bool orbitToWallExamAxis = true;
    [SerializeField] private float wallExamHorizontalAxis = 178f;
    [SerializeField, Min(0.1f)] private float wallExamOrbitSpeed = 12f;
    [Header("Tutorial Popup")]
    [SerializeField] private bool showTutorialPopup = true;
    [SerializeField, Min(0f)] private float tutorialPopupDelay = 1f;
    [SerializeField] private string tutorialPopupTitle = "BADANIE SCENY";
    [SerializeField, TextArea] private string tutorialPopupText;
    [SerializeField] private VideoClip tutorialPopupVideoClip;
    [Header("Initial Examination Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] initialDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ta œciana pustki siê nie boi, Watsonie.",
            duration = 3f
        }
    };

    private Collider intCollider;
    private PlayerController examiningPlayer;
    private GameObject playerZone;
    private bool isPlayerInsideZone;
    private bool isMonitoringExamination;
    private bool isWallExamCameraActive;
    private bool tutorialPopupShown;
    private bool tutorialPopupPending;
    private Coroutine tutorialPopupCoroutine;
    private bool waitingForInitialDialogue;
    private bool initialDialogueCompleted;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => initialDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        intCollider = GetComponent<Collider>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

        ActiveNextInteractions(false);

    }

    private void Update()
    {
        if (!isMonitoringExamination || examiningPlayer == null)
            return;

        if (ideaPoint != null && ideaPoint.IsDiscovered)
        {
            FinishExamination();
            return;
        }

        Vector3 zoneCenter = transform.TransformPoint(playerZoneOffset);
        bool playerIsNowInside = Vector3.Distance(examiningPlayer.transform.position, zoneCenter) <= playerZoneRange;
        if (isPlayerInsideZone)
        {
            SetWallInteractionShaderVisible(false);

            if (playerIsNowInside)
                return;

            isPlayerInsideZone = false;
            cameraController?.StopScriptedHorizontalOrbit();
            cameraController?.ReturnToPreviousZoomState(wallExamZoomSmoothSpeed);
            isWallExamCameraActive = false;
            SetWallInteractionShaderVisible(true);
            CancelPendingTutorialPopup();
            return;
        }

        if (!playerIsNowInside)
            return;

        isPlayerInsideZone = true;
        isWallExamCameraActive = cameraController != null &&
                                  cameraController.SetZoomPreset(wallExamPresetName, wallExamZoomSmoothSpeed);
        StartWallExamOrbit();
        SetWallInteractionShaderVisible(false);
        ShowTutorialPopupIfNeeded();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player == null)
            return;

        if (!performed)
        {
            //interactable.AddClue(0, cardPosition);
            performed = true;
            ActiveNextInteractions(true);
            waitingForInitialDialogue = initialDialogue != null && initialDialogue.Length > 0;
            initialDialogueCompleted = !waitingForInitialDialogue;
            if (waitingForInitialDialogue)
                PlayDialogue(player, initialDialogue);

            DisableWallInteractionPresentation();
        }

        BeginExamination(player);
        player.currentInteractable = null;
    }
    private void DisableWallInteractionPresentation()
    {
        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (intCollider != null)
            intCollider.enabled = false;
    }
    private void BeginExamination(PlayerController player)
    {
        examiningPlayer = player;
        isMonitoringExamination = examiningPlayer != null;
        CreatePlayerZone();
        isPlayerInsideZone = IsPlayerInsideZone();

        if (isPlayerInsideZone)
        {
            isWallExamCameraActive = cameraController != null &&
                                      cameraController.SetZoomPreset(wallExamPresetName, wallExamZoomSmoothSpeed);
            StartWallExamOrbit();
            SetWallInteractionShaderVisible(false);
            ShowTutorialPopupIfNeeded();
        }
    }
    private void FinishExamination()
    {
        isMonitoringExamination = false;
        CancelPendingTutorialPopup();
        cameraController?.StopScriptedHorizontalOrbit();
        if (isWallExamCameraActive)
            cameraController?.ReturnToPreviousZoomState(wallExamZoomSmoothSpeed);

        isWallExamCameraActive = false;

        if (playerZone != null)
            Destroy(playerZone);

        DisableWallInteractionPresentation();


        enabled = false;
    }
    private void CreatePlayerZone()
    {
        if (playerZone != null)
            Destroy(playerZone);

        playerZone = new GameObject("EmptyWall_PlayerZone");
        playerZone.transform.SetParent(transform, false);
        playerZone.transform.localPosition = playerZoneOffset;
        playerZone.layer = LayerMask.NameToLayer("Ignore Raycast");

        SphereCollider zoneCollider = playerZone.AddComponent<SphereCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.radius = playerZoneRange;
    }

    private bool IsPlayerInsideZone()
    {
        if (examiningPlayer == null)
            return false;

        Vector3 zoneCenter = transform.TransformPoint(playerZoneOffset);
        return Vector3.Distance(examiningPlayer.transform.position, zoneCenter) <= playerZoneRange;
    }
    private void StartWallExamOrbit()
    {
        if (orbitToWallExamAxis)
            cameraController?.OrbitHorizontalAxisTo(wallExamHorizontalAxis, wallExamOrbitSpeed);
    }

    private void ShowTutorialPopupIfNeeded()
    {
        if (!initialDialogueCompleted || !showTutorialPopup || tutorialPopupShown || tutorialPopupPending || TutorialTimeline.Instance == null)
            return;

        tutorialPopupPending = true;
        tutorialPopupCoroutine = StartCoroutine(ShowTutorialPopupAfterCameraSettles());
    }

    private System.Collections.IEnumerator ShowTutorialPopupAfterCameraSettles()
    {
        if (tutorialPopupDelay > 0f)
            yield return new WaitForSecondsRealtime(tutorialPopupDelay);

        tutorialPopupCoroutine = null;
        tutorialPopupPending = false;

        if (!isMonitoringExamination || !isPlayerInsideZone || TutorialTimeline.Instance == null)
            yield break;

        tutorialPopupShown = true;
        TutorialTimeline.Instance.ShowGameplayTutorialPopup(
            tutorialPopupTitle,
            tutorialPopupText,
            tutorialPopupVideoClip);
    }

    private void CancelPendingTutorialPopup()
    {
        if (tutorialPopupCoroutine != null)
            StopCoroutine(tutorialPopupCoroutine);

        tutorialPopupCoroutine = null;
        tutorialPopupPending = false;
    }

    private void SetWallInteractionShaderVisible(bool visible)
    {
        if (interactable != null && interactable.interactiveShader != null)
            interactable.interactiveShader.SetActive(visible);
    }



    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (!waitingForInitialDialogue || lines != initialDialogue)
            return;

        waitingForInitialDialogue = false;
        initialDialogueCompleted = true;

        if (isMonitoringExamination && isPlayerInsideZone)
            ShowTutorialPopupIfNeeded();
    }
    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isMonitoringExamination
            ? new Color(0.3f, 0.8f, 0.5f, 0.7f)
            : new Color(0.4f, 0.7f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.TransformPoint(playerZoneOffset), playerZoneRange);
    }

    


}
