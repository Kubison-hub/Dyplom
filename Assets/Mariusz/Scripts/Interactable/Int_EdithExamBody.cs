using System.Collections;
using System;

using Unity.Cinemachine;
using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;
using UnityEngine.AI;
using UnityEngine.Video;
using DialogueEditor;


public class Int_EdithExamBody : Lvl3InteractionDialogueBase
{
    private bool interactionPerforming = false;
    private Interactable interactable;

    public bool performed = false;
    public CinemachineCamera interactionCamera;
    private CinemachineSplineDolly splineDolly;

    public GameObject watsonGO;
    public GameObject sherlockGO;
    private NavMeshAgent watsonNavMesh;
    [SerializeField] private Animator watsonAnimator;
    [SerializeField] private string watsonExamAnimatorBool = "Exam";
    private Animator sherlockAnimator;

    public Transform watsonPosition;
    [Header("Bullet CP Watson Position")]
    [SerializeField] private Transform watsonExaminationPoint;
    [SerializeField, Min(0.01f)] private float watsonExaminationArrivalTolerance = 0.1f;
    [SerializeField, Min(1f)] private float watsonExaminationRotationSpeed = 360f;
    [SerializeField] private Collider intCollider;

    //public GameObject[] nextInteractions;
    public GameObject nextInteractions;
    [SerializeField] private CameraController cameraController;
    [Header("Examination Zone")]
    [SerializeField, Min(0.1f)] private float examinationZoneRange = 3f;
    [SerializeField] private Vector3 examinationZoneOffset;
    [Header("Examination Camera")]
    [SerializeField] private string examinationPresetName = "EdithExam";
    [SerializeField] private float examinationHorizontalAxis = -80f;
    [SerializeField, Min(0.1f)] private float examinationOrbitSpeed = 1.5f;
    [Header("Tutorial Popup")]
    [SerializeField] private bool showTutorialPopup = true;
    [SerializeField, Min(0f)] private float tutorialPopupDelay = 1f;
    [SerializeField] private string tutorialPopupTitle = "BADANIE CIALA";
    [SerializeField, TextArea] private string tutorialPopupText;
    [SerializeField] private VideoClip tutorialPopupVideoClip;
    [Header("Initial Examination Dialogue")]
    [SerializeField]
    private Lvl3DialogueLine[] initialExaminationDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Przyjrzyjmy się jej uważnie, Watsonie.",
            duration = 3f
        }
    };
    [Header("Examination CP Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] ringCpDialogue;
    [SerializeField] private Lvl3DialogueLine[] paperCpDialogue;
    [SerializeField] private Lvl3DialogueLine[] bulletCpDialogue;
    [Header("Arthur Conversation Conditions")]
    [Tooltip("Arthur's SmartNPC. Ring and Paper are set on both his Sherlock and Watson conversations.")]
    [SerializeField] private SmartNPC arthurSmartNpc;
    [SerializeField] private string arthurRingParameterName = "Ring";
    [SerializeField] private string arthurPaperParameterName = "Paper";
    [Tooltip("Played before every Bullet CP attempt after the player failed the examination conversation once.")]
    [SerializeField]
    private Lvl3DialogueLine[] bulletCpRetryDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Spróbuję jeszcze raz.",
            duration = 2f
        }
    };
    [SerializeField]
    private Lvl3DialogueLine[] bulletCpExamDoneDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Kula trafiła prosto w serce i przeszła na wylot.",
            duration = 3f
        }
    };
    [Header("Bullet Idea Point Check")]
    [Tooltip("Optional IdeaPoint checked after the successful Bullet CP examination.")]
    [SerializeField] private DetectiveIdeaPoint bulletIdeaPointToCheck;
    [SerializeField]
    private Lvl3DialogueLine[] bulletIdeaPointMissingDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "W takim razie, gdzie jest kula?",
            duration = 3f
        }
    };
    [SerializeField]
    private Lvl3DialogueLine[] bulletCpExamIncompleteDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Muszę dokładniej przyjrzeć się ciału.",
            duration = 3f
        }
    };
    [SerializeField] private Lvl3DialogueLine[] allCpCollectedDialogue;
    [Header("Bullet CP Conversation")]
    [Tooltip("Conversation opened after Bullet CP Dialogue. The CP is counted only after this conversation ends.")]
    [SerializeField] private SmartNPC bulletCpSmartNpc;
    [SerializeField, Min(0.01f)] private float bulletCpDialogueZoomTransitionSpeed = 0.2f;
    [SerializeField] private string bulletCpExamDoneParameterName = "BulletExamDone";
    [Header("All CP Collected Clue")]
    [Tooltip("Index from Interactable > Clues. Set to -1 to skip adding a final clue.")]
    [SerializeField] private int allCpCollectedClueIndex = -1;
    [SerializeField] private Transform allCpCollectedClueCardPosition;
    [Header("Detective Idea")]
    [SerializeField] private DetectiveIdeaPoint edithIdeaPoint;
    [Header("Examination Clue Points")]
    [Tooltip("CP GameObjects are disabled at scene start and enabled only after examining Lady Edith's body.")]
    [SerializeField] private GameObject[] examinationClueObjects;
    //ClueCards Positions
    public Transform cp0;
    public Transform cp1;
    public Transform cp2;

    [Min(1)] public int requiredCluesToRevealIdea = 3;

    public bool bulletExamined = false;
    public GameObject bulletLine;

    public int currentRequiredBulletExam = 0;

    private int collectedExamClueCount;
    private bool edithIdeaRevealed;
    private bool examinationCompleted;
    private PlayerController examiningPlayer;
    private GameObject examinationZone;
    private bool isPlayerInsideExaminationZone;
    private bool isExaminationCameraActive;
    private bool tutorialPopupShown;
    private bool tutorialPopupPending;
    private Coroutine tutorialPopupCoroutine;
    private bool waitingForInitialDialogue;
    private bool initialDialogueCompleted;
    private bool completionDialoguePending;
    private bool allCpCollectedClueAdded;
    private Coroutine completionTutorialCoroutine;
    private bool bulletCpAwaitingConversation;
    private bool bulletCpConversationActive;
    private bool bulletCpConversationStarting;
    private bool bulletExamDone;
    private bool bulletCpExamAttempted;
    private bool bulletCpResultDialoguePlaying;
    private bool ringCpCollected;
    private bool paperCpCollected;
    private bool arthurFoundItemsObjectiveAdded;
    private PlayerController bulletCpPlayer;
    private Coroutine bulletCpConversationCoroutine;
    private Int_Edith_BulletHole pendingBulletCp;

    public bool IsExaminationCompleted => examinationCompleted || edithIdeaRevealed;
    public bool IsExaminationActive => interactionPerforming && !examinationCompleted && !edithIdeaRevealed;
    public bool IsBulletExamInProgress => bulletCpAwaitingConversation || bulletCpConversationStarting ||
                                           bulletCpConversationActive || bulletCpResultDialoguePlaying;
    protected override Lvl3DialogueLine[] DefaultDialogueLines => initialExaminationDialogue;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
        FindEdithIdeaPointIfNeeded();
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        if (watsonAnimator == null && watsonGO != null)
            watsonAnimator = watsonGO.GetComponentInChildren<Animator>();

        if (watsonNavMesh == null && watsonGO != null)
            watsonNavMesh = watsonGO.GetComponent<NavMeshAgent>();

        if (arthurSmartNpc == null)
        {
            int_lv1_ArthurNPC arthurInteraction = FindFirstObjectByType<int_lv1_ArthurNPC>();
            if (arthurInteraction != null)
                arthurSmartNpc = arthurInteraction.GetComponentInChildren<SmartNPC>(true);
        }

        SetExaminationClueObjectsActive(false);
        //watsonNavMesh = watsonGO.GetComponent<NavMeshAgent>();
        //watsonAnimator = watsonGO.GetComponent <Animator>();
        //sherlockAnimator = sherlockGO.GetComponent<Animator>();

        //splineDolly = interactionCamera.GetComponent<CinemachineSplineDolly>();


    }

    private void OnDestroy()
    {
        StopWaitingForBulletCpConversation();
        ReleaseBulletCpWorldInput();
    }

    private void Update()
    {
        if (examinationCompleted || !interactionPerforming || examiningPlayer == null)
            return;

        if (edithIdeaRevealed)
        {
            if (!completionDialoguePending)
                EndExamination(true);

            return;
        }

        bool playerIsNowInside = IsPlayerInsideExaminationZone();
        if (playerIsNowInside == isPlayerInsideExaminationZone)
            return;

        isPlayerInsideExaminationZone = playerIsNowInside;

        if (isPlayerInsideExaminationZone)
            EnterExaminationCamera();
        else
            ExitExaminationCamera();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (examinationCompleted || player == null)
            return;


        if (!performed)
        {
            performed = true;
            waitingForInitialDialogue = initialExaminationDialogue != null && initialExaminationDialogue.Length > 0;
            initialDialogueCompleted = !waitingForInitialDialogue;

            if (waitingForInitialDialogue)
                PlayDialogue(player, initialExaminationDialogue);
        }

        if (intCollider != null)
            intCollider.enabled = true;

        if (nextInteractions != null)
            nextInteractions.SetActive(true);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
        }

        if (!interactionPerforming)
        {
            BeginExamination(player);
            SetExaminationClueObjectsActive(true);
        }

        player.currentInteractable = null;



    }


    //public void PerformInteraction2(PlayerController player)
    //{

    //    ChangeCamera();
    //    PerformWatsonAction();
    //    interactable.interactiveShader = null;
    //    interactionPerforming = true;
    //    sherlockAnimator.SetBool("IsThinking", true);
    //    StartCoroutine(EdithExamination());
    //    interactable.isInteractableActive = false;
    //    player.currentInteractable = null;
    //    intCollider.isTrigger = false;

    //    interactable.interactionVFX.Stop();
    //    interactable.questionVFX.Stop();

    //}



    public void ChangeCamera()
    {
        if (interactionCamera != null)
        {
            splineDolly.CameraPosition = 0.5f;
            interactionCamera.Priority = 50;


        }
    }


    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);

        RegisterExamClue();
    }

    public void RegisterExamClue()
    {
        RegisterExamClue(-1, null);
    }

    public void RegisterExamClue(int clueIndex, PlayerController player)
    {
        if (!performed || examinationCompleted || edithIdeaRevealed)
            return;

        SetArthurExaminationCondition(clueIndex);

        Lvl3DialogueLine[] clueDialogue = GetClueDialogue(clueIndex);

        if (clueIndex == 2 && bulletCpSmartNpc != null)
        {
            bulletCpPlayer = player != null ? player : examiningPlayer;
            bulletCpAwaitingConversation = true;

            if (clueDialogue != null && clueDialogue.Length > 0)
                PlayDialogue(player, clueDialogue);
            else
                StartBulletCpConversation();

            return;
        }

        if (clueDialogue != null && clueDialogue.Length > 0)
            PlayDialogue(player, clueDialogue);

        CompleteExamClueRegistration(clueDialogue == null || clueDialogue.Length == 0);
    }

    public void RegisterBulletExamClue(PlayerController player, Int_Edith_BulletHole bulletCp)
    {
        if (!performed || examinationCompleted || edithIdeaRevealed || IsBulletExamInProgress)
            return;

        pendingBulletCp = bulletCp;
        bulletExamDone = false;
        bulletCpPlayer = player != null ? player : examiningPlayer;
        bulletCpAwaitingConversation = true;
        LockBulletCpWorldInput();

        Lvl3DialogueLine[] attemptDialogue = GetBulletCpAttemptDialogue();
        if (attemptDialogue != null && attemptDialogue.Length > 0)
            PlayDialogue(player, attemptDialogue);
        else
            StartBulletCpConversation();
    }

    public void MarkBulletExamDone()
    {
        if (!bulletCpConversationActive)
        {
            Debug.LogWarning("Int_EdithExamBody: MarkBulletExamDone was called outside the Bullet CP conversation.", this);
            return;
        }

        bulletExamDone = true;
    }

    private void CompleteExamClueRegistration(bool finalCpDialogueAlreadyFinished)
    {
        collectedExamClueCount++;
        CluesLog.Instance?.SetLadyEdithBodyProgress(collectedExamClueCount, requiredCluesToRevealIdea);

        if (collectedExamClueCount < requiredCluesToRevealIdea)
            return;

        AddAllCpCollectedClue();
        completionDialoguePending = true;
        edithIdeaRevealed = true;
        FindEdithIdeaPointIfNeeded();
        edithIdeaPoint?.RevealFromExternalSource();

        // Keep this component enabled while the final CP dialogue is playing.
        // Disabling it here would stop its dialogue coroutine and cut the audio off.
        if (finalCpDialogueAlreadyFinished)
            ContinueAfterFinalClueDialogue();
    }

    private void SetArthurExaminationCondition(int clueIndex)
    {
        RegisterArthurFoundItemClue(clueIndex);

        string parameterName = clueIndex switch
        {
            0 => arthurRingParameterName,
            1 => arthurPaperParameterName,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(parameterName) || arthurSmartNpc == null)
            return;

        SetConversationBoolParameter(arthurSmartNpc.rozmowaDlaPostaciA, parameterName, true);
        SetConversationBoolParameter(arthurSmartNpc.rozmowaDlaPostaciB, parameterName, true);
    }

    private void RegisterArthurFoundItemClue(int clueIndex)
    {
        switch (clueIndex)
        {
            case 0:
                ringCpCollected = true;
                break;
            case 1:
                paperCpCollected = true;
                break;
            default:
                return;
        }

        if (arthurFoundItemsObjectiveAdded || !ringCpCollected || !paperCpCollected)
            return;

        arthurFoundItemsObjectiveAdded = true;
        CluesLog.Instance?.AddAskArthurAboutFoundItemsObjective();
    }

    private void SetConversationBoolParameter(NPCConversation conversation, string parameterName, bool value)
    {
        if (conversation == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        string requestedParameterName = parameterName.Trim();
        string resolvedParameterName = requestedParameterName;

        if (conversation.ParameterList == null)
            conversation.DeserializeForEditor();

        if (conversation.ParameterList != null)
        {
            foreach (EditableParameter parameter in conversation.ParameterList)
            {
                if (parameter != null &&
                    string.Equals(parameter.ParameterName?.Trim(), requestedParameterName, StringComparison.Ordinal))
                {
                    resolvedParameterName = parameter.ParameterName;
                    break;
                }
            }
        }

        conversation.SetRuntimeBoolParameter(resolvedParameterName, value);
    }

    private void StartBulletCpConversation()
    {
        bulletCpAwaitingConversation = false;

        ConversationManager conversationManager = ConversationManager.Instance;
        NPCConversation conversation = GetBulletCpConversation();
        if (conversationManager == null || conversation == null)
        {
            Debug.LogWarning(
                "Int_EdithExamBody: Bullet CP SmartNPC needs a valid Dialogue Editor conversation. The CP will remain available.",
                this);
            BeginBulletCpResultDialogue(false);
            return;
        }

        if (conversationManager.IsConversationActive)
        {
            StartCoroutine(WaitForConversationAndStartBulletCp(conversation));
            return;
        }

        BeginBulletCpConversation(conversationManager, conversation);
    }

    private IEnumerator WaitForConversationAndStartBulletCp(NPCConversation conversation)
    {
        while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            yield return null;

        if (ConversationManager.Instance == null)
        {
            BeginBulletCpResultDialogue(false);
            yield break;
        }

        BeginBulletCpConversation(ConversationManager.Instance, conversation);
    }

    private NPCConversation GetBulletCpConversation()
    {
        if (bulletCpSmartNpc == null)
            return null;

        bool isWatson = bulletCpPlayer != null &&
                        (bulletCpPlayer.playerCharacter == PlayerCharacter.Watson || bulletCpPlayer.CompareTag("PlayerB"));

        return isWatson
            ? bulletCpSmartNpc.rozmowaDlaPostaciB
            : bulletCpSmartNpc.rozmowaDlaPostaciA;
    }

    private void BeginBulletCpConversation(ConversationManager conversationManager, NPCConversation conversation)
    {
        if (bulletCpConversationStarting || bulletCpConversationActive)
            return;

        if (watsonExaminationPoint != null && watsonNavMesh != null &&
            watsonNavMesh.isActiveAndEnabled && watsonNavMesh.isOnNavMesh)
        {
            bulletCpConversationStarting = true;
            bulletCpConversationCoroutine = StartCoroutine(
                MoveWatsonToExaminationPointAndBeginConversation(conversationManager, conversation));
            return;
        }

        StartBulletCpConversationAfterWatsonArrives(conversationManager, conversation);
    }

    private IEnumerator MoveWatsonToExaminationPointAndBeginConversation(
        ConversationManager conversationManager,
        NPCConversation conversation)
    {
        watsonNavMesh.SetDestination(watsonExaminationPoint.position);

        while (watsonNavMesh != null && watsonNavMesh.isActiveAndEnabled &&
               watsonNavMesh.isOnNavMesh && watsonNavMesh.pathPending)
        {
            yield return null;
        }

        while (watsonNavMesh != null && watsonNavMesh.isActiveAndEnabled && watsonNavMesh.isOnNavMesh &&
               watsonNavMesh.hasPath &&
               watsonNavMesh.remainingDistance > watsonNavMesh.stoppingDistance + watsonExaminationArrivalTolerance)
        {
            yield return null;
        }

        yield return RotateWatsonToExaminationPoint();

        bulletCpConversationCoroutine = null;
        bulletCpConversationStarting = false;

        if (conversationManager == null || ConversationManager.Instance == null ||
            ConversationManager.Instance.IsConversationActive)
        {
            BeginBulletCpResultDialogue(false);
            yield break;
        }

        StartBulletCpConversationAfterWatsonArrives(conversationManager, conversation);
    }

    private IEnumerator RotateWatsonToExaminationPoint()
    {
        if (watsonExaminationPoint == null || watsonNavMesh == null ||
            !watsonNavMesh.isActiveAndEnabled || !watsonNavMesh.isOnNavMesh)
            yield break;

        watsonNavMesh.ResetPath();
        watsonNavMesh.isStopped = true;
        watsonNavMesh.updateRotation = false;

        Vector3 forward = watsonExaminationPoint.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            while (Quaternion.Angle(watsonNavMesh.transform.rotation, targetRotation) > 0.1f)
            {
                watsonNavMesh.transform.rotation = Quaternion.RotateTowards(
                    watsonNavMesh.transform.rotation,
                    targetRotation,
                    watsonExaminationRotationSpeed * Time.deltaTime);
                yield return null;
            }

            watsonNavMesh.transform.rotation = targetRotation;
        }

        watsonNavMesh.updateRotation = true;
        watsonNavMesh.isStopped = false;
    }

    private void StartBulletCpConversationAfterWatsonArrives(
        ConversationManager conversationManager,
        NPCConversation conversation)
    {
        bulletCpSmartNpc?.BeginDialogueCameraFocusWithZoom(
            "WatsonExam",
            bulletCpDialogueZoomTransitionSpeed);
        SetWatsonExamAnimation(true);
        bulletCpConversationActive = true;
        ConversationManager.OnConversationEnded += HandleBulletCpConversationEnded;
        conversationManager.StartConversation(conversation);
    }

    private void HandleBulletCpConversationEnded()
    {
        if (!bulletCpConversationActive)
            return;

        bool examDone = bulletExamDone || IsBulletExamDoneConversationParameterSet();
        bulletCpSmartNpc?.EndDialogueCameraFocus();
        StopWaitingForBulletCpConversation();
        BeginBulletCpResultDialogue(examDone);
    }

    private bool IsBulletExamDoneConversationParameterSet()
    {
        if (ConversationManager.Instance == null || string.IsNullOrWhiteSpace(bulletCpExamDoneParameterName))
            return false;

        return ConversationManager.Instance.GetBool(bulletCpExamDoneParameterName);
    }

    private void BeginBulletCpResultDialogue(bool examDone)
    {
        bulletCpResultDialoguePlaying = true;
        Lvl3DialogueLine[] resultDialogue = examDone
            ? bulletCpExamDoneDialogue
            : bulletCpExamIncompleteDialogue;

        if (resultDialogue != null && resultDialogue.Length > 0)
        {
            PlayDialogue(null, resultDialogue);
            return;
        }

        FinishBulletCpResult(examDone);
    }

    private void FinishBulletCpResult(bool examDone)
    {
        bulletCpResultDialoguePlaying = false;

        if (examDone)
        {
            pendingBulletCp?.CompleteSuccessfulExamination();
            CompleteExamClueRegistration(true);
        }
        else
        {
            bulletCpExamAttempted = true;
        }

        pendingBulletCp = null;
        bulletExamDone = false;
        ReleaseBulletCpWorldInput();
    }

    private Lvl3DialogueLine[] GetBulletCpAttemptDialogue()
    {
        return bulletCpExamAttempted ? bulletCpRetryDialogue : bulletCpDialogue;
    }

    private void LockBulletCpWorldInput()
    {
        PlayerController.SetWorldInputLocked(true);
    }

    private void ReleaseBulletCpWorldInput()
    {
        PlayerController.SetWorldInputLocked(false);
    }

    private void StopWaitingForBulletCpConversation()
    {
        if (bulletCpConversationActive)
            ConversationManager.OnConversationEnded -= HandleBulletCpConversationEnded;

        if (bulletCpConversationCoroutine != null)
            StopCoroutine(bulletCpConversationCoroutine);

        bulletCpAwaitingConversation = false;
        bulletCpConversationActive = false;
        bulletCpConversationStarting = false;
        bulletCpPlayer = null;
        bulletCpConversationCoroutine = null;
        SetWatsonExamAnimation(false);
    }

    private void SetWatsonExamAnimation(bool isExamining)
    {
        if (watsonAnimator != null && !string.IsNullOrWhiteSpace(watsonExamAnimatorBool))
            watsonAnimator.SetBool(watsonExamAnimatorBool, isExamining);
    }

    private void AddAllCpCollectedClue()
    {
        if (allCpCollectedClueAdded || allCpCollectedClueIndex < 0 || interactable == null)
            return;

        allCpCollectedClueAdded = true;
        interactable.AddClue(allCpCollectedClueIndex, allCpCollectedClueCardPosition);
    }

    private Lvl3DialogueLine[] GetClueDialogue(int clueIndex)
    {
        return clueIndex switch
        {
            0 => ringCpDialogue,
            1 => paperCpDialogue,
            2 => bulletCpDialogue,
            _ => null
        };
    }

    private void EndExamination(bool completed)
    {
        examinationCompleted |= completed;
        interactionPerforming = false;
        examiningPlayer = null;
        isPlayerInsideExaminationZone = false;
        CancelPendingTutorialPopup();

        cameraController?.StopScriptedHorizontalOrbit();
        cameraController?.SetZoomState(CameraZoomState.Medium);
        isExaminationCameraActive = false;

        if (examinationZone != null)
        {
            examinationZone.SetActive(false);
            Destroy(examinationZone);
            examinationZone = null;
        }

        if (completed)
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

            enabled = false;
        }
    }

    private void BeginExamination(PlayerController player)
    {
        interactionPerforming = player != null;
        examiningPlayer = player;
        CreateExaminationZone();
        isPlayerInsideExaminationZone = IsPlayerInsideExaminationZone();

        if (isPlayerInsideExaminationZone)
            EnterExaminationCamera();
        else
            ExitExaminationCamera();
    }

    private void SetExaminationClueObjectsActive(bool active)
    {
        if (examinationClueObjects == null)
            return;

        foreach (GameObject clueObject in examinationClueObjects)
        {
            if (clueObject != null)
                clueObject.SetActive(active);
        }
    }

    private void EnterExaminationCamera()
    {
        SetExaminationGuidanceVisible(false);

        if (isExaminationCameraActive)
            return;

        isExaminationCameraActive = cameraController != null &&
                                  cameraController.SetZoomPreset(examinationPresetName);
        cameraController?.OrbitHorizontalAxisTo(examinationHorizontalAxis, examinationOrbitSpeed);

        ShowExaminationTutorialsIfReady();
    }

    private void ExitExaminationCamera()
    {
        if (!examinationCompleted && interactable != null)
            interactable.isInteractableActive = true;

        if (!isExaminationCameraActive)
        {
            SetExaminationGuidanceVisible(true);
            return;
        }

        cameraController?.StopScriptedHorizontalOrbit();
        cameraController?.SetZoomState(CameraZoomState.Medium);
        isExaminationCameraActive = false;
        CancelPendingTutorialPopup();

        SetExaminationGuidanceVisible(true);
    }

    private void ShowTutorialPopupIfNeeded()
    {
        if (!initialDialogueCompleted || !showTutorialPopup || tutorialPopupShown || tutorialPopupPending || TutorialTimeline.Instance == null)
            return;

        tutorialPopupPending = true;
        tutorialPopupCoroutine = StartCoroutine(ShowTutorialPopupAfterCameraSettles());
    }

    private IEnumerator ShowTutorialPopupAfterCameraSettles()
    {
        if (tutorialPopupDelay > 0f)
            yield return new WaitForSecondsRealtime(tutorialPopupDelay);

        tutorialPopupCoroutine = null;
        tutorialPopupPending = false;

        if (!interactionPerforming || !isExaminationCameraActive || TutorialTimeline.Instance == null)
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

    private void ShowExaminationTutorialsIfReady()
    {
        if (!initialDialogueCompleted)
            return;

        ShowTutorialPopupIfNeeded();
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (waitingForInitialDialogue && lines == initialExaminationDialogue)
        {
            waitingForInitialDialogue = false;
            initialDialogueCompleted = true;

            if (interactionPerforming && isExaminationCameraActive)
                ShowExaminationTutorialsIfReady();
            return;
        }

        if (bulletCpAwaitingConversation && lines == GetBulletCpAttemptDialogue())
        {
            StartBulletCpConversation();
            return;
        }

        if (bulletCpResultDialoguePlaying && lines == bulletCpExamDoneDialogue)
        {
            if (bulletIdeaPointToCheck != null && !bulletIdeaPointToCheck.IsDiscovered &&
                bulletIdeaPointMissingDialogue != null && bulletIdeaPointMissingDialogue.Length > 0)
            {
                PlayDialogue(null, bulletIdeaPointMissingDialogue);
                return;
            }

            FinishBulletCpResult(true);
            return;
        }

        if (bulletCpResultDialoguePlaying && lines == bulletIdeaPointMissingDialogue)
        {
            FinishBulletCpResult(true);
            return;
        }

        if (bulletCpResultDialoguePlaying && lines == bulletCpExamIncompleteDialogue)
        {
            FinishBulletCpResult(false);
            return;
        }

        if (!edithIdeaRevealed)
            return;

        if (IsCpDialogue(lines))
        {
            ContinueAfterFinalClueDialogue();
            return;
        }

        if (lines == allCpCollectedDialogue)
        {
            completionDialoguePending = false;
            FinishCompletedExamination();
        }
    }

    private void ContinueAfterFinalClueDialogue()
    {
        if (allCpCollectedDialogue != null && allCpCollectedDialogue.Length > 0)
        {
            PlayDialogue(null, allCpCollectedDialogue);
            return;
        }

        completionDialoguePending = false;
        FinishCompletedExamination();
    }

    private void FinishCompletedExamination()
    {
        if (completionTutorialCoroutine != null)
            return;

        // Keep this interaction alive until the IdeaPoint has been shown.
        completionDialoguePending = true;
        completionTutorialCoroutine = StartCoroutine(FinishExaminationAfterIdeaReveal());
    }

    private IEnumerator FinishExaminationAfterIdeaReveal()
    {
        if (edithIdeaPoint != null)
        {
            while (!edithIdeaPoint.IsDiscovered)
                yield return null;
        }

        completionDialoguePending = false;
        completionTutorialCoroutine = null;
        interactable?.MarkCompleted();
        EndExamination(true);
    }

    private bool IsCpDialogue(Lvl3DialogueLine[] lines)
    {
        return lines == ringCpDialogue ||
               lines == paperCpDialogue ||
               lines == bulletCpDialogue;
    }

    private void SetExaminationGuidanceVisible(bool visible)
    {
        if (interactable == null || examinationCompleted)
            return;

        if (interactable.interactiveShader != null)
            interactable.interactiveShader.SetActive(visible);

        bool eagleVisionActive = EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
        interactable.SetQuestionFXEagleVisionState(visible && eagleVisionActive);
    }

    private void CreateExaminationZone()
    {
        if (examinationZone != null)
            Destroy(examinationZone);

        examinationZone = new GameObject("EdithBody_ExaminationZone");
        examinationZone.transform.SetParent(GetExaminationZoneAnchor(), false);
        examinationZone.transform.localPosition = examinationZoneOffset;
        examinationZone.layer = LayerMask.NameToLayer("Ignore Raycast");

        SphereCollider zoneCollider = examinationZone.AddComponent<SphereCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.radius = examinationZoneRange;
    }

    private bool IsPlayerInsideExaminationZone()
    {
        if (examiningPlayer == null)
            return false;

        Vector3 zoneCenter = GetExaminationZoneAnchor().TransformPoint(examinationZoneOffset);
        return Vector3.Distance(examiningPlayer.transform.position, zoneCenter) <= examinationZoneRange;
    }

    private Transform GetExaminationZoneAnchor()
    {
        return interactable != null && interactable.interactabePoint != null
            ? interactable.interactabePoint
            : transform;
    }

    private void FindEdithIdeaPointIfNeeded()
    {
        if (edithIdeaPoint != null)
            return;

        DetectiveIdeaPoint[] ideaPoints = FindObjectsByType<DetectiveIdeaPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (DetectiveIdeaPoint point in ideaPoints)
        {
            if (point.ideaId == "IdeaPoint_Edith" || point.gameObject.name == "IdeaPoint_Edith")
            {
                edithIdeaPoint = point;
                edithIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;
                return;
            }
        }

        Debug.LogWarning("Int_EdithExamBody: IdeaPoint_Edith was not found.");
    }



    private void PerformWatsonAction()
    {
        watsonNavMesh.SetDestination(watsonPosition.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = interactionPerforming
            ? new Color(0.3f, 0.8f, 0.5f, 0.7f)
            : new Color(0.4f, 0.7f, 1f, 0.45f);
        Gizmos.DrawWireSphere(GetExaminationZoneAnchor().TransformPoint(examinationZoneOffset), examinationZoneRange);
    }

    //private void CheckWatsonArrival()
    //{

    //    if (watsonNavMesh.pathPending)
    //        return;

    //    if (watsonNavMesh.remainingDistance > watsonNavMesh.stoppingDistance + 0.05f)
    //        return;

    //    if (watsonNavMesh.hasPath && watsonNavMesh.velocity.sqrMagnitude > 0.01f)
    //        return;

    //    StartCoroutine(WatsonRotateAndPerform());

    //}
    private IEnumerator WatsonRotateAndPerform()
    {


        watsonNavMesh.updateRotation = false;

        Quaternion targetRotation = watsonPosition.rotation;

        while (Quaternion.Angle(watsonGO.transform.rotation, targetRotation) > 1f)
        {
            watsonGO.transform.rotation = Quaternion.RotateTowards(
                watsonGO.transform.rotation,
                targetRotation,
                10 * Time.deltaTime
            );

            yield return null;
        }

        watsonGO.transform.rotation = targetRotation;
        watsonNavMesh.updateRotation = true;

        watsonAnimator.SetBool("AnimEdithExam", true);

    }



    // ---------------------------------------------------------------
    // SYSTEM ZAPISU - postep badania ciala Lady Edith
    // ---------------------------------------------------------------

    public EdithExamSaveData GetSaveState()
    {
        EdithExamSaveData d = new EdithExamSaveData();

        d.performed = performed;
        d.collectedExamClueCount = collectedExamClueCount;
        d.ringCpCollected = ringCpCollected;
        d.paperCpCollected = paperCpCollected;
        d.bulletCpExamAttempted = bulletCpExamAttempted;
        d.edithIdeaRevealed = edithIdeaRevealed;
        d.examinationCompleted = examinationCompleted;
        d.allCpCollectedClueAdded = allCpCollectedClueAdded;
        d.arthurFoundItemsObjectiveAdded = arthurFoundItemsObjectiveAdded;
        d.tutorialPopupShown = tutorialPopupShown;

        // Stan poszczegolnych punktow badania czytamy wprost z ich komponentow.
        d.ringPerformed = GetRingCp() != null && GetRingCp().performed;
        d.paperPerformed = GetPaperCp() != null && GetPaperCp().performed;
        d.bulletPerformed = GetBulletCp() != null && GetBulletCp().performed;

        return d;
    }

    public void RestoreSaveState(EdithExamSaveData d)
    {
        if (d == null || !d.performed)
        {
            Debug.Log("Int_EdithExamBody: brak zapisanego postepu badania ciala.");
            return;
        }

        performed = true;
        collectedExamClueCount = d.collectedExamClueCount;
        ringCpCollected = d.ringCpCollected;
        paperCpCollected = d.paperCpCollected;
        bulletCpExamAttempted = d.bulletCpExamAttempted;
        edithIdeaRevealed = d.edithIdeaRevealed;
        allCpCollectedClueAdded = d.allCpCollectedClueAdded;
        arthurFoundItemsObjectiveAdded = d.arthurFoundItemsObjectiveAdded;
        tutorialPopupShown = d.tutorialPopupShown;

        // Dialog wstepny byl juz odegrany - nie odtwarzamy go ponownie.
        waitingForInitialDialogue = false;
        initialDialogueCompleted = true;

        // Cialo bylo juz zbadane, wiec punkty badania musza byc widoczne.
        SetExaminationClueObjectsActive(true);

        if (intCollider != null)
            intCollider.enabled = true;

        if (nextInteractions != null)
            nextInteractions.SetActive(true);

        // Warunki w rozmowach Arthura (pierscien / papier).
        if (ringCpCollected)
            SetArthurConditionByName(arthurRingParameterName);

        if (paperCpCollected)
            SetArthurConditionByName(arthurPaperParameterName);

        // Zebrane punkty badania oznaczamy jako wykonane, zeby nie dawaly
        // poszlaki i notatki po drugim razie.
        if (d.ringPerformed)
            WylaczZebranyPunkt(GetRingCp() != null ? GetRingCp().gameObject : null);

        if (d.paperPerformed)
        {
            Int_Edith_Paper paperCp = GetPaperCp();
            if (paperCp != null)
            {
                paperCp.performed = true;

                if (paperCp.paper != null)
                    paperCp.paper.SetActive(false);

                WylaczZebranyPunkt(paperCp.gameObject);
            }
        }

        if (d.bulletPerformed)
            WylaczZebranyPunkt(GetBulletCp() != null ? GetBulletCp().gameObject : null);

        // Panel zadan dostaje licznik z prawdziwego stanu.
        CluesLog.Instance?.SetLadyEdithBodyProgress(collectedExamClueCount, requiredCluesToRevealIdea);

        Debug.Log("Int_EdithExamBody: przywrocono badanie ciala (" + collectedExamClueCount +
                  "/" + requiredCluesToRevealIdea + ").");

        // Badanie bylo domkniete - wygaszamy interakcje calkowicie.
        if (d.examinationCompleted || d.edithIdeaRevealed)
        {
            completionDialoguePending = false;
            EndExamination(true);
        }
    }

    private void SetArthurConditionByName(string parameterName)
    {
        if (arthurSmartNpc == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        SetConversationBoolParameter(arthurSmartNpc.rozmowaDlaPostaciA, parameterName, true);
        SetConversationBoolParameter(arthurSmartNpc.rozmowaDlaPostaciB, parameterName, true);
    }

    // Ustawia flage 'performed' na punkcie badania i wygasza jego Interactable.
    private void WylaczZebranyPunkt(GameObject cpObject)
    {
        if (cpObject == null)
            return;

        Int_Edith_Ring ring = cpObject.GetComponent<Int_Edith_Ring>();
        if (ring != null) ring.performed = true;

        Int_Edith_BulletHole bullet = cpObject.GetComponent<Int_Edith_BulletHole>();
        if (bullet != null) bullet.performed = true;

        Interactable cpInteractable = cpObject.GetComponent<Interactable>();
        if (cpInteractable != null)
        {
            cpInteractable.isInteractableActive = false;
            cpInteractable.allowQuestionFXWhenInactive = false;
            cpInteractable.SetQuestionFXEagleVisionState(false);
            cpInteractable.interactiveShader = null;
        }
    }

    private Int_Edith_Ring GetRingCp()
    {
        return GetComponentInChildren<Int_Edith_Ring>(true);
    }

    private Int_Edith_Paper GetPaperCp()
    {
        return GetComponentInChildren<Int_Edith_Paper>(true);
    }

    private Int_Edith_BulletHole GetBulletCp()
    {
        return GetComponentInChildren<Int_Edith_BulletHole>(true);
    }
}