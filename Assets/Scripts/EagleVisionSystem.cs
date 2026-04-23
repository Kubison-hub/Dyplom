using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Konieczne dla obs³ugi URP i Kamery

public class EagleVisionSystem : MonoBehaviour
{
    [Header("Post-Processing (Ciemny Œwiat)")]
    public Volume eagleVisionVolume; // Twój drugi Volume z priority 1
    public float transitionSpeed = 3f;

    [Header("Ustawienia Renderera (Podœwietlanie)")]
    // Index 0 to zazwyczaj domyœlny renderer, Index 1 to ten z Eagle Vision
    // Wyjaœnienie konfiguracji poni¿ej kodu
    public int normalRendererIndex = 0;
    public int eagleRendererIndex = 1;

    public bool isActive = false;
    private UniversalAdditionalCameraData cameraData; // Komponent kamery URP

    public EagleVisionScanner eagleVisionScanner;
    public WatsonEagleVisionScanner watsonEagleVisionScanner;

    public static EagleVisionSystem Instance;
    void Start()
    {

        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Pobieramy komponent URP z G³ównej Kamery, ¿eby móc zmieniaæ renderery
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
       
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isActive = true;
            SwitchRenderer(); 
            Scan();          

        }

       
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isActive = false;
            SwitchRenderer();
            Scan();
        }

        // 3. P³ynne przejœcie Volume (zostaje bez zmian, bo reaguje na isActive)
        float targetWeight = isActive ? 1f : 0f;
        if (eagleVisionVolume != null)
        {
            eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, Time.deltaTime * transitionSpeed);
        }
    }


    //void Update()
    //{
    //    // W³¹czanie / Wy³¹czanie pod klawiszem E
    //    if (Input.GetKeyDown(KeyCode.V))
    //    {

    //        isActive = !isActive;
    //        SwitchRenderer(); // Zmieniamy sposób renderowania (widzenie przez œciany)

    //        Scan();

    //    }

    //    // P³ynne przejœcie kolorów (Volume)
    //    //Jeœli isActive = true, waga d¹¿y do 1.Jeœli false, do 0.

    //   float targetWeight = isActive ? 1f : 0f;
    //    if (eagleVisionVolume != null)
    //    {
    //        eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, Time.deltaTime * transitionSpeed);
    //    }
    //}

    void Scan()
    {
        if (SwitchCharacter.Instance.activePlayerIndex  == 0)
        {
            watsonEagleVisionScanner.ScanWatson(false);
            eagleVisionScanner.ScanSherlock(isActive);
        }
        else 
        {
            eagleVisionScanner.ScanSherlock(false);
            watsonEagleVisionScanner.ScanWatson(isActive);
        }
        
    }

    void SwitchRenderer()
    {
        if (cameraData == null) return;

        // Jeœli tryb aktywny -> ustaw renderer nr 1 (Eagle). Jeœli nie -> nr 0 (Normal).
        int indexToSet = isActive ? eagleRendererIndex : normalRendererIndex;
        Debug.Log($"EagleVision isActive = {isActive}");
        cameraData.SetRenderer(indexToSet);
    }

}