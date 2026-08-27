using DialogueEditor;
using System.Collections;           // Potrzebne do integracji z systemem dialogowym
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems; // Potrzebne do wykrywania kliknięć na UI
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Klasa PlayerController zaimplementowana z blokadami dla Dialogów, Dziennika i UI.
/// </summary>


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NavMeshAgent))]

public class PlayerController : MonoBehaviour
{

    public bool isActivePlayer = false;

    public PlayerCharacter playerCharacter = PlayerCharacter.None;

    [Header("Animation")]
    [Tooltip("Assign the Animator that drives this character's visible model. If empty, the Animator on this GameObject is used.")]
    [SerializeField] private Animator animator;
    private Rigidbody rb;
    public NavMeshAgent navMeshAgent;
    private bool hasTemporaryMovementSpeed;
    private float temporaryMovementSpeed;

    public LayerMask interactableMask;
    public LayerMask groundMask;

    public Interactable currentInteractable = null;
    public Transform currentInteractionPoint;
    public float interactionPointRotationSpeed = 360f;

    private bool hasAutoInteractionApproachPoint;
    private Vector3 autoInteractionApproachPoint;
    
    public AudioClip[] FootstepAudioClips;

    [Header("Unreachable Interaction Feedback")]
    [SerializeField, TextArea] private string sherlockUnreachableInteractionText = "Nie mogę się tam dostać.";
    [SerializeField] private AudioClip sherlockUnreachableInteractionAudioClip;
    [SerializeField, TextArea] private string watsonUnreachableInteractionText = "Nie mogę się tam dostać.";
    [SerializeField] private AudioClip watsonUnreachableInteractionAudioClip;

    private bool canMove = true;
    private bool tutorialMovementLocked;
    private bool minigameMovementLocked;
    private PlayerInput playerInput;

    public bool isWalking = false;
    public bool lookAtCamera = false;

    public float normalSpeed = 2.5f;
    public float thinkingMultiplier = 0.6f;
    // Interakcja
    [HideInInspector] public Vector3 targetPosition;

    private bool isThinking = false;
    private bool isPerformingInteraction = false;
    private bool isWaitingForInteractionReaction;
    private NavMeshPath lightMazePath;
    private Interactable hoveredInteractable;
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogError($"{name}: PlayerController could not find a character Animator.", this);
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        // Upewniamy się, że Rigidbody nie koliduje fizycznie (używamy CharacterController)
        if (rb.isKinematic == false) rb.isKinematic = true;

        if (playerCharacter == PlayerCharacter.None)
        {
            Debug.LogError("PlayerCharacter is None!");
        }

        targetPosition = transform.position;

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null) Debug.LogError("NavMeshAgent is null");
        lightMazePath = new NavMeshPath();
    }

    private void Update()
    {
        CheckUILock();
        
        CheckInteractionArrival();

        HandleAnimations();

        UpdateMovementSpeed();
        UpdateInteractionShaderHover();


    }


    private void UpdateInteractionShaderHover()
    {
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
        {
            SetHoveredInteractable(null);
            return;
        }

        if (playerInput == null || !playerInput.enabled || Mouse.current == null || Camera.main == null)
        {
            SetHoveredInteractable(null);
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetHoveredInteractable(null);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Interactable nextHovered = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableMask))
            nextHovered = hit.collider.GetComponentInParent<Interactable>();

        SetHoveredInteractable(nextHovered);
    }

    private void SetHoveredInteractable(Interactable nextHovered)
    {
        if (hoveredInteractable == nextHovered)
            return;

        if (hoveredInteractable != null)
            hoveredInteractable.SetInteractionShaderHover(false);

        hoveredInteractable = nextHovered;

        if (hoveredInteractable != null)
            hoveredInteractable.SetInteractionShaderHover(true);
    }

    private void OnDisable()
    {
        SetHoveredInteractable(null);
    }
    private void CheckUILock()
    {
        // --- 1. SPRAWDZANIE BLOKAD (DIALOG / DZIENNIK) ---

        // Czy trwa dialog?
        bool dialogAktywny = (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive);

        // Czy otwarty jest dziennik? (Sprawdzamy null, żeby nie wywaliło błędu jeśli nie ma Managera)
        bool dziennikAktywny = (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen);
        bool notebookAktywny = NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen;

        // Jeśli któraś z blokad jest aktywna...
        if (dialogAktywny || dziennikAktywny || notebookAktywny || tutorialMovementLocked || minigameMovementLocked)
        {
            // ...zablokuj NavMesh.
            LockMovement();

            // Przerwij funkcję Update (nie wykonuj ruchu)
            return;
        }
        else
        {
            // Jeżeli nie, odblokuj NavMesh
            UnlockMovement();
        }
    }

    public void LockMovement()
    {
        canMove = false;
        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    public void UnlockMovement()
    {
        canMove = true;
        navMeshAgent.isStopped = false;
    }

    public void SetTutorialMovementLocked(bool locked)
    {
        tutorialMovementLocked = locked;

        if (tutorialMovementLocked)
            LockMovement();
    }


    public void SetMinigameMovementLocked(bool locked)
    {
        minigameMovementLocked = locked;

        if (minigameMovementLocked)
            LockMovement();
    }

    public void SetTutorialInputLocked(bool locked)
    {
        if (playerInput == null)
            return;

        if (locked)
            playerInput.DeactivateInput();
        else
            playerInput.ActivateInput();
    }
    //---------------------------------------------


    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            return;

        if (!context.performed || !canMove)
            return;

        HandleLeftClick();
    }

    private void HandleLeftClick()
    {
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput)
            return;

        if (TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput)
            return;

        if (TutorialTimeline.Instance != null)
            TutorialTimeline.Instance.NotifyWorldClick();

        if (SafeCodeDrumMinigame.IsPointerOverActiveBoard)
            return;

        if (DetectiveIdeaManager.Instance != null && DetectiveIdeaManager.Instance.TryHandlePointerPress())
            return;


        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableMask))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable == null)
            {
                EmptyWall emptyWall = hit.collider.GetComponentInParent<EmptyWall>();
                if (emptyWall != null)
                    interactable = emptyWall.GetComponent<Interactable>();
            }
            if (interactable != null)
            {
                interactable.TryToInteract(this);
                return;
            }
        }

        
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundMask))
        {
            if (TutorialTimeline.Instance != null)
                TutorialTimeline.Instance.NotifyIdeaPuzzleGroundClick();

            if (!CanMoveToPointInLightMaze(groundHit.point))
                return;

            // Escort uses its already visible world-space preview; this click confirms it.
            if (WatsonEscortController.Instance != null &&
                WatsonEscortController.Instance.TryBeginPlacementFromPointer(this))
                return;

            if (WatsonEscortController.Instance != null &&
                WatsonEscortController.Instance.TryHandleGroundClick(this, groundHit.point))
                return;

            if (WatsonCarryController.Instance != null &&
                WatsonCarryController.Instance.TryHandleGroundClick(this, groundHit.point))
                return;

            MoveToPoint(groundHit.point);
        }
    }

    private bool CanMoveToPointInLightMaze(Vector3 point)
    {
        if (lvl3_GameProgress.Instance == null || !lvl3_GameProgress.Instance.lightMazeMode)
            return true;

        if (!lvl3_GameProgress.Instance.lampPickedUp)
        {
            ShowMovementBlockedText("Za ciemno.", "Bez lampy nie powinienem isc dalej.");
            return false;
        }

        if (lightMazePath == null)
            lightMazePath = new NavMeshPath();

        if (!NavMesh.CalculatePath(transform.position, point, NavMesh.AllAreas, lightMazePath))
        {
            ShowMovementBlockedText("Za ciemno.", "Nie widze bezpiecznej drogi.");
            return false;
        }

        if (lightMazePath.status != NavMeshPathStatus.PathComplete)
        {
            ShowMovementBlockedText("Za ciemno.", "Nie widze bezpiecznej drogi.");
            return false;
        }

        float pathDistance = GetPathDistance(lightMazePath);
        if (pathDistance > lvl3_GameProgress.Instance.lightMazeRange)
        {
            ShowMovementBlockedText("Za ciemno.", "Musze podejsc blizej z lampa.");
            return false;
        }

        return true;
    }

    private float GetPathDistance(NavMeshPath path)
    {
        float distance = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return distance;
    }

    private void ShowMovementBlockedText(string title, string description)
    {
        if (PlayerTopText.Instance != null)
        {
            PlayerTopText.Instance.ShowTopText(title, description);
        }
        else
        {
            Debug.Log($"{title} {description}");
        }
    }
    public void MoveToPoint(Vector3 point)
    {
        float blockerRadius = navMeshAgent != null ? navMeshAgent.radius : 0.2f;
        if (WatsonCarryable.IsWorldPositionBlocked(point, blockerRadius))
        {
            currentInteractable = null;
            currentInteractionPoint = null;
            ClearAutoInteractionApproachPoint();
            return;
        }
        if (!NavMeshWallGuard.TryGetClearPath(navMeshAgent, point, out NavMeshPath clearPath))
            return;

        isPerformingInteraction = false;
        isWaitingForInteractionReaction = false;
        currentInteractable = null;
        currentInteractionPoint = null;
        ClearAutoInteractionApproachPoint();
        navMeshAgent.SetPath(clearPath);
        NavMeshDestinationMarker.GetOrCreate().ShowAt(point, navMeshAgent);
    }

    public void SetAutoInteractionApproachPoint(Vector3 point)
    {
        autoInteractionApproachPoint = point;
        hasAutoInteractionApproachPoint = true;
    }

    public void ClearAutoInteractionApproachPoint()
    {
        hasAutoInteractionApproachPoint = false;
    }

    public void SetWaitingForInteractionReaction(bool isWaiting)
    {
        isWaitingForInteractionReaction = isWaiting;
    }
    public void MoveToInteractable()
    {
        NavMeshDestinationMarker.HideCurrent();

        if (currentInteractable == null)
            return;

        if (currentInteractionPoint == null)
        {
            if (hasAutoInteractionApproachPoint)
            {
                if (!NavMeshWallGuard.TryGetClearPath(navMeshAgent, autoInteractionApproachPoint, out NavMeshPath approachPath))
                {
                    currentInteractable = null;
                    ClearAutoInteractionApproachPoint();
                    ShowUnreachableInteractionFeedback();
                    return;
                }

                navMeshAgent.SetPath(approachPath);
                return;
            }

            navMeshAgent.ResetPath();
            StartCoroutine(RotateAndPerform());
            return;
        }


        if (!NavMeshWallGuard.TryGetClearPath(navMeshAgent, currentInteractionPoint.position, out NavMeshPath interactionPath))
        {
            currentInteractable = null;
            currentInteractionPoint = null;
            ShowUnreachableInteractionFeedback();
            return;
        }

        navMeshAgent.SetPath(interactionPath);
    }

    private void ShowUnreachableInteractionFeedback()
    {
        bool isWatson = playerCharacter == PlayerCharacter.Watson || CompareTag("PlayerB");
        string text = isWatson ? watsonUnreachableInteractionText : sherlockUnreachableInteractionText;
        AudioClip audioClip = isWatson
            ? watsonUnreachableInteractionAudioClip
            : sherlockUnreachableInteractionAudioClip;

        if (PlayerTopText.Instance != null)
        {
            if (isWatson)
                PlayerTopText.Instance.ShowWatsonTopText(text);
            else
                PlayerTopText.Instance.ShowTopText(text, string.Empty);
        }
        else
        {
            Debug.Log(text);
        }

        if (audioClip == null)
            return;

        DialogueAudioRegistry registry = DialogueAudioRegistry.Instance;
        AudioSource voiceSource = isWatson
            ? registry != null ? registry.WatsonVoiceSource : null
            : registry != null ? registry.SherlockVoiceSource : null;

        if (voiceSource == null)
            return;

        voiceSource.Stop();
        voiceSource.PlayOneShot(audioClip);
    }

    public void RotateTowardsInteractableAndShowTopText(Interactable interactable, string message)
    {
        if (interactable == null)
            return;

        currentInteractable = interactable;
        currentInteractionPoint = null;
        navMeshAgent.ResetPath();
        StartCoroutine(RotateAndShowBlockedInteractionText(message));
    }

    private IEnumerator RotateAndShowBlockedInteractionText(string message)
    {
        if (currentInteractable == null)
            yield break;

        isPerformingInteraction = true;
        navMeshAgent.updateRotation = false;
        if (PlayerTopText.Instance != null)
        {
            if (playerCharacter == PlayerCharacter.Watson || CompareTag("PlayerB"))
                PlayerTopText.Instance.ShowWatsonTopText(message);
            else
                PlayerTopText.Instance.ShowTopText(message, string.Empty);
        }
        else
            Debug.Log(message);

        Quaternion targetRotation = GetCurrentInteractableRotation();
        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                interactionPointRotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;
        navMeshAgent.updateRotation = true;
        currentInteractable = null;
        currentInteractionPoint = null;
        isPerformingInteraction = false;
    }
    private void CheckInteractionArrival()
    {
        if (isPerformingInteraction || isWaitingForInteractionReaction)
            return;

        if (currentInteractable == null)
            return;

        if (navMeshAgent.pathPending)
            return;

        if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance + 0.05f)
            return;

        if (navMeshAgent.hasPath && navMeshAgent.velocity.sqrMagnitude > 0.01f)
            return;

        StartCoroutine(RotateAndPerform());

    }

    private IEnumerator RotateAndPerform()
    {
        if (currentInteractable == null)
            yield break;

        isPerformingInteraction = true;
        navMeshAgent.updateRotation = false;

        Quaternion targetRotation = GetCurrentInteractionTargetRotation();

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                interactionPointRotationSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.rotation = targetRotation;
        navMeshAgent.updateRotation = true;

        if (currentInteractable != null)
        {
            Interactable performedInteractable = currentInteractable;
            performedInteractable.PerformInteraction(this);
            if (performedInteractable.addDatabaseNotesAutomatically)
                performedInteractable.AddAllDatabaseNotes();
        }

        ClearAutoInteractionApproachPoint();
        isPerformingInteraction = false;

        
   
    }

    private Quaternion GetCurrentInteractableRotation()
    {
        if (currentInteractable == null)
            return transform.rotation;

        Vector3 directionToInteractable = currentInteractable.transform.position - transform.position;
        directionToInteractable.y = 0f;
        return directionToInteractable.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(directionToInteractable)
            : transform.rotation;
    }
    private Quaternion GetCurrentInteractionTargetRotation()
    {
        if (currentInteractionPoint != null)
            return currentInteractionPoint.rotation;

        if (currentInteractable == null)
            return transform.rotation;

        Vector3 directionToInteractable = currentInteractable.transform.position - transform.position;
        directionToInteractable.y = 0f;
        return directionToInteractable.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(directionToInteractable)
            : transform.rotation;
    }
    private void RotateToInteractionPoint2()
    {
        if (currentInteractable == null)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            currentInteractionPoint.rotation,
            360 * Time.deltaTime
        );
    }

    private float transitionSpeed = 2f; // 1 / 0.5s = 2 (prędkość zmiany)

    private void HandleAnimations()
    {
        if (animator == null)
            return;
       
        bool isWalking = navMeshAgent.velocity.magnitude > 0.1f;
        animator.SetBool("IsWalking", isWalking);

        if (playerCharacter == PlayerCharacter.Sherlock)
        {
            isThinking = EagleVisionSystem.Instance.isActive && SwitchCharacter.Instance.activePlayerIndex == 0;
            animator.SetBool("IsThinking", isThinking);
        }
        else
        {
            isThinking = false;
        }

        float targetL0 = 1f;
        float targetL1 = 0f;
        float targetWalkSpeed = 1f;

        if (isThinking)
        {
            if (isWalking)
            {
                targetL0 = 1f;
                targetL1 = 1f;
                targetWalkSpeed = 0.6f;
            }
            else
            {
                targetL0 = 0f;
                targetL1 = 1f;
                targetWalkSpeed = 1f;
            }
        }
        else
        {
            targetL0 = 1f;
            targetL1 = 0f;
            targetWalkSpeed = 1f;
        }

        float currentL0 = animator.layerCount > 0 ? animator.GetLayerWeight(0) : 0f;
        float currentL1 = animator.layerCount > 1 ? animator.GetLayerWeight(1) : 0f;
        float currentWalkSpeed = animator.GetFloat("WalkSpeed");

        if (animator.layerCount > 0)
            animator.SetLayerWeight(0, Mathf.MoveTowards(currentL0, targetL0, Time.deltaTime * transitionSpeed));

        if (animator.layerCount > 1)
            animator.SetLayerWeight(1, Mathf.MoveTowards(currentL1, targetL1, Time.deltaTime * transitionSpeed));

        animator.SetFloat("WalkSpeed", Mathf.MoveTowards(currentWalkSpeed, targetWalkSpeed, Time.deltaTime * transitionSpeed));
    }
    //private float layerTimer = 0f;
    //private void HandleAnimations()
    //{
    //    if (navMeshAgent.velocity.magnitude != 0f)
    //    {
    //        animator.SetBool("IsWalking", true);
    //    }
    //    else
    //    {
    //        animator.SetBool("IsWalking", false);
    //    }

    //    isThinking = EagleVisionSystem.Instance.isActive;
    //    animator.SetBool("IsThinking", isThinking);
    //    if (isThinking)
    //    {


    //        if (animator.GetBool("IsWalking"))
    //        {
    //            animator.SetLayerWeight(1, 1);
    //            animator.SetLayerWeight(0, 1);
    //            animator.SetFloat("WalkSpeed", 0.5f);
    //        }
    //        else
    //        {
    //            animator.SetLayerWeight(1, 1);
    //            animator.SetLayerWeight(0,0);
    //            animator.SetFloat("WalkSpeed", 1f);
    //        }

    //    }
    //    else
    //    {
    //        animator.SetFloat("WalkSpeed", 1f);
    //        animator.SetLayerWeight(0,1);
    //        animator.SetLayerWeight(1,0);
    //    }


    //}

    public void UpdateMovementSpeed()
    {
        if (navMeshAgent == null) return;

        navMeshAgent.speed = hasTemporaryMovementSpeed
            ? temporaryMovementSpeed
            : isThinking ? (normalSpeed * thinkingMultiplier) : normalSpeed;

        // Opcjonalnie: Zmień też szybkość obrotu, żeby postać była "cięższa"
        //navMeshAgent.angularSpeed = isThinking ? 60f : 120f;
    }

    public void SetTemporaryMovementSpeed(float speed)
    {
        temporaryMovementSpeed = Mathf.Max(0.1f, speed);
        hasTemporaryMovementSpeed = true;

        if (navMeshAgent != null)
            navMeshAgent.speed = temporaryMovementSpeed;
    }

    public void ClearTemporaryMovementSpeed()
    {
        hasTemporaryMovementSpeed = false;
        UpdateMovementSpeed();
    }

    public void OnDebugRestart(InputAction.CallbackContext context)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    //private void OnFootstep(AnimationEvent animationEvent)
    //{

    //    if (animationEvent.animatorClipInfo.weight > 0.5f)
    //    {
    //        if (FootstepAudioClips.Length > 0)
    //        {
    //            var index = Random.Range(0, FootstepAudioClips.Length);
    //            AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(transform.position), .8f);
    //        }
    //    }
    //}
}

public enum PlayerCharacter
{
    None,
    Sherlock,
    Watson
}
