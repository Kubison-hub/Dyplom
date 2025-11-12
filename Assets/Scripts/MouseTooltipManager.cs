using UnityEngine;
using TMPro; // Wa¿ne dla tekstów

public class MouseTooltipManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Przeci¹gnij tutaj swój obiekt Text (TMP) z Canvasa")]
    public TextMeshProUGUI tooltipText;

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, -15f); // Przesuniêcie tekstu wzglêdem kursora
    public LayerMask detectionLayer = ~0; // Jakie warstwy ma wykrywaæ (domyœlnie wszystko)

    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        // Na starcie ukryj tekst
        tooltipText.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateTooltipPosition();
        CheckObjectUnderMouse();
    }

    private void UpdateTooltipPosition()
    {
        // Tekst pod¹¿a za myszk¹ + przesuniêcie
        // (Dzia³a idealnie dla Canvasa w trybie Overlay)
        tooltipText.transform.position = (Vector2)Input.mousePosition + offset;
    }

    private void CheckObjectUnderMouse()
    {
        // Tworzymy promieñ od kamery przez kursor myszy
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectionLayer))
        {
            // SprawdŸ, czy trafiony obiekt ma nasz¹ "Metkê" (skrypt ObjectDescription)
            ObjectDescription desc = hit.collider.GetComponent<ObjectDescription>();

            if (desc != null)
            {
                // Znalaz³ opis! Poka¿ go.
                ShowTooltip(desc.description);
            }
            else
            {
                // Trafi³ w coœ (np. pod³ogê), co nie ma opisu.
                HideTooltip();
            }
        }
        else
        {
            // Myszka jest w powietrzu (nic nie trafi³a).
            HideTooltip();
        }
    }

    private void ShowTooltip(string text)
    {
        tooltipText.text = text;
        tooltipText.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        tooltipText.gameObject.SetActive(false);
    }
}