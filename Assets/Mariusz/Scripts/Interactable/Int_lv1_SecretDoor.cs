using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SecretDoor : MonoBehaviour
{
    [Header("Narration")]
    [SerializeField, TextArea] private string topText =
        "W jaki sposób otworzyć to tajne przejście?";

    [Header("Footsteps Tutorial")]
    [SerializeField, Min(0f)] private float tutorialDelay = 1f;
    [SerializeField] private string tutorialPopupTitle = "ŚLADY";
    [SerializeField, TextArea] private string tutorialText =
        "Sherlock potrafi rozpoznawać ślady. Przytrzymaj Lewy Shift, aby wejść w tryb skupienia.";
    [SerializeField] private VideoClip tutorialPopupVideoClip;
    [SerializeField] private EagleVisionScanner scanner;
    [SerializeField] private GameObject[] footprintSplines;

    [Header("Tutorial Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private string tutorialCameraPresetName = "Wide";
    [SerializeField, Min(0.1f)] private float cameraTransitionSpeed = 2.5f;
    [SerializeField, Min(0f)] private float cameraSettleDelay = 0.75f;
    [SerializeField] private bool returnToPreviousCameraAfterTutorial = true;

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_SecretDoor);

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        SetFootprintSplinesActive(false);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        player.currentInteractable = null;

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;

        StartCoroutine(RunFootstepsTutorial());
    }

    private IEnumerator RunFootstepsTutorial()
    {
        if (tutorialDelay > 0f)
            yield return new WaitForSecondsRealtime(tutorialDelay);

        bool cameraChanged = cameraController != null &&
                             cameraController.SetZoomPreset(tutorialCameraPresetName, cameraTransitionSpeed);

        if (cameraChanged && cameraSettleDelay > 0f)
            yield return new WaitForSecondsRealtime(cameraSettleDelay);

        TutorialTimeline tutorialTimeline = TutorialTimeline.Instance;
        if (tutorialTimeline != null)
        {
            tutorialTimeline.ShowGameplayTutorialPopup(
                tutorialPopupTitle,
                tutorialText,
                tutorialPopupVideoClip);

            while (tutorialTimeline.BlocksWorldInput)
                yield return null;
        }

        if (scanner != null)
            scanner.footPrints = true;

        SetFootprintSplinesActive(true);

        if (cameraChanged && returnToPreviousCameraAfterTutorial)
            cameraController.ReturnToPreviousZoomState(cameraTransitionSpeed);
    }

    private void SetFootprintSplinesActive(bool active)
    {
        foreach (GameObject footprintSpline in footprintSplines)
        {
            if (footprintSpline != null)
                footprintSpline.SetActive(active);
        }
    }
}
