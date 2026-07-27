using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_lamp : MonoBehaviour
{
    [SerializeField] private DetectiveIdeaPoint lampIdeaPoint;
    [SerializeField, TextArea] private string topText =
        "Ta lampa mogla oswietlic wiecej niz tylko zakurzony pokoj.";

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;
    private bool completed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_lamp);

        if (lampIdeaPoint != null)
        {
            lampIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.MagnifierOrExternal;
            lampIdeaPoint.OnDiscovered += HandleIdeaPointDiscovered;

            if (lampIdeaPoint.IsDiscovered)
                CompleteInteraction();
        }
    }

    private void OnDestroy()
    {
        if (lampIdeaPoint != null)
            lampIdeaPoint.OnDiscovered -= HandleIdeaPointDiscovered;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed)
            return;

        if (player != null)
            player.currentInteractable = null;

        performed = true;

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");

        lampIdeaPoint?.RevealFromExternalSource();
        CompleteInteraction();
    }

    private void HandleIdeaPointDiscovered(DetectiveIdeaPoint discoveredPoint)
    {
        performed = true;
        CompleteInteraction();
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
        performed = true;

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;

    }
}
