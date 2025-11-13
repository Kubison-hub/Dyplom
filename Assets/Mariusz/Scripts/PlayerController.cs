
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


/// <summary>
/// Klasa PlayerController dzia³a w oparciu o NewPlayerInput (Unity.Events) i CharacterController (RigidBody u¿ywamy do detekcji triggerów).
/// OnLeftClick(InputAction.CallbackContext context) pobiera Raycast Camera.main.ScreenPointToRay do wykrywana interakcji i poruszania siê.
/// Poruszanie dzia³a na zasadzie direction i zmiennej isWalking. PlayerCharacter okreœla czy Controller jest Shelockiem czy Watsonem (dla Interakcji)
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
        if (rb.isKinematic ==  false) rb.isKinematic = true;

        targetPosition = transform.position;

        if (playerCharacter == PlayerCharacter.None)
        {
            Debug.LogError("PlayerCharacter is None!");
        }   
    }

    private void Update()
    {
        HandleMovement();
        HandleAnimations();
    }


    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

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
        if(!isWalking) return;

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