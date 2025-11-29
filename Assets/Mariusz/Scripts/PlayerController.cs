using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Potrzebne do wykrywania klikniêæ na UI
using DialogueEditor;           // Potrzebne do integracji z systemem dialogowym

/// <summary>
/// Klasa PlayerController zaimplementowana z blokadami dla Dialogów, Dziennika i UI.
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

    // Interakcja
    [HideInInspector] public Vector3 targetPosition;
    public Interactable currentInteractable = null;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Upewniamy siê, ¿e Rigidbody nie koliduje fizycznie (u¿ywamy CharacterController)
        if (rb.isKinematic == false) rb.isKinematic = true;

        targetPosition = transform.position;

        if (playerCharacter == PlayerCharacter.None)
        {
            Debug.LogError("PlayerCharacter is None!");
        }
    }

    private void Update()
    {
        // --- 1. SPRAWDZANIE BLOKAD (DIALOG / DZIENNIK) ---

        // Czy trwa dialog?
        bool dialogAktywny = (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive);

        // Czy otwarty jest dziennik? (Sprawdzamy null, ¿eby nie wywali³o b³êdu jeœli nie ma Managera)
        bool dziennikAktywny = (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen);

        // Jeœli któraœ z blokad jest aktywna...
        if (dialogAktywny || dziennikAktywny)
        {
            // ...a postaæ by³a w trakcie ruchu -> zatrzymaj j¹ natychmiast.
            if (isWalking)
            {
                isWalking = false;
                animator.SetBool("IsWalking", false);
            }

            // Przerwij funkcjê Update (nie wykonuj ruchu)
            return;
        }

        // Jeœli brak blokad, obs³uguj ruch i animacje normalnie
        HandleMovement();
        HandleAnimations();
    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // --- 2. BLOKADA KLIKANIA MYSZK¥ ---

        // A. Jeœli trwa dialog -> ignoruj klikniêcie
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
        {
            return;
        }

        // B. Jeœli otwarty jest dziennik -> ignoruj klikniêcie
        if (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen)
        {
            return;
        }

        // C. Jeœli kursor jest nad elementem UI (np. przycisk, panel) -> ignoruj klikniêcie
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // --- KONIEC BLOKAD, WYKONAJ RAYCAST ---

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

        // SprawdŸ czy doszliœmy do celu
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

        // Ruch postaci
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
        // Kod zakomentowany zgodnie z orygina³em, odkomentuj jeœli chcesz dŸwiêki
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