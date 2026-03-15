using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Potrzebne do wykrywania klikniêæ na UI
using DialogueEditor;
using UnityEngine.AI;
using System.Collections;           // Potrzebne do integracji z systemem dialogowym

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
    public float interactionPointRotationSpeed = 10f;
    
    public AudioClip[] FootstepAudioClips;

    private bool canMove = true;

    public bool isWalking = false;
    public bool lookAtCamera = false;


    // Interakcja
    [HideInInspector] public Vector3 targetPosition;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Upewniamy siê, ¿e Rigidbody nie koliduje fizycznie (u¿ywamy CharacterController)
        if (rb.isKinematic == false) rb.isKinematic = true;

        if (playerCharacter == PlayerCharacter.None)
        {
            Debug.LogError("PlayerCharacter is None!");
        }

        targetPosition = transform.position;

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null) Debug.LogError("NavMeshAgent is null");
    }

    private void Update()
    {
        CheckUILock();
        
        CheckInteractionArrival();

        HandleAnimations();
    }


    private void CheckUILock()
    {
        // --- 1. SPRAWDZANIE BLOKAD (DIALOG / DZIENNIK) ---

        // Czy trwa dialog?
        bool dialogAktywny = (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive);

        // Czy otwarty jest dziennik? (Sprawdzamy null, ¿eby nie wywali³o b³êdu jeœli nie ma Managera)
        bool dziennikAktywny = (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen);

        // Jeœli któraœ z blokad jest aktywna...
        if (dialogAktywny || dziennikAktywny)
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


    //---------------------------------------------


    public void OnLeftClick(InputAction.CallbackContext context)
    {

        if (!context.performed || !canMove)
            return;

        HandleLeftClick();
    }

    private void HandleLeftClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableMask))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.TryToInteract(this);
                return;
            }
        }

        
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundMask))
        {
            MoveToPoint(groundHit.point);
        }
    }

    public void MoveToPoint(Vector3 point)
    {
        currentInteractable = null;
        navMeshAgent.destination = point;
    }

    public void MoveToInteractable()
    {
        navMeshAgent.SetDestination(currentInteractionPoint.position);
    }

    private void CheckInteractionArrival()
    {
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

        navMeshAgent.updateRotation = false;

        Quaternion targetRotation = currentInteractionPoint.rotation;

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

        currentInteractable.PerformInteraction(this);
        currentInteractable = null;
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

    private void HandleAnimations()
    {
        if (navMeshAgent.velocity.magnitude != 0f)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
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