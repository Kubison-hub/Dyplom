using UnityEngine;
using TMPro;

public class MouseTooltipManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Przeci¹gnij tutaj ca³y panel t³a z Canvasa")]
    public GameObject tooltipPanel;

    [Tooltip("Przeci¹gnij tutaj obiekt Text (TMP) znajduj¹cy siê w panelu")]
    public TextMeshProUGUI tooltipText;

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, -15f); // Przesuniêcie wzglêdem kursora
    public LayerMask detectionLayer = ~0; // Jakie warstwy ma wykrywaæ

    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;

        // Na starcie ukryj ca³kowicie panel
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 1. Sprawdzamy co jest pod myszk¹
        CheckObjectUnderMouse();

        // 2. Jeœli panel jest w³¹czony, aktualizujemy jego pozycjê
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }

    private void UpdateTooltipPosition()
    {
        // Przesuwamy ca³y panel (t³o razem z tekstem w œrodku)
        tooltipPanel.transform.position = (Vector2)Input.mousePosition + offset;
    }

    private void CheckObjectUnderMouse()
    {
        // Wypuszczamy promieñ z kamery w stronê kursora
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectionLayer))
        {
            // Sprawdzamy czy trafiony obiekt ma nasz¹ "metkê" z opisem
            ObjectDescription desc = hit.collider.GetComponent<ObjectDescription>();

            if (desc != null)
            {
                // Trafiliœmy na obiekt z opisem - poka¿ go
                ShowTooltip(desc.description);
            }
            else
            {
                // Trafiliœmy w coœ bez opisu (np. pod³ogê)
                HideTooltip();
            }
        }
        else
        {
            // Myszka patrzy w pustkê
            HideTooltip();
        }
    }

    private void ShowTooltip(string text)
    {
        if (tooltipText != null) tooltipText.text = text;
        if (tooltipPanel != null) tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}