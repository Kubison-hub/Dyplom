using System.Collections;
using UnityEngine;

public class int_LibraryPainting : Lvl3InteractionDialogueBase
{

    private Interactable interactable;
    private Collider interactionCollider;
    [SerializeField] private CameraController cameraController;
    [SerializeField, Min(0f)] private float cameraTransitionDuration = 2.5f;

    public bool performed = false;

    public float pushDuration = 1f;
    public float targetX = -16.8f;
    private Coroutine moveCoroutine;
    private bool paintingPushed;

    [Header("Inspection Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] inspectionDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Za tym obrazem na pewno znajduje się jakaś skrytka...",
            duration = 3f
        }
    };

    [Header("Watson Inspection Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] watsonInspectionDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Za tym obrazem może znajdować się ukryty mechanizm.",
            duration = 3f
        }
    };

    protected override Lvl3DialogueLine[] DefaultDialogueLines => inspectionDialogue;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable?.SetWatsonInteractionAllowed(true);
        interactionCollider = GetComponent<Collider>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

    }

    public void PerformInteraction(PlayerController player)
    {
        cameraController?.SetZoomPreset("Narrow", cameraTransitionDuration);

        if (!paintingPushed)
            PlayDialogue(player, IsWatson(player) ? watsonInspectionDialogue : inspectionDialogue);

        if (player != null)
            player.currentInteractable = null;

    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
    }

    public void PushPainting()
    {
        paintingPushed = true;
        MovePaintingToX(targetX);
    }

    public void MovePaintingToX(float destinationX)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MovePainting(destinationX));
    }

    public void DeactivatePaintingInteraction()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.MarkCompleted();
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }

    private IEnumerator MovePainting(float destinationX)
    {
        float startX = transform.position.x;
        float elapsedTime = 0f;

        while (elapsedTime < pushDuration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime = elapsedTime / pushDuration;

            
            float newX = Mathf.Lerp(startX, destinationX, normalizedTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            
            yield return null;
        }


        transform.position = new Vector3(destinationX, transform.position.y, transform.position.z);
        moveCoroutine = null;
        DeactivatePaintingInteraction();
    }
}
    
