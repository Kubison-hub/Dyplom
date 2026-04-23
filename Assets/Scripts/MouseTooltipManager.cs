using DialogueEditor;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public class MouseTooltipManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Przeci¹gnij tutaj ca³y panel t³a z Canvasa")]
    public GameObject tooltipPanel;

    public GameObject interactionShader;
    public VisualEffect interactionFx;
    [Tooltip("Przeci¹gnij tutaj obiekt Text (TMP) znajduj¹cy siê w panelu")]
    public TextMeshProUGUI tooltipText;

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, -15f); // Przesuniêcie wzglêdem kursora
    public LayerMask detectionLayer = ~0; // Jakie warstwy ma wykrywaæ

    private Camera mainCam;
    public Interactable interactable;
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
            Interactable desc = hit.collider.GetComponent<Interactable>();
            interactable = desc;

            if (interactable != null)
            {
                if (ConversationManager.Instance.inConversation || TutorialManager.Instance.isTutorialActive) return;

                if (interactable.isInteractableActive == true )

                {
                    

                    if (interactable.isFootPrintInteraction)
                    {
                       

                        if (interactable.isNearPlayer)
                        {

                            
                            ShowTooltip(interactable.objectDescription);
                            interactionFx = interactable.interactionVFX;
                            interactionShader = interactable.interactiveShader;
                            ToggleIntShader(true);
                            
                        }
                    }
                    else
                    {
                        
                        ShowTooltip(interactable.objectDescription);
                        interactionFx = interactable.interactionVFX;
                        interactionShader = interactable.interactiveShader;
                        ToggleIntShader(true);
                        
                    }
                }
            }
            else
            {
                
                // Trafiliœmy w coœ bez opisu (np. pod³ogê)
                HideTooltip();
                ToggleIntShader(false);
            }
        }
        else
        {
            
            // Myszka patrzy w pustkê
            HideTooltip();
            ToggleIntShader(false);
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

    private void ToggleIntShader(bool v)
    {
        if (v)
        {
            
            if (interactionFx != null)
                interactionFx.Play();
            if (interactionShader != null) 
                interactionShader.SetActive(true);

            
        }
        else
        {
            
            if (interactionFx != null)
                interactionFx.Stop();

            if (interactionShader != null)
                interactionShader.SetActive(false);

            
        }

    }
}