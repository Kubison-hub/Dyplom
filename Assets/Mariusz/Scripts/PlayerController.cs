using DialogueEditor;
using System.Collections;           // Potrzebne do integracji z systemem dialogowym
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems; // Potrzebne do wykrywania klikniêæ na UI
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

    private Animator animator;
    private Rigidbody rb;
    public NavMeshAgent navMeshAgent;

    public LayerMask interactableMask;
    public LayerMask groundMask;

    public Interactable currentInteractable = null;
    public Transform currentInteractionPoint;
    public float interactionPointRotationSpeed = 360f;
    
    public AudioClip[] FootstepAudioClips;

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
    private NavMeshPath lightMazePath;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        // Upewniamy siê, ¿e Rigidbody nie koliduje fizycznie (u¿ywamy CharacterController)
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


    }


    private void CheckUILock()
    {
        // --- 1. SPRAWDZANIE BLOKAD (DIALOG / DZIENNIK) ---

        // Czy trwa dialog?
        bool dialogAktywny = (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive);

        // Czy otwarty jest dziennik? (Sprawdzamy null, ¿eby nie wywali³o b³êdu jeœli nie ma Managera)
        bool dziennikAktywny = (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen);

        // Jeœli któraœ z blokad jest aktywna...
        if (dialogAktywny || dziennikAktywny || tutorialMovementLocked || minigameMovementLocked)
        {
            // ...zablokuj NavMesh.
            LockMovement();

            // Przerwij funkcjê Update (nie wykonuj ruchu)
            return;
        }
        else
        {
            // Je¿eli nie, odblokuj NavMesh
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

        if (!context.performed || !canMove)
            return;

        HandleLeftClick();
    }

    private void HandleLeftClick()
    {
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
            Interactable interactable = hit.collider.GetComponent<Interactable>();

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
        isPerformingInteraction = false;
        currentInteractable = null;
        navMeshAgent.destination = point;
        NavMeshDestinationMarker.GetOrCreate().ShowAt(point, navMeshAgent);
    }

    public void MoveToInteractable()
    {
        NavMeshDestinationMarker.HideCurrent();
        navMeshAgent.SetDestination(currentInteractionPoint.position);
    }

    private void CheckInteractionArrival()
    {
        if (isPerformingInteraction)
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

        Quaternion targetRotation = currentInteractionPoint != null
            ? currentInteractionPoint.rotation
            : transform.rotation;

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
            currentInteractable.PerformInteraction(this);
        }

        isPerformingInteraction = false;

        
   
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

    private float transitionSpeed = 2f; // 1 / 0.5s = 2 (prêdkoœæ zmiany)

    private void HandleAnimations()
    {
       
        bool isWalking = navMeshAgent.velocity.magnitude > 0.1f;
        animator.SetBool("IsWalking", isWalking);

        isThinking = EagleVisionSystem.Instance.isActive && SwitchCharacter.Instance.activePlayerIndex == 0;
        animator.SetBool("IsThinking", isThinking);

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

        float currentL0 = animator.GetLayerWeight(0);
        float currentL1 = animator.GetLayerWeight(1);
        float currentWalkSpeed = animator.GetFloat("WalkSpeed");

        animator.SetLayerWeight(0, Mathf.MoveTowards(currentL0, targetL0, Time.deltaTime * transitionSpeed));
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

        navMeshAgent.speed = isThinking ? (normalSpeed * thinkingMultiplier) : normalSpeed;

        // Opcjonalnie: Zmieñ te¿ szybkoœæ obrotu, ¿eby postaæ by³a "ciê¿sza"
        //navMeshAgent.angularSpeed = isThinking ? 60f : 120f;
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