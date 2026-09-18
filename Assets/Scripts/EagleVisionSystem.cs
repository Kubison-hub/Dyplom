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

    [Header("Audio")]
    [SerializeField] private AudioSource eagleVisionAudioSource;
    [SerializeField] private AudioClip eagleVisionEnterClip;
    [SerializeField] private AudioClip eagleVisionExitClip;

    [Header("Debug")]
    [Tooltip("Włącza lub wyłącza stale aktywne Vision Eye.")]
    [SerializeField] private KeyCode debugVisionToggleKey = KeyCode.H;

    [Header("Color Mask")]
    [Tooltip("Nasycenie obrazu wewnatrz ringu. 0 oznacza naturalny kolor, wartosci ujemne go wyciszaja.")]
    [SerializeField, Range(-100f, 100f)] private float insideSaturation = -35f;
    [SerializeField, Range(-2f, 2f)] private float insideBrightness = 0f;
    [SerializeField, Range(-100f, 100f)] private float insideContrast = 0f;
    [SerializeField, ColorUsage(true, true)] private Color insideTint = Color.white;

    [Tooltip("Nasycenie obrazu poza ringiem. -100 oznacza pelne odbarwienie.")]
    [SerializeField, Range(-100f, 100f)] private float outsideSaturation = -100f;
    [SerializeField, Range(-2f, 2f)] private float outsideBrightness = 0f;
    [SerializeField, Range(-100f, 100f)] private float outsideContrast = 0f;
    [SerializeField, ColorUsage(true, true)] private Color outsideTint = Color.white;

    [SerializeField, Min(0.001f)] private float colorBoundarySoftness = 0.2f;
    [SerializeField, ColorUsage(true, true)] private Color colorBoundaryTint = new Color(0.05f, 0.8f, 0.4f, 0.12f);

    [Header("First Vision Eye Tutorial")]
    [SerializeField] private bool showFirstVisionEyeTutorial = true;
    [SerializeField, Min(0f)] private float firstVisionEyeTutorialDelay = 1.5f;
    [SerializeField, Min(0f)] private float firstVisionEyeReleaseDelay = 0.2f;
    [SerializeField] private string firstVisionEyeTutorialTitle = "WIZJA DETEKTYWA";
    [SerializeField, TextArea] private string firstVisionEyeTutorialText;
    [SerializeField] private VideoClip firstVisionEyeTutorialVideoClip;

    private UniversalAdditionalCameraData cameraData; // Komponent kamery URP

    public EagleVisionScanner eagleVisionScanner;

    private float forcedActiveUntil;
    private bool firstVisionEyeTutorialShown;
    private bool firstVisionEyeTutorialForcesActive;
    private Coroutine firstVisionEyeTutorialCoroutine;
    private bool debugVisionLockedOn;
    private bool manualVisionAudioSessionActive;
    private ColorAdjustments eagleVisionColorAdjustments;
    private float originalEagleVisionSaturation;
    private bool saturationOverrideNeutralized;

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

        NeutralizeVolumeSaturation();



    }

    void Update()
    {
        bool tutorialBlocksVisionInput = TutorialTimeline.Instance != null &&
                                         TutorialTimeline.Instance.BlocksWorldInput;
        bool dialogueBlocksVisionInput = DialogueEditor.ConversationManager.Instance != null &&
                                         DialogueEditor.ConversationManager.Instance.IsConversationActive;
        bool visionInputBlocked = tutorialBlocksVisionInput || dialogueBlocksVisionInput;

        if (dialogueBlocksVisionInput)
        {
            forcedActiveUntil = 0f;
            debugVisionLockedOn = false;
            firstVisionEyeTutorialForcesActive = false;
        }

        bool ideaSequenceForcesVision = !dialogueBlocksVisionInput && IsIdeaSequenceActive();
        bool puzzleForcesVision =
            !dialogueBlocksVisionInput &&
            ((TutorialTimeline.Instance != null && TutorialTimeline.Instance.KeepsEagleVisionActive) ||
             (BasementIdeaPointPuzzle.Instance != null && BasementIdeaPointPuzzle.Instance.KeepsEagleVisionActive));
        bool gameplayForcesVision = puzzleForcesVision || ideaSequenceForcesVision;

        if (gameplayForcesVision)
            debugVisionLockedOn = false;
        else if (Input.GetKeyDown(debugVisionToggleKey))
        {
            debugVisionLockedOn = !debugVisionLockedOn;
            Debug.Log("Debug Vision Eye: " + (debugVisionLockedOn ? "WLACZONE" : "WYLACZONE"), this);
        }

        bool magnifierActivatesVision = !visionInputBlocked &&
                                        Input.GetKey(magnifierHoldKey) &&
                                        !IsWatsonActive();
        bool manuallyActivatedVision = !visionInputBlocked &&
                                       !gameplayForcesVision &&
                                       !IsWatsonActive() &&
                                       Input.GetKeyDown(visionHoldKey);
        bool manualVisionHeld = !visionInputBlocked &&
                                !gameplayForcesVision &&
                                !IsWatsonActive() &&
                                Input.GetKey(visionHoldKey);
        bool manualVisionRequested = !visionInputBlocked &&
                                     !gameplayForcesVision &&
                                     Input.GetKey(visionHoldKey);
        bool shouldBeActive = manualVisionRequested ||
                               magnifierActivatesVision ||
                               gameplayForcesVision ||
                               firstVisionEyeTutorialForcesActive ||
                               debugVisionLockedOn ||
                               (!dialogueBlocksVisionInput && Time.unscaledTime < forcedActiveUntil);
        if (isActive != shouldBeActive)
        {
            isActive = shouldBeActive;
            if (isActive)
            {
                manualVisionAudioSessionActive = manualVisionRequested;
                if (manualVisionAudioSessionActive)
                    PlayEagleVisionTransitionAudio(true);
            }
            else
            {
                if (manualVisionAudioSessionActive)
                    PlayEagleVisionTransitionAudio(false);

                manualVisionAudioSessionActive = false;
            }
            //SwitchRenderer();
            Scan();
        }

        if (manuallyActivatedVision && !firstVisionEyeTutorialShown && showFirstVisionEyeTutorial)
        {
            firstVisionEyeTutorialShown = true;
            firstVisionEyeTutorialCoroutine = StartCoroutine(ShowFirstVisionEyeTutorialAfterDelay());
        }

        SyncSherlockScannerState();

        if (eagleVisionScanner != null)
            eagleVisionScanner.SetManualScanRingActive(manualVisionHeld || debugVisionLockedOn);

        // 3. P?ynne przej?cie Volume (zostaje bez zmian, bo reaguje na isActive)
        float targetWeight = isActive ? 1f : 0f;
        if (eagleVisionVolume != null)
        {
            float speed = isActive ? transitionSpeed : exitTransitionSpeed;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.unscaledDeltaTime);
            eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, blend);
        }

        UpdateColorMaskShaderGlobals(manualVisionHeld || debugVisionLockedOn);
    }

    private void PlayEagleVisionTransitionAudio(bool entering)
    {
        AudioClip clip = entering ? eagleVisionEnterClip : eagleVisionExitClip;
        if (eagleVisionAudioSource == null || clip == null)
            return;

        eagleVisionAudioSource.PlayOneShot(clip);
    }
    private bool IsWatsonActive()
    {
        return SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1;
    }

    public bool IsMagnifierHeld()
    {
        return isActive && !IsWatsonActive() && Input.GetKey(magnifierHoldKey);
    }

    public void RefreshScan()
    {
        Scan();
    }

    public void HoldVisionFor(float duration)
    {
        forcedActiveUntil = Mathf.Max(forcedActiveUntil, Time.unscaledTime + Mathf.Max(0f, duration));
    }

    private void NeutralizeVolumeSaturation()
    {
        if (eagleVisionVolume == null || eagleVisionVolume.profile == null ||
            !eagleVisionVolume.profile.TryGet(out eagleVisionColorAdjustments))
            return;

        originalEagleVisionSaturation = eagleVisionColorAdjustments.saturation.value;
        eagleVisionColorAdjustments.saturation.value = 0f;
        saturationOverrideNeutralized = true;
    }

    private void UpdateColorMaskShaderGlobals(bool colorRingActive)
    {
        float strength = eagleVisionVolume != null ? eagleVisionVolume.weight : isActive ? 1f : 0f;
        Vector3 center = eagleVisionScanner != null
            ? eagleVisionScanner.QuestionFxScanOrigin
            : transform.position;
        float scanRadius = eagleVisionScanner != null
            ? eagleVisionScanner.CurrentQuestionFxScanRadius
            : 0f;

        Shader.SetGlobalVector("_EagleVisionScanCenter", center);
        Shader.SetGlobalFloat("_EagleVisionScanRadius", scanRadius);
        Shader.SetGlobalFloat("_EagleVisionColorMaskEnabled", colorRingActive ? 1f : 0f);
        Shader.SetGlobalFloat("_EagleVisionColorMaskStrength", strength);
        Shader.SetGlobalFloat("_EagleVisionInsideSaturation", insideSaturation);
        Shader.SetGlobalFloat("_EagleVisionInsideBrightness", insideBrightness);
        Shader.SetGlobalFloat("_EagleVisionInsideContrast", insideContrast);
        Shader.SetGlobalColor("_EagleVisionInsideTint", insideTint);
        Shader.SetGlobalFloat("_EagleVisionOutsideSaturation", outsideSaturation);
        Shader.SetGlobalFloat("_EagleVisionOutsideBrightness", outsideBrightness);
        Shader.SetGlobalFloat("_EagleVisionOutsideContrast", outsideContrast);
        Shader.SetGlobalColor("_EagleVisionOutsideTint", outsideTint);
        Shader.SetGlobalFloat("_EagleVisionBoundarySoftness", colorBoundarySoftness);
        Shader.SetGlobalColor("_EagleVisionBoundaryTint", colorBoundaryTint);
    }

    private void OnDestroy()
    {
        Shader.SetGlobalFloat("_EagleVisionColorMaskStrength", 0f);
        Shader.SetGlobalFloat("_EagleVisionColorMaskEnabled", 0f);

        if (saturationOverrideNeutralized && eagleVisionColorAdjustments != null)
            eagleVisionColorAdjustments.saturation.value = originalEagleVisionSaturation;
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
        if (eagleVisionScanner == null || SwitchCharacter.Instance == null)
            return;

        eagleVisionScanner.ScanSherlock(isActive && SwitchCharacter.Instance.activePlayerIndex == 0);

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
