using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Konieczne dla obs�ugi URP i Kamery

public class EagleVisionSystem : MonoBehaviour
{
    [Header("Post-Processing (Ciemny �wiat)")]
    public Volume eagleVisionVolume; // Tw�j drugi Volume z priority 1
    public float transitionSpeed = 3f;
    [Tooltip("Wolniejszy, lagodny powrot do normalnego widzenia.")]
    public float exitTransitionSpeed = 0.35f;

    [Header("Ustawienia Renderera (Pod�wietlanie)")]
    // Index 0 to zazwyczaj domy�lny renderer, Index 1 to ten z Eagle Vision
    // Wyja�nienie konfiguracji poni�ej kodu
    public int normalRendererIndex = 0;
    public int eagleRendererIndex = 1;

    public bool isActive = false;
    public KeyCode visionHoldKey = KeyCode.LeftShift;
    public KeyCode magnifierHoldKey = KeyCode.F;
    private UniversalAdditionalCameraData cameraData; // Komponent kamery URP

    public EagleVisionScanner eagleVisionScanner;
    public WatsonEagleVisionScanner watsonEagleVisionScanner;

    private float forcedActiveUntil;

    public static EagleVisionSystem Instance;
    void Start()
    {

        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Pobieramy komponent URP z G��wnej Kamery, �eby m�c zmienia� renderery
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

        bool shouldBeActive = Input.GetKey(visionHoldKey) || Input.GetKey(magnifierHoldKey) ||
                              IsIdeaSequenceActive() ||
                              Time.unscaledTime < forcedActiveUntil;
        if (isActive != shouldBeActive)
        {
            isActive = shouldBeActive;
            //SwitchRenderer();
            Scan();
        }

        // 3. P�ynne przej�cie Volume (zostaje bez zmian, bo reaguje na isActive)
        float targetWeight = isActive ? 1f : 0f;
        if (eagleVisionVolume != null)
        {
            float speed = isActive ? transitionSpeed : exitTransitionSpeed;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.unscaledDeltaTime);
            eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, blend);
        }
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

    private bool IsIdeaSequenceActive()
    {
        return DetectiveIdeaManager.Instance != null && DetectiveIdeaManager.Instance.IsDraggingIdea();
    }


    //void Update()
    //{
    //    // W��czanie / Wy��czanie pod klawiszem E
    //    if (Input.GetKeyDown(KeyCode.V))
    //    {

    //        isActive = !isActive;
    //        SwitchRenderer(); // Zmieniamy spos�b renderowania (widzenie przez �ciany)

    //        Scan();

    //    }

    //    // P�ynne przej�cie kolor�w (Volume)
    //    //Je�li isActive = true, waga d��y do 1.Je�li false, do 0.

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

        // Je�li tryb aktywny -> ustaw renderer nr 1 (Eagle). Je�li nie -> nr 0 (Normal).
        int indexToSet = isActive ? eagleRendererIndex : normalRendererIndex;
        Debug.Log($"EagleVision isActive = {isActive}");
        cameraData.SetRenderer(indexToSet);
    }

}