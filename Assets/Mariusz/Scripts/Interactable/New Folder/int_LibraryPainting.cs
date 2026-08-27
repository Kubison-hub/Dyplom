using System.Collections;
using UnityEngine;

public class int_LibraryPainting : MonoBehaviour
{

    private Interactable interactable;
    private Collider interactionCollider;
    [SerializeField] private CameraController cameraController;
    [SerializeField, Min(0.1f)] private float cameraTransitionSpeed = 0.5f;

    public bool performed = false;

    public float pushDuration = 1f;
    public float targetX = -16.8f;
    private Coroutine moveCoroutine;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

    }

    public void PerformInteraction(PlayerController player)
    {
        cameraController?.SetZoomPreset("Narrow", cameraTransitionSpeed);

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText("Za tym obrazem na pewno znajduje siê jakaœ skrytka...", "");

        if (player != null)
            player.currentInteractable = null;

    }

    public void PushPainting()
    {
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
        if (interactable != null)
        {
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
    }
}
    
