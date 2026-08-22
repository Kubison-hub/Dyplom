using System.Collections;

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;


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
    private Animator watsonAnimator;
    private Animator sherlockAnimator;

    public Transform watsonPosition;
    [SerializeField]private Collider intCollider;

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
    [Header("Shot Tutorial")]
    [SerializeField, TextArea] private string shotTutorialText = "Tutorial Znajdywanie";
    [SerializeField] private string shotTutorialId = "ShotTutorial";
    [SerializeField, Min(0f)] private float shotTutorialDelay = 1.5f;
    [SerializeField, Range(0.1f, 1f)] private float shotTutorialScale = 0.5f;
    [SerializeField] private Vector2 shotTutorialOffset = new Vector2(180f, 110f);
    [SerializeField] private bool useLegacyShotTutorial;
    [Header("Tutorial Popup")]
    [SerializeField] private bool showTutorialPopup = true;
    [SerializeField, Min(0f)] private float tutorialPopupDelay = 1f;
    [SerializeField] private string tutorialPopupTitle = "BADANIE CIALA";
    [SerializeField, TextArea] private string tutorialPopupText;
    [SerializeField] private VideoClip tutorialPopupVideoClip;
    [Header("Initial Examination Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] initialExaminationDialogue =
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
    [SerializeField] private Lvl3DialogueLine[] allCpCollectedDialogue;
    [Header("Detective Idea")]
    [SerializeField] private DetectiveIdeaPoint edithIdeaPoint;
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
    private bool shotTutorialRequested;
    private bool tutorialPopupShown;
    private bool tutorialPopupPending;
    private Coroutine tutorialPopupCoroutine;
    private bool waitingForInitialDialogue;
    private bool initialDialogueCompleted;
    private bool completionDialoguePending;

    public bool IsExaminationCompleted => examinationCompleted || edithIdeaRevealed;
    protected override Lvl3DialogueLine[] DefaultDialogueLines => initialExaminationDialogue;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
        FindEdithIdeaPointIfNeeded();
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
        //watsonNavMesh = watsonGO.GetComponent<NavMeshAgent>();
        //watsonAnimator = watsonGO.GetComponent <Animator>();
        //sherlockAnimator = sherlockGO.GetComponent<Animator>();

        //splineDolly = interactionCamera.GetComponent<CinemachineSplineDolly>();

        
    }

    private void Update()
    {
        if (examinationCompleted || !interactionPerforming || examiningPlayer == null)
            return;

        if (edithIdeaRevealed)
        {
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
            BeginExamination(player);

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

        Lvl3DialogueLine[] clueDialogue = GetClueDialogue(clueIndex);
        if (clueDialogue != null && clueDialogue.Length > 0)
            PlayDialogue(player, clueDialogue);

        collectedExamClueCount++;
        if (collectedExamClueCount < requiredCluesToRevealIdea)
            return;

        completionDialoguePending = allCpCollectedDialogue != null && allCpCollectedDialogue.Length > 0;
        edithIdeaRevealed = true;
        FindEdithIdeaPointIfNeeded();
        edithIdeaPoint?.RevealFromExternalSource();
        EndExamination(true);

        if (completionDialoguePending && (clueDialogue == null || clueDialogue.Length == 0))
        {
            completionDialoguePending = false;
            PlayDialogue(null, allCpCollectedDialogue);
        }
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

    public void ShotTutorial()
    {
        if (shotTutorialRequested)
            return;

        shotTutorialRequested = true;
        StartCoroutine(ShowShotTutorialAfterCameraSettles());
    }

    private IEnumerator ShowShotTutorialAfterCameraSettles()
    {
        if (shotTutorialDelay > 0f)
            yield return new WaitForSecondsRealtime(shotTutorialDelay);

        TutorialManager tutorialManager = TutorialManager.Instance;
        if (tutorialManager == null)
            yield break;

        tutorialManager.PokazTutorial(shotTutorialText, shotTutorialId);
        if (!tutorialManager.isTutorialActive || tutorialManager.tutorialPanel == null)
            yield break;

        Transform tutorialPanelTransform = tutorialManager.tutorialPanel.transform;
        Vector3 originalScale = tutorialPanelTransform.localScale;
        tutorialPanelTransform.localScale = originalScale * shotTutorialScale;

        RectTransform tutorialPanelRect = tutorialPanelTransform as RectTransform;
        Vector2 originalAnchoredPosition = tutorialPanelRect != null
            ? tutorialPanelRect.anchoredPosition
            : Vector2.zero;

        if (tutorialPanelRect != null)
            tutorialPanelRect.anchoredPosition = originalAnchoredPosition + shotTutorialOffset;

        while (tutorialManager.isTutorialActive)
            yield return null;

        if (tutorialPanelTransform != null)
            tutorialPanelTransform.localScale = originalScale;

        if (tutorialPanelRect != null)
            tutorialPanelRect.anchoredPosition = originalAnchoredPosition;
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

        if (useLegacyShotTutorial)
            ShotTutorial();

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

        if (!completionDialoguePending || !IsCpDialogue(lines))
            return;

        completionDialoguePending = false;
        PlayDialogue(null, allCpCollectedDialogue);
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

    
}
