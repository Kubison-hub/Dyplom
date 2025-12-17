using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Konieczne dla obs³ugi URP i Kamery

public class EagleVisionSystem : MonoBehaviour
{
    [Header("Post-Processing (Ciemny Œwiat)")]
    public Volume eagleVisionVolume; // Twój drugi Volume z priority 1
    public float transitionSpeed = 5f;

    [Header("Ustawienia Renderera (Podœwietlanie)")]
    // Index 0 to zazwyczaj domyœlny renderer, Index 1 to ten z Eagle Vision
    // Wyjaœnienie konfiguracji poni¿ej kodu
    public int normalRendererIndex = 0;
    public int eagleRendererIndex = 1;

    private bool isActive = false;
    private UniversalAdditionalCameraData cameraData; // Komponent kamery URP

    void Start()
    {
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
        // W³¹czanie / Wy³¹czanie pod klawiszem E
        if (Input.GetKeyDown(KeyCode.J))
        {
            isActive = !isActive;
            SwitchRenderer(); // Zmieniamy sposób renderowania (widzenie przez œciany)
        }

        // P³ynne przejœcie kolorów (Volume)
        // Jeœli isActive = true, waga d¹¿y do 1. Jeœli false, do 0.
        float targetWeight = isActive ? 1f : 0f;
        if (eagleVisionVolume != null)
        {
            eagleVisionVolume.weight = Mathf.Lerp(eagleVisionVolume.weight, targetWeight, Time.deltaTime * transitionSpeed);
        }
    }

    void SwitchRenderer()
    {
        if (cameraData == null) return;

        // Jeœli tryb aktywny -> ustaw renderer nr 1 (Eagle). Jeœli nie -> nr 0 (Normal).
        int indexToSet = isActive ? eagleRendererIndex : normalRendererIndex;

        cameraData.SetRenderer(indexToSet);
    }
}