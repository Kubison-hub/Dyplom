using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_WindowBulletCP : MonoBehaviour
{
    [SerializeField] private Int_lv1_WindowBullet windowExamination;
    [SerializeField] private AudioSource foundAudio;

    private Interactable interactable;
    private Collider interactionCollider;
    private Renderer[] objectRenderers;
    private bool performed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        objectRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        foundAudio?.Play();
        windowExamination?.RegisterWindowClue();

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText("Doskonale", "");

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

        foreach (Renderer objectRenderer in objectRenderers)
        {
            if (objectRenderer != null)
                objectRenderer.enabled = false;
        }

        if (player != null)
            player.currentInteractable = null;
    }
}
