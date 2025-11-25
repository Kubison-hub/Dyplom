using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DialogueEditor; // 1. Dodano namespace do dialogów
using UnityEngine.EventSystems; // 2. Dodano namespace do wykrywania UI

/// <summary>
/// Klasa PlayerController z dodan¹ blokad¹ ruchu podczas dialogów.
/// </summary>

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]

public class PlayerController : MonoBehaviour
{
    public PlayerCharacter playerCharacter = PlayerCharacter.None;

    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    public float stoppingDistance = 0.1f;
    public LayerMask interactableMask;

    private CharacterController controller;
    private Animator animator;
    private Rigidbody rb;

    public bool isWalking = false;
    public bool lookAtCamera = false;

    public AudioClip[] FootstepAudioClips;

    //Interakcja
    [HideInInspector] public Vector3 targetPosition;
    public Interactable currentInteractable = null;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        if (rb.isKinematic == false) rb.isKinematic = true;

        targetPosition = transform.position;

        if (playerCharacter == PlayerCharacter.None)
        {
            Debug.LogError("PlayerCharacter is None!");
        }
    }

    private void Update()
    {
        // --- 3. AWARYJNE ZATRZYMANIE ---
        // Jeœli dialog siê rozpocz¹³ (np. przez Trigger), a postaæ sz³a - zatrzymaj j¹ natychmiast.
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
        {
            if (isWalking)
            {
                isWalking = false;
                animator.SetBool("IsWalking", false); // Wy³¹cz animacjê chodzenia
            }
            return; // Nie wykonuj reszty Update (HandleMovement)
        }

        HandleMovement();
        HandleAnimations();
    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // --- 4. BLOKADA KLIKANIA (INPUTU) ---

        // A. SprawdŸ, czy trwa dialog. Jeœli tak - ignoruj klikniêcie.
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
        {
            return;
        }

        // B. SprawdŸ, czy klikamy na UI (przyciski, t³o dialogu). Jeœli tak - ignoruj klikniêcie.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // --- KONIEC BLOKADY ---

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableMask))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null)
            {
                interactable.TryToInteract(this);
            }
            else
            {
                targetPosition = hit.point;
                currentInteractable = null;
                isWalking = true;
            }
        }
    }

    private void HandleMovement()
    {
        if (!isWalking) return;

        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0;

        if (direction.magnitude < stoppingDistance)
        {
            isWalking = false;

            if (currentInteractable != null)
            {
                currentInteractable.PerformInteraction(this);
                currentInteractable = null;
            }

            return;
        }

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        controller.Move(move);

        HandleRotation(direction);
    }

    private void HandleRotation(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation;

            if (lookAtCamera)
            {
                Vector3 cameraDirection = transform.position - Camera.main.transform.position;
                cameraDirection.y = 0;
                targetRotation = Quaternion.LookRotation(-cameraDirection);
            }
            else
            {
                targetRotation = Quaternion.LookRotation(direction);
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleAnimations()
    {
        if (isWalking) animator.SetBool("IsWalking", true);
        else animator.SetBool("IsWalking", false);
    }

    public void OnDebugRestart(InputAction.CallbackContext context)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        //if (animationEvent.animatorClipInfo.weight > 0.5f)
        //{
        //    if (FootstepAudioClips.Length > 0)
        //    {
        //        var index = Random.Range(0, FootstepAudioClips.Length);
        //        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(transform.position), .8f);
        //    }
        //}
    }
}

public enum PlayerCharacter
{
    None,
    Sherlock,
    Watson
}