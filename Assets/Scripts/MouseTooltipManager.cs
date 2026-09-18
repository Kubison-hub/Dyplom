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
    [Tooltip("Layers that block tooltip raycasts. Default: Walls.")]
    public LayerMask blockingLayers = 1 << 12;

    private Camera mainCam;
    private Interactable activeInteractionShaderOwner;
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
        if (IsWorldInputBlocked())
        {
            HideTooltip();
            ToggleIntShader(false);
            return;
        }

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

        // The closest collider decides whether the tooltip is visible.
        int tooltipRaycastMask = detectionLayer.value | blockingLayers.value;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tooltipRaycastMask))
        {
            if (IsBlockedByWall(hit.collider))
            {
                HideTooltip();
                ToggleIntShader(false);
                return;
            }
            // Sprawdzamy czy trafiony obiekt ma nasz¹ "metkê" z opisem
            Interactable desc = hit.collider.GetComponent<Interactable>();
            interactable = desc;

            if (interactable != null)
            {
                PlayerController activePlayer = GetActivePlayerController();
                if (activePlayer == null || !interactable.CanPlayerInteract(activePlayer))
                {
                    HideTooltip();
                    ToggleIntShader(false);
                    return;
                }

                //Debug.Log("Interactable");
                //if (ConversationManager.Instance.inConversation || TutorialManager.Instance.isTutorialActive) return;

                if (ClueManager.Instance.isLockpicking) return;

                if (interactable.isInteractableActive == true)

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
                FoxusDetection();
            }
        }
        else
        {

            // Myszka patrzy w pustkê
            HideTooltip();
            ToggleIntShader(false);
        }
    }

    private static PlayerController GetActivePlayerController()
    {
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null && switchCharacter.players != null &&
            switchCharacter.activePlayerIndex >= 0 &&
            switchCharacter.activePlayerIndex < switchCharacter.players.Length)
        {
            UnityEngine.InputSystem.PlayerInput activeInput =
                switchCharacter.players[switchCharacter.activePlayerIndex];
            PlayerController activePlayer = activeInput != null
                ? activeInput.GetComponent<PlayerController>()
                : null;

            if (activePlayer != null)
                return activePlayer;
        }

        foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player != null && player.enabled)
                return player;
        }

        return null;
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

    private static bool IsWorldInputBlocked()
    {
        return PlayerController.IsWorldInputLocked ||
               DialogueEditor.ConversationManager.Instance != null &&
               DialogueEditor.ConversationManager.Instance.IsConversationActive ||
               NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen ||
               TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput ||
               TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput ||
               DetectiveIdeaManager.Instance != null && DetectiveIdeaManager.Instance.IsDraggingIdea();
    }

    private bool IsBlockedByWall(Collider hitCollider)
    {
        return hitCollider != null &&
               (blockingLayers.value & (1 << hitCollider.gameObject.layer)) != 0;
    }

    private void ToggleIntShader(bool v)
    {
        if (v)
        {
            if (interactionFx != null)
                interactionFx.Play();

            if (interactable != activeInteractionShaderOwner)
            {
                FadeOutActiveShader();
                activeInteractionShaderOwner = interactable;
            }

            if (activeInteractionShaderOwner != null)
                activeInteractionShaderOwner.SetInteractionShaderHover(true);
        }
        else
        {
            if (interactionFx != null)
                interactionFx.Stop();

            FadeOutActiveShader();
        }
    }

    private void FadeOutActiveShader()
    {
        if (activeInteractionShaderOwner == null)
            return;

        activeInteractionShaderOwner.SetInteractionShaderHover(false);
        activeInteractionShaderOwner = null;
    }
    public void FoxusDetection()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hitCol, 100f, detectionLayer))
        {
            Focus_Detector focusDetector = hitCol.collider.GetComponent<Focus_Detector>();

            if (focusDetector != null)
            {
                Debug.Log("FocusDetector");
                focusDetector.ChangeLayer();
            }


        }
    }
}