using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Konieczne dla obs?ugi URP i Kamery
using UnityEngine.Video;

public class EagleVisionSystem : MonoBehaviour
{
    [Header("Post-Processing (Ciemny ?wiat)")]
    public Volume eagleVisionVolume; // Tw?j drugi Volume z priority 1
    public float transitionSpeed = 3f;
    [Tooltip("Wolniejszy, lagodny powrot do normalnego widzenia.")]
    public float exitTransitionSpeed = 0.35f;

    [Header("Ustawienia Renderera (Pod?wietlanie)")]
    // Index 0 to zazwyczaj domy?lny renderer, Index 1 to ten z Eagle Vision
    // Wyja?nienie konfiguracji poni?ej kodu
    public int normalRendererIndex = 0;
    public int eagleRendererIndex = 1;

    public bool isActive = false;
    public KeyCode visionHoldKey = KeyCode.LeftShift;
    public KeyCode magnifierHoldKey = KeyCode.F;

    [Header("First Vision Eye Tutorial")]
    [SerializeField] private bool showFirstVisionEyeTutorial = true;
    [SerializeField, Min(0f)] private float firstVisionEyeTutorialDelay = 1.5f;
    [SerializeField, Min(0f)] private float firstVisionEyeReleaseDelay = 0.2f;
    [SerializeField] private string firstVisionEyeTutorialTitle = "WIZJA DETEKTYWA";
    [SerializeField, TextArea] private string firstVisionEyeTutorialText;
    [SerializeField] private VideoClip firstVisionEyeTutorialVideoClip;

    private UniversalAdditionalCameraData cameraData; // Komponent kamery URP

    public EagleVisionScanner eagleVisionScanner;
    public WatsonEagleVisionScanner watsonEagleVisionScanner;

    private float forcedActiveUntil;
    private bool firstVisionEyeTutorialShown;
    private bool firstVisionEyeTutorialForcesActive;
    private Coroutine firstVisionEyeTutorialCoroutine;

    public static EagleVisionSystem Instance;
    void Start()
    {

        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Pobieramy komponent URP z G??wnej Kamery, ?eby m?c zmienia? renderery
        if (Camera.main != null)
        {
            cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        }
        else
        {
            Debug.LogError("Nie znaleziono MainCamera!");
        }



    }

    void Update()
    {
        bool tutorialBlocksVisionInput = TutorialTimeline.Instance != null &&
                                         TutorialTimeline.Instance.BlocksWorldInput;
        bool dialogueBlocksVisionInput = DialogueEditor.ConversationManager.Instance != null &&
                                         DialogueEditor.ConversationManager.Instance.IsConversationActive;
        bool visionInputBlocked = tutorialBlocksVisionInput || dialogueBlocksVisionInput;

        if (dialogueBlocksVisionInput)
            forcedActiveUntil = 0f;

        bool puzzleForcesVision =
            !dialogueBlocksVisionInput &&
            ((TutorialTimeline.Instance != null && TutorialTimeline.Instance.KeepsEagleVisionActive) ||
             (BasementIdeaPointPuzzle.Instance != null && BasementIdeaPointPuzzle.Instance.KeepsEagleVisionActive));
        bool magnifierActivatesVision = !visionInputBlocked &&
                                        Input.GetKey(magnifierHoldKey) &&
                                        !IsWatsonActive();
        bool watsonEscortActivatesVision = !visionInputBlocked &&
                                            IsWatsonActive() &&
                                            Input.GetKey(magnifierHoldKey) &&
                                            WatsonEscortController.Instance != null &&
                                            WatsonEscortController.Instance.IsEscorting;
        bool manuallyActivatedVision = !visionInputBlocked &&
                                       !IsWatsonActive() &&
                                       Input.GetKeyDown(visionHoldKey);
        bool shouldBeActive = (!visionInputBlocked && Input.GetKey(visionHoldKey)) ||
                               magnifierActivatesVision ||
                               watsonEscortActivatesVision ||
                               puzzleForcesVision ||
                               (!dialogueBlocksVisionInput && IsIdeaSequenceActive()) ||
                               firstVisionEyeTutorialForcesActive ||
                               (!dialogueBlocksVisionInput && Time.unscaledTime < forcedActiveUntil);
        if (isActive != shouldBeActive)
        {
            isActive = shouldBeActive;
            //SwitchRenderer();
            Scan();
        }

        if (manuallyActivatedVision && !firstVisionEyeTutorialShown && showFirstVisionEyeTutorial)
        {
            firstVisionEyeTutorialShown = true;
            firstVisionEyeTutorialCoroutine = StartCoroutine(ShowFirstVisionEyeTutorialAfterDelay());
        }

        SyncSherlockScannerState();

        // 3. P?ynne przej?cie Volume (zostaje bez zmian, bo reaguje na isActive)
        float targetWeight = isActive ? 1f : 0f;
        if (eagleVisionVolume != null)
        {
            float speed = isActive ? transitionSpeed : exitTransitionSpeed;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.unscaledDeltaTime);
            eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, blend);
        }
    }
    private bool IsWatsonActive()
    {
        return SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1;
    }

    public bool IsMagnifierHeld()
    {
        return isActive && Input.GetKey(magnifierHoldKey);
    }

    public void RefreshScan()
    {
        Scan();
    }

    public void HoldVisionFor(float duration)
    {
        forcedActiveUntil = Mathf.Max(forcedActiveUntil, Time.unscaledTime + Mathf.Max(0f, duration));
    }

    private IEnumerator ShowFirstVisionEyeTutorialAfterDelay()
    {
        if (firstVisionEyeTutorialDelay > 0f)
            yield return new WaitForSecondsRealtime(firstVisionEyeTutorialDelay);

        TutorialTimeline tutorialTimeline = TutorialTimeline.Instance;
        if (tutorialTimeline == null || !tutorialTimeline.ShowGameplayTutorialPopup(
            firstVisionEyeTutorialTitle,
            firstVisionEyeTutorialText,
            firstVisionEyeTutorialVideoClip))
        {
            firstVisionEyeTutorialShown = false;
            firstVisionEyeTutorialCoroutine = null;
            yield break;
        }

        firstVisionEyeTutorialForcesActive = true;
        if (!isActive)
        {
            isActive = true;
            Scan();
        }

        while (tutorialTimeline != null && tutorialTimeline.BlocksWorldInput)
            yield return null;

        if (firstVisionEyeReleaseDelay > 0f)
            yield return new WaitForSecondsRealtime(firstVisionEyeReleaseDelay);

        // The normal Shift check in Update keeps the vision active when the player still holds it.
        firstVisionEyeTutorialForcesActive = false;

        firstVisionEyeTutorialCoroutine = null;
    }

    private bool IsIdeaSequenceActive()
    {
        return DetectiveIdeaManager.Instance != null && DetectiveIdeaManager.Instance.IsDraggingIdea();
    }

    // Keeps Sherlock's scan wave aligned with Eagle Vision even if a character switch
    // or component start order left the scanner in an outdated state.
    private void SyncSherlockScannerState()
    {
        if (eagleVisionScanner == null || SwitchCharacter.Instance == null)
            return;

        bool shouldScanSherlock = isActive && SwitchCharacter.Instance.activePlayerIndex == 0;
        if (eagleVisionScanner.isScanning != shouldScanSherlock)
            eagleVisionScanner.ScanSherlock(shouldScanSherlock);
    }


    //void Update()
    //{
    //    // W??czanie / Wy??czanie pod klawiszem E
    //    if (Input.GetKeyDown(KeyCode.V))
    //    {

    //        isActive = !isActive;
    //        SwitchRenderer(); // Zmieniamy spos?b renderowania (widzenie przez ?ciany)

    //        Scan();

    //    }

    //    // P?ynne przej?cie kolor?w (Volume)
    //    //Je?li isActive = true, waga d??y do 1.Je?li false, do 0.

    //   float targetWeight = isActive ? 1f : 0f;
    //    if (eagleVisionVolume != null)
    //    {
    //        eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, Time.deltaTime * transitionSpeed);
    //    }
    //}

    void Scan()
    {
        if (SwitchCharacter.Instance.activePlayerIndex == 0)
        {
            if (watsonEagleVisionScanner != null)
            {
                watsonEagleVisionScanner.ScanWatson(false);
            }
            eagleVisionScanner.ScanSherlock(isActive);
        }
        else
        {
            eagleVisionScanner.ScanSherlock(false);
            if (watsonEagleVisionScanner != null)
            {
                watsonEagleVisionScanner.ScanWatson(isActive);
            }

        }

    }

    void SwitchRenderer()
    {
        if (cameraData == null) return;

        // Je?li tryb aktywny -> ustaw renderer nr 1 (Eagle). Je?li nie -> nr 0 (Normal).
        int indexToSet = isActive ? eagleRendererIndex : normalRendererIndex;
        Debug.Log($"EagleVision isActive = {isActive}");
        cameraData.SetRenderer(indexToSet);
    }

}
