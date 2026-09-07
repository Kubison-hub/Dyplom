using System.Collections;
using System.Collections.Generic;
using System.Text;
using DialogueEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class TutorialTimeline : MonoBehaviour
{
    public static TutorialTimeline Instance { get; private set; }
    public static bool LockpickTutorialShown { get; private set; }
    public bool BlocksWorldInput => activeTutorialPopup != null || releasePopupInputCoroutine != null;
    public bool KeepsEagleVisionActive =>
        currentStage == TutorialStage.WaitingForIdeaLineTutorialClose ||
        currentStage == TutorialStage.IdeaLinePuzzleActive;

    public enum TutorialStage
    {
        WaitingForOpeningTutorial,
        RunningOpeningConversationSequence,
        WaitingForOpeningPopupDelay,
        WaitingForOpeningPopupClose,
        WaitingForFirstWorldClick,
        WaitingForIdeaPoints,
        MovingToIdeaLineTutorialPosition,
        WaitingForIdeaLineTutorialClose,
        IdeaLinePuzzleActive,
        Completed
    }

    [Header("Camera")]
    [SerializeField] private CameraController[] tutorialCameras;

    [Header("Player Control")]
    [SerializeField] private PlayerController[] tutorialPlayers;

    [Header("Tutorial Popups")]
    [SerializeField] private TutorialPopupWindow tutorialPopupPrefab;
    [SerializeField] private Transform tutorialPopupParent;
    [SerializeField] private bool showOpeningPopupAfterTutorial;
    [SerializeField, Min(0f)] private float openingPopupDelay = 3f;
    [SerializeField] private string openingPopupTitle = "PORUSZANIE SIE";
    [SerializeField, TextArea] private string openingPopupText;
    [SerializeField] private VideoClip openingPopupVideoClip;

    [Header("Opening Conversation And Notebook")]
    [Tooltip("Runs after the first standard tutorial panel closes. Leave the conversation empty to skip the dialogue.")]
    [SerializeField] private bool runOpeningConversationSequence = true;
    [SerializeField] private NPCConversation openingConversation;
    [Tooltip("Optional SmartNPC whose dialogue camera target is used during the opening conversation.")]
    [SerializeField] private SmartNPC openingConversationNpc;
    [Tooltip("When disabled, the opening dialogue changes only the zoom and leaves the current camera LookAt unchanged.")]
    [SerializeField] private bool focusCameraOnOpeningConversation;
    [SerializeField, Min(0.01f)] private float openingConversationZoomTransitionSpeed = 0.2f;
    [Tooltip("LookAt target used by SmartNPC when the opening conversation ends. Leave empty for the normal camera return.")]
    [SerializeField] private Transform openingConversationReturnCameraTarget;
    [SerializeField, Min(0f)] private float openingConversationCameraLeadDelay = 1f;
    [SerializeField, Min(0f)] private float openingConversationInputRestoreDelay = 1f;
    [SerializeField, Min(0f)] private float notebookTutorialDelay = 1f;
    [SerializeField, TextArea] private string notebookTutorialText;
    [SerializeField] private string notebookTutorialId = "NotebookTutorial";

    [Header("Tutorial Popup Audio")]
    [SerializeField] private AudioSource tutorialPopupAudioSource;
    [SerializeField] private AudioClip tutorialPopupOpenAudio;
    [SerializeField] private AudioClip tutorialPopupCloseAudio;

    [Header("Focus Tutorial Popup")]
    [SerializeField] private bool showFocusTutorialPopup;
    [SerializeField, Min(0f)] private float focusTutorialPopupDelay = 10f;
    [SerializeField] private string focusTutorialPopupTitle = "SKUPIENIE";
    [SerializeField, TextArea] private string focusTutorialPopupText =
        "Sherlock moze skupic mysli, aby odnalezc wskazowki.\n\nWcisnij Lewy Shift, aby wejsc w tryb skupienia.";
    [SerializeField] private VideoClip focusTutorialPopupVideoClip;

    [Header("Idea Line Puzzle Tutorial")]
    [Tooltip("Leave empty to use every unique point from the active DetectiveSequencePuzzle.")]
    [SerializeField] private DetectiveIdeaPoint[] requiredIdeaPoints;
    [SerializeField] private PlayerController ideaLineTutorialPlayer;
    [SerializeField] private Transform ipTutorialPosition;
    [SerializeField] private PlayerController ideaLineTutorialWatson;
    [SerializeField] private Transform watsonIdeaLineTutorialPosition;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.1f;
    [SerializeField, Min(1f)] private float rotationSpeed = 360f;
    [SerializeField, Min(0.05f)] private float eagleVisionHoldRefreshDuration = 0.25f;
    [SerializeField] private float ideaLineTutorialHorizontalAxis = 161.2f;
    [SerializeField, Min(0.1f)] private float ideaLineTutorialOrbitSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float ideaLineTutorialOrbitTolerance = 0.5f;
    [SerializeField, Min(0f)] private float cameraSettleDuration = 5f;
    [SerializeField] private string ideaLineTutorialId = "IdeaLinePuzzleTutorial";
    [SerializeField] private string ideaLineTutorialPopupTitle = "LACZENIE FAKTOW";
    [SerializeField, TextArea] private string ideaLineTutorialText =
        "Sherlock to mistrz dedukcji:\n- Polacz fakty w odpowiedniej kolejnosci.";
    [SerializeField] private VideoClip ideaLineTutorialPopupVideoClip;

    [Header("Idea Line Puzzle Music")]
    [SerializeField] private GameMusicManager gameMusicManager;
    [SerializeField] private AudioClip ideaLinePuzzleMusic;
    [SerializeField] private bool loopIdeaLinePuzzleMusic;

    [Header("Legacy Tutorial Objective Panel")]
    [Tooltip("Kept only to hide the retired panel at runtime. Use ClueManager and CluesLog for visible progress.")]
    [SerializeField] private TutorialObjectivePanel tutorialObjectivePanel;

    [Header("Events")]
    [SerializeField] private UnityEvent onTimelineStarted;
    [SerializeField] private UnityEvent onFirstWorldClick;
    [SerializeField] private UnityEvent onTimelineCompleted;
    [SerializeField] private UnityEvent onIdeaLinePuzzleTutorialStarted;
    [SerializeField] private UnityEvent onIdeaLinePuzzleTutorialClosed;

    [Header("Debug")]
    [SerializeField] private TutorialStage currentStage = TutorialStage.WaitingForOpeningTutorial;
    [SerializeField] private bool logIdeaLineTutorialProgress = true;

    private bool openingTutorialWasVisible;
    private bool openingConversationSequenceStarted;
    private Coroutine moveToIdeaLineTutorialCoroutine;
    private string lastIdeaPointDebugStatus;
    private Coroutine firstPopupCoroutine;
    private Coroutine focusTutorialPopupCoroutine;
    private Coroutine releasePopupInputCoroutine;
    private GameObject activeTutorialPopup;
    private bool activePopupReturnsToPreviousStage;
    private TutorialStage popupReturnStage;
    private Coroutine moveWatsonToIdeaLineTutorialCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (tutorialObjectivePanel != null)
            tutorialObjectivePanel.gameObject.SetActive(false);

        // Prevent one-frame world input and hover feedback before the opening tutorial starts.
        PlayerController.SetWorldInputLocked(true);
        SetTutorialMovementLocked(true);
        SetTutorialInputLocked(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDestroy()
    {
        GameplayTimePause.Resume(this);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (activeTutorialPopup != null && activePopupReturnsToPreviousStage)
        {
            if (WasPopupCloseRequestedThisFrame())
                CloseGameplayTutorialPopup();

            return;
        }

        TutorialManager tutorialManager = TutorialManager.Instance;

        if (currentStage == TutorialStage.WaitingForOpeningTutorial)
        {
            if (tutorialManager != null && tutorialManager.isTutorialActive)
            {
                openingTutorialWasVisible = true;
                SetTutorialMovementLocked(true);
            }

            if (!openingTutorialWasVisible || tutorialManager == null || tutorialManager.BlocksWorldInput)
                return;

            BeginAfterOpeningTutorial();
            return;
        }

        if (currentStage == TutorialStage.WaitingForIdeaPoints)
        {
            if (AreAllRequiredIdeaPointsDiscovered())
            {
                LogIdeaLineTutorial("All required IdeaPoints are discovered. Starting the tutorial.");
                IdeaLinePuzzeTutorial();
            }

            return;
        }

        if (currentStage == TutorialStage.WaitingForOpeningPopupClose)
        {
            if (WasPopupCloseRequestedThisFrame())
                CloseGameplayTutorialPopup();

            return;
        }

        if (currentStage == TutorialStage.WaitingForIdeaLineTutorialClose)
        {
            KeepEagleVisionForced();

            if (activeTutorialPopup == null && releasePopupInputCoroutine == null)
                FinishIdeaLinePuzzleTutorial();

            return;
        }

        if (currentStage == TutorialStage.IdeaLinePuzzleActive)
        {
            if (IsIdeaLinePuzzleSolved())
            {
                currentStage = TutorialStage.Completed;
                return;
            }

            KeepEagleVisionForced();
        }
    }

    public void NotifyIdeaPuzzleGroundClick()
    {
        if (currentStage != TutorialStage.IdeaLinePuzzleActive)
            return;

        StopCameraHorizontalOrbit();
        currentStage = TutorialStage.Completed;
        LogIdeaLineTutorial("Ground click ended the idea line puzzle session and released forced Eagle Vision.");
    }

    public void BeginAfterOpeningTutorial()
    {
        if (currentStage != TutorialStage.WaitingForOpeningTutorial)
            return;

        if (runOpeningConversationSequence && !openingConversationSequenceStarted)
        {
            openingConversationSequenceStarted = true;
            currentStage = TutorialStage.RunningOpeningConversationSequence;
            StartCoroutine(RunOpeningConversationAndNotebookSequence());
            return;
        }

        if (showOpeningPopupAfterTutorial && tutorialPopupPrefab != null)
        {
            currentStage = TutorialStage.WaitingForOpeningPopupDelay;
            SetTutorialMovementLocked(true);
            firstPopupCoroutine = StartCoroutine(ShowFirstPopupAfterDelay());
            return;
        }

        BeginFirstWorldClickStage();

    }

    private IEnumerator RunOpeningConversationAndNotebookSequence()
    {
        SetTutorialMovementLocked(true);
        SetTutorialInputLocked(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        ConversationManager conversationManager = ConversationManager.Instance;
        if (openingConversation != null && conversationManager != null)
        {
            SetCameraZoom(CameraZoomState.Medium, openingConversationZoomTransitionSpeed);
            if (focusCameraOnOpeningConversation)
            {
                openingConversationNpc?.SetDialogueCameraReturnTarget(openingConversationReturnCameraTarget);
                openingConversationNpc?.BeginDialogueCameraFocus();
            }

            if (openingConversationCameraLeadDelay > 0f)
                yield return new WaitForSecondsRealtime(openingConversationCameraLeadDelay);

            conversationManager.StartConversation(openingConversation);

            yield return null;
            // ConversationManager applies the active camera's default dialogue preset on its first frame.
            // Reapply the opening framing afterwards so this sequence stays on Narrow while it continues to move.
            SetCameraZoom(CameraZoomState.Narrow, openingConversationZoomTransitionSpeed);

            if (openingConversationInputRestoreDelay > 0f)
                yield return new WaitForSecondsRealtime(openingConversationInputRestoreDelay);

            PlayerController.SetWorldInputLocked(false);
            SetTutorialInputLocked(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
                yield return null;

            SetCameraZoom(CameraZoomState.Wide);
        }

        if (notebookTutorialDelay > 0f)
            yield return new WaitForSeconds(notebookTutorialDelay);

        NotebookManager notebookManager = NotebookManager.Instance;
        if (notebookManager != null && !notebookManager.IsNotebookOpen)
            notebookManager.ToggleNotebook();

        TutorialManager tutorialManager = TutorialManager.Instance;
        if (tutorialManager != null && !string.IsNullOrWhiteSpace(notebookTutorialText))
        {
            tutorialManager.PokazTutorial(notebookTutorialText, notebookTutorialId);
            while (tutorialManager.BlocksWorldInput)
                yield return null;
        }

        BeginFirstWorldClickStage();
        StartFocusTutorialPopupCountdown();
        LogIdeaLineTutorial("Opening conversation and notebook tutorial completed. Waiting for the first world click.");
    }

    public void NotifyWorldClick()
    {
        if (currentStage != TutorialStage.WaitingForFirstWorldClick)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput)
            return;

        CompleteFirstWorldClickStage();
    }

    public void IdeaLinePuzzeTutorial()
    {
        if (currentStage != TutorialStage.WaitingForIdeaPoints ||
            moveToIdeaLineTutorialCoroutine != null ||
            !AreAllRequiredIdeaPointsDiscovered())
            return;

        PlayerController player = GetIdeaLineTutorialPlayer();
        if (player == null || ipTutorialPosition == null)
        {
            Debug.LogWarning("TutorialTimeline: Assign Idea Line Tutorial Player and Ip Tutorial Position.", this);
            return;
        }

        LogIdeaLineTutorial("IdeaLinePuzzeTutorial started. Player input is locked and Sherlock is moving to Ip Tutorial Position.");
        currentStage = TutorialStage.MovingToIdeaLineTutorialPosition;
        SetIdeaLineTutorialInputLocked(true);
        player.currentInteractable = null;
        moveToIdeaLineTutorialCoroutine = StartCoroutine(MovePlayerToIdeaLineTutorialPosition(player));
    }

    private void CompleteFirstWorldClickStage()
    {
        onFirstWorldClick?.Invoke();

        currentStage = TutorialStage.WaitingForIdeaPoints;
        LogIdeaLineTutorial("Opening tutorial flow completed. Waiting for all required IdeaPoints.");
        onTimelineCompleted?.Invoke();
    }

    public void ShowGameplayTutorialPopup(string title, string content, VideoClip videoClip)
    {
        if (tutorialPopupPrefab == null || activeTutorialPopup != null)
            return;

        popupReturnStage = currentStage;
        activePopupReturnsToPreviousStage = true;
        SetTutorialMovementLocked(true);
        ShowTutorialPopup(title, content, videoClip);

        if (activeTutorialPopup != null)
            return;

        activePopupReturnsToPreviousStage = false;
        SetTutorialMovementLocked(false);
    }

    public bool TryShowLockpickTutorialPopup(string title, string content, VideoClip videoClip)
    {
        if (LockpickTutorialShown || tutorialPopupPrefab == null || activeTutorialPopup != null)
            return false;

        LockpickTutorialShown = true;
        ShowGameplayTutorialPopup(title, content, videoClip);
        return true;
    }

    // Compatibility with the existing TutorialManager startup call.
    public void ShowGameplayTutorialPopup(int legacyPopupIndex)
    {
        if (legacyPopupIndex != 0 || currentStage != TutorialStage.WaitingForOpeningTutorial)
        {
            Debug.LogWarning("TutorialTimeline: use ShowGameplayTutorialPopup(title, text, videoClip) for gameplay popups.", this);
            return;
        }

        if (tutorialPopupPrefab == null || activeTutorialPopup != null)
            return;

        openingTutorialWasVisible = true;
        currentStage = TutorialStage.WaitingForOpeningPopupClose;
        SetTutorialMovementLocked(true);
        ShowOpeningPopup();
    }

    private void ShowOpeningPopup()
    {
        ShowTutorialPopup(openingPopupTitle, openingPopupText, openingPopupVideoClip);
    }

    private void ShowTutorialPopup(string title, string content, VideoClip videoClip)
    {
        if (tutorialPopupPrefab == null || activeTutorialPopup != null)
            return;

        PlayerTopText.Instance?.ClearAllTopText();

        TutorialPopupWindow popup = tutorialPopupParent != null
            ? Instantiate(tutorialPopupPrefab, tutorialPopupParent, false)
            : Instantiate(tutorialPopupPrefab);

        popup.Configure(title, content, videoClip);
        activeTutorialPopup = popup.gameObject;

        if (tutorialPopupAudioSource != null && tutorialPopupOpenAudio != null)
            tutorialPopupAudioSource.PlayOneShot(tutorialPopupOpenAudio);

        GameplayTimePause.Pause(this);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private IEnumerator ShowFirstPopupAfterDelay()
    {
        if (openingPopupDelay > 0f)
            yield return new WaitForSecondsRealtime(openingPopupDelay);

        firstPopupCoroutine = null;

        if (currentStage != TutorialStage.WaitingForOpeningPopupDelay)
            yield break;

        ShowOpeningPopup();
        if (activeTutorialPopup == null)
        {
            FinishOpeningPopupStage();
            yield break;
        }

        currentStage = TutorialStage.WaitingForOpeningPopupClose;
    }

    public void CloseGameplayTutorialPopup()
    {
        if (activeTutorialPopup == null)
            return;

        if (tutorialPopupAudioSource != null && tutorialPopupCloseAudio != null)
            tutorialPopupAudioSource.PlayOneShot(tutorialPopupCloseAudio);

        Destroy(activeTutorialPopup);
        activeTutorialPopup = null;
        GameplayTimePause.Resume(this);

        if (releasePopupInputCoroutine != null)
            StopCoroutine(releasePopupInputCoroutine);

        releasePopupInputCoroutine = StartCoroutine(ReleasePopupInputAfterMouseRelease());
    }

    private static bool WasPopupCloseRequestedThisFrame()
    {
        return Keyboard.current != null &&
               (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame);
    }

    private IEnumerator ReleasePopupInputAfterMouseRelease()
    {
        yield return null;

        while ((Mouse.current != null && Mouse.current.leftButton.isPressed) || Input.GetMouseButton(0))
            yield return null;

        releasePopupInputCoroutine = null;

        if (activePopupReturnsToPreviousStage)
        {
            activePopupReturnsToPreviousStage = false;
            currentStage = popupReturnStage;
            SetTutorialMovementLocked(false);
            yield break;
        }

        FinishOpeningPopupStage();
    }

    private void FinishOpeningPopupStage()
    {
        SetTutorialMovementLocked(false);
        BeginFirstWorldClickStage();
        StartFocusTutorialPopupCountdown();
        LogIdeaLineTutorial("Opening tutorial popup closed. Waiting for the first world click.");
    }

    private void StartFocusTutorialPopupCountdown()
    {
        if (!showFocusTutorialPopup || tutorialPopupPrefab == null || focusTutorialPopupCoroutine != null)
            return;

        focusTutorialPopupCoroutine = StartCoroutine(ShowFocusTutorialPopupAfterDelay());
    }

    private IEnumerator ShowFocusTutorialPopupAfterDelay()
    {
        if (focusTutorialPopupDelay > 0f)
            yield return new WaitForSecondsRealtime(focusTutorialPopupDelay);

        while (activeTutorialPopup != null || releasePopupInputCoroutine != null)
            yield return null;

        focusTutorialPopupCoroutine = null;
        ShowGameplayTutorialPopup(
            focusTutorialPopupTitle,
            focusTutorialPopupText,
            focusTutorialPopupVideoClip);
    }

    private void BeginFirstWorldClickStage()
    {
        PlayerController.SetWorldInputLocked(false);
        SetTutorialMovementLocked(false);
        SetTutorialInputLocked(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        currentStage = TutorialStage.WaitingForFirstWorldClick;
        onTimelineStarted?.Invoke();
    }

    private IEnumerator MovePlayerToIdeaLineTutorialPosition(PlayerController player)
    {
        CancelConflictingWatsonCompletionMovements();
        StartWatsonMoveToIdeaLineTutorialPosition();
        KeepEagleVisionForced();
        SetCameraZoom(CameraZoomState.Wide);
        SetCameraHorizontalOrbit(ideaLineTutorialHorizontalAxis, ideaLineTutorialOrbitSpeed);
        onIdeaLinePuzzleTutorialStarted?.Invoke();

        player.navMeshAgent.ResetPath();
        player.navMeshAgent.isStopped = false;
        player.navMeshAgent.SetDestination(ipTutorialPosition.position);

        while (player.navMeshAgent.pathPending ||
               player.navMeshAgent.remainingDistance > Mathf.Max(player.navMeshAgent.stoppingDistance, arrivalDistance))
        {
            MaintainIdeaLineTutorialCameraOrbit();
            yield return null;
        }

        player.navMeshAgent.ResetPath();
        player.navMeshAgent.updateRotation = false;

        while (Quaternion.Angle(player.transform.rotation, ipTutorialPosition.rotation) > 0.5f)
        {
            MaintainIdeaLineTutorialCameraOrbit();
            player.transform.rotation = Quaternion.RotateTowards(
                player.transform.rotation,
                ipTutorialPosition.rotation,
                rotationSpeed * Time.deltaTime);
            yield return null;
        }

        player.transform.rotation = ipTutorialPosition.rotation;
        player.navMeshAgent.updateRotation = true;

        while (moveWatsonToIdeaLineTutorialCoroutine != null)
        {
            MaintainIdeaLineTutorialCameraOrbit();
            yield return null;
        }

        currentStage = TutorialStage.WaitingForIdeaLineTutorialClose;
        PlayIdeaLinePuzzleMusic();
        ShowGameplayTutorialPopup(
            ideaLineTutorialPopupTitle,
            ideaLineTutorialText,
            ideaLineTutorialPopupVideoClip);

        LogIdeaLineTutorial("Idea line popup requested. Waiting until it is closed.");
        moveToIdeaLineTutorialCoroutine = null;
    }

    private static void CancelConflictingWatsonCompletionMovements()
    {
        foreach (Int_lv1_HidenWallMask hiddenWallMask in FindObjectsByType<Int_lv1_HidenWallMask>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            hiddenWallMask.CancelWatsonCompletionMovement();
        }
    }

    private void StartWatsonMoveToIdeaLineTutorialPosition()
    {
        if (moveWatsonToIdeaLineTutorialCoroutine != null || watsonIdeaLineTutorialPosition == null)
            return;

        PlayerController watson = GetIdeaLineTutorialWatson();
        if (watson == null)
        {
            Debug.LogWarning("TutorialTimeline: Assign Idea Line Tutorial Watson or add Watson to Tutorial Players.", this);
            return;
        }

        moveWatsonToIdeaLineTutorialCoroutine = StartCoroutine(
            MoveWatsonToIdeaLineTutorialPosition(watson));
    }

    private IEnumerator MoveWatsonToIdeaLineTutorialPosition(PlayerController watson)
    {
        NavMeshAgent agent = watson != null ? watson.navMeshAgent : null;
        if (agent == null || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(watsonIdeaLineTutorialPosition.position, out NavMeshHit destination, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning("TutorialTimeline: Watson Idea Line Position is not on the NavMesh.", watsonIdeaLineTutorialPosition);
            moveWatsonToIdeaLineTutorialCoroutine = null;
            yield break;
        }

        WatsonCompanionController companionController = watson.GetComponent<WatsonCompanionController>();
        companionController?.ClearInteractionFocus();
        watson.currentInteractable = null;
        watson.currentInteractionPoint = null;

        agent.ResetPath();
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.SetDestination(destination.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.hasPath &&
                   agent.remainingDistance > Mathf.Max(agent.stoppingDistance, arrivalDistance))
            {
                // A previous interaction reaction may have disabled agent rotation.
                // Keep the NavMeshAgent responsible for facing Watson along his path.
                agent.updateRotation = true;
                yield return null;
            }

            agent.ResetPath();
            agent.updateRotation = false;

            while (Quaternion.Angle(watson.transform.rotation, watsonIdeaLineTutorialPosition.rotation) > 0.5f)
            {
                watson.transform.rotation = Quaternion.RotateTowards(
                    watson.transform.rotation,
                    watsonIdeaLineTutorialPosition.rotation,
                    rotationSpeed * Time.deltaTime);
                yield return null;
            }

            watson.transform.rotation = watsonIdeaLineTutorialPosition.rotation;
            agent.updateRotation = true;
        }
        else
        {
            Debug.LogWarning("TutorialTimeline: Watson cannot reach Watson Idea Line Position.", watsonIdeaLineTutorialPosition);
        }

        agent.ResetPath();
        moveWatsonToIdeaLineTutorialCoroutine = null;
    }

    private void FinishIdeaLinePuzzleTutorial()
    {
        StopCameraHorizontalOrbit();
        SetIdeaLineTutorialInputLocked(false);
        currentStage = TutorialStage.IdeaLinePuzzleActive;
        LogIdeaLineTutorial("Idea line tutorial closed. Player input restored; Eagle Vision remains forced until the puzzle is solved.");
        onIdeaLinePuzzleTutorialClosed?.Invoke();
    }

    private void PlayIdeaLinePuzzleMusic()
    {
        if (ideaLinePuzzleMusic == null)
            return;

        if (gameMusicManager == null)
            gameMusicManager = FindFirstObjectByType<GameMusicManager>();

        gameMusicManager?.ChangeMusic(ideaLinePuzzleMusic, loopIdeaLinePuzzleMusic);
    }

    private bool AreAllRequiredIdeaPointsDiscovered()
    {
        List<DetectiveIdeaPoint> points = GetRequiredIdeaPoints();
        LogIdeaPointStatuses(points);

        if (points.Count == 0)
            return false;

        foreach (DetectiveIdeaPoint point in points)
        {
            if (point == null || !point.IsDiscovered)
                return false;
        }

        return true;
    }

    [ContextMenu("Log Idea Line Tutorial State")]
    public void LogIdeaLineTutorialState()
    {
        List<DetectiveIdeaPoint> points = GetRequiredIdeaPoints();
        lastIdeaPointDebugStatus = null;
        LogIdeaPointStatuses(points);
        LogIdeaLineTutorial($"Current stage: {currentStage}. Ip Tutorial Position assigned: {ipTutorialPosition != null}. " +
                            $"Idea Line Tutorial Player assigned: {GetIdeaLineTutorialPlayer() != null}.");
    }

    private void LogIdeaPointStatuses(List<DetectiveIdeaPoint> points)
    {
        if (!logIdeaLineTutorialProgress)
            return;

        string source = requiredIdeaPoints != null && requiredIdeaPoints.Length > 0
            ? "Required Idea Points from Inspector"
            : "DetectiveIdeaManager.activePuzzle.correctSequence";

        StringBuilder status = new StringBuilder(source).Append(" | ");
        if (points.Count == 0)
        {
            status.Append("No IdeaPoints found.");
        }
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                DetectiveIdeaPoint point = points[i];
                status.Append(point == null
                    ? $"[{i}: NULL] "
                    : $"[{point.ideaId} = {point.IsDiscovered}] ");
            }
        }

        string statusText = status.ToString();
        if (statusText == lastIdeaPointDebugStatus)
            return;

        lastIdeaPointDebugStatus = statusText;
        Debug.Log($"TutorialTimeline IdeaPoint check: {statusText}", this);
    }

    private void LogIdeaLineTutorial(string message)
    {
        if (logIdeaLineTutorialProgress)
            Debug.Log($"TutorialTimeline: {message}", this);
    }

    private List<DetectiveIdeaPoint> GetRequiredIdeaPoints()
    {
        List<DetectiveIdeaPoint> points = new List<DetectiveIdeaPoint>();

        if (requiredIdeaPoints != null && requiredIdeaPoints.Length > 0)
        {
            foreach (DetectiveIdeaPoint point in requiredIdeaPoints)
            {
                if (point != null && !points.Contains(point))
                    points.Add(point);
            }

            return points;
        }

        DetectiveSequencePuzzle puzzle = DetectiveIdeaManager.Instance != null
            ? DetectiveIdeaManager.Instance.activePuzzle
            : null;

        if (puzzle == null)
            return points;

        foreach (DetectiveSequencePuzzle.IdeaConnectionStep step in puzzle.correctSequence)
        {
            if (step == null)
                continue;

            if (step.from != null && !points.Contains(step.from))
                points.Add(step.from);

            if (step.to != null && !points.Contains(step.to))
                points.Add(step.to);
        }

        return points;
    }

    private bool IsIdeaLinePuzzleSolved()
    {
        return DetectiveIdeaManager.Instance != null &&
               DetectiveIdeaManager.Instance.activePuzzle != null &&
               DetectiveIdeaManager.Instance.activePuzzle.IsSolved;
    }

    private PlayerController GetIdeaLineTutorialPlayer()
    {
        if (ideaLineTutorialPlayer != null)
            return ideaLineTutorialPlayer;

        if (tutorialPlayers != null && tutorialPlayers.Length > 0)
            return tutorialPlayers[0];

        return null;
    }

    private PlayerController GetIdeaLineTutorialWatson()
    {
        if (ideaLineTutorialWatson != null)
            return ideaLineTutorialWatson;

        if (tutorialPlayers == null)
            return null;

        foreach (PlayerController player in tutorialPlayers)
        {
            if (player != null && player.playerCharacter == PlayerCharacter.Watson)
                return player;
        }

        return null;
    }

    private void KeepEagleVisionForced()
    {
        if (EagleVisionSystem.Instance != null)
            EagleVisionSystem.Instance.HoldVisionFor(eagleVisionHoldRefreshDuration);
    }

    private void SetCameraZoom(CameraZoomState zoomState, float transitionSmoothSpeed = -1f)
    {
        if (tutorialCameras == null)
            return;

        foreach (CameraController cameraController in tutorialCameras)
        {
            if (cameraController != null)
                cameraController.SetZoomIndex((int)zoomState, transitionSmoothSpeed);
        }
    }

    private void SetCameraHorizontalOrbit(float horizontalAxisValue, float orbitSpeed)
    {
        if (tutorialCameras == null)
            return;

        foreach (CameraController cameraController in tutorialCameras)
        {
            if (cameraController != null)
                cameraController.OrbitHorizontalAxisTo(horizontalAxisValue, orbitSpeed);
        }
    }

    private void MaintainIdeaLineTutorialCameraOrbit()
    {
        SetCameraHorizontalOrbit(ideaLineTutorialHorizontalAxis, ideaLineTutorialOrbitSpeed);
        KeepEagleVisionForced();
    }

    private bool AreTutorialCamerasAtHorizontalAxis(float horizontalAxisValue)
    {
        if (tutorialCameras == null || tutorialCameras.Length == 0)
            return true;

        foreach (CameraController cameraController in tutorialCameras)
        {
            if (cameraController != null &&
                !cameraController.IsHorizontalAxisAt(horizontalAxisValue, ideaLineTutorialOrbitTolerance))
            {
                return false;
            }
        }

        return true;
    }

    private void StopCameraHorizontalOrbit()
    {
        if (tutorialCameras == null)
            return;

        foreach (CameraController cameraController in tutorialCameras)
        {
            if (cameraController != null)
                cameraController.StopScriptedHorizontalOrbit();
        }
    }

    private void SetTutorialMovementLocked(bool locked)
    {
        if (tutorialPlayers == null)
            return;

        foreach (PlayerController playerController in tutorialPlayers)
        {
            if (playerController != null)
                playerController.SetTutorialMovementLocked(locked);
        }
    }

    private void SetTutorialInputLocked(bool locked)
    {
        if (tutorialPlayers == null)
            return;

        foreach (PlayerController playerController in tutorialPlayers)
        {
            if (playerController != null)
                playerController.SetTutorialInputLocked(locked);
        }
    }

    private void SetIdeaLineTutorialInputLocked(bool locked)
    {
        PlayerController player = GetIdeaLineTutorialPlayer();
        if (player != null)
            player.SetTutorialInputLocked(locked);
    }
}
