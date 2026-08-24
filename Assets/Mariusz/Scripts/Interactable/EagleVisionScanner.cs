using System.Collections.Generic;
using UnityEngine;


public class EagleVisionScanner : MonoBehaviour
{
    public static EagleVisionScanner Instance;
    [SerializeField] private Int_EdithExamBody edith;


    public Transform playerTransform;

    public Material[] footprintMaterials;
    public Material footprintMaterial;
    public float radius = 5.0f;
    public float bacgroundTreshold = 10f;

    [Header("QuestionFX Scan Wave")]
    [SerializeField] private string outlinedObjectsLayerName = "Outlined Objects";
    [SerializeField, Min(0.1f)] private float questionFxScanRange = 4f;
    [SerializeField, Min(0.01f)] private float questionFxScanExpansionSpeed = 1.5f;
    [SerializeField] private bool drawQuestionFxScanGizmo = true;

    private ParticleSystem scannerPS;
    private SphereCollider scannerCollider;
    private readonly Collider[] questionFxScanHits = new Collider[128];
    private readonly HashSet<Interactable> scannedInteractables = new HashSet<Interactable>();
    private int outlinedObjectsLayer;
    private float currentQuestionFxScanRadius;

    public float duration = 5;
    public float size = 30;
    public float scannerSpeed = 15;

    public bool isScanning = false;
    private float timer = 0f;

    public bool footPrints;

    public GameObject toolTipPanel;
    public GameObject magnififierGlassCanvas;
    public GameObject toolTipCanvas;

    private bool toolTipSuppressedByDialogue;
    private bool toolTipWasActiveBeforeDialogue;

    public List<GameObject> footprintsSplines = new List<GameObject>();



    private void Start()
    {
        Instance = this;

        scannerPS = GetComponentInChildren<ParticleSystem>();
        scannerCollider = GetComponent<SphereCollider>();

        if (scannerCollider == null)
            scannerCollider = gameObject.AddComponent<SphereCollider>();

        // This trigger drives the scan wave only; it must not intercept mouse raycasts.
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
            scannerCollider.gameObject.layer = ignoreRaycastLayer;

        scannerCollider.isTrigger = true;
        scannerCollider.radius = 0f;
        scannerCollider.enabled = false;

        outlinedObjectsLayer = LayerMask.NameToLayer(outlinedObjectsLayerName);
        if (outlinedObjectsLayer < 0)
            Debug.LogWarning("EagleVisionScanner: Layer '" + outlinedObjectsLayerName + "' was not found.", this);
    }

    private void Update()
    {
        UpdateTooltipDialogueVisibility();
        FindshaderFootPrints();

        if (isScanning)
            ExpandQuestionFxScanWave();
    }


    public void ScanSherlock(bool isActive)
    {
        isScanning = isActive;

        if (scannerPS == null || scannerCollider == null)
        {
            if (!isActive)
                SetAllQuestionFXState(false);

            return;
        }

        // Detection is performed by Physics.OverlapSphereNonAlloc below. The collider itself
        // stays disabled so it can never block the click raycast to IdeaPoints.
        scannerCollider.enabled = false;

        if (isActive)
        {
            ResetQuestionFxScanWave();

            var main = scannerPS.main;
            main.startLifetime = duration;
            main.startSize = size;

            scannerPS.Play();

            if (footPrints)
            {
                foreach (var footprint in footprintsSplines)
                    footprint.SetActive(true);
            }
        }
        else
        {
            scannerPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            scannerCollider.radius = 0f;
            SetAllQuestionFXState(false);

            foreach (var footprint in footprintsSplines)
                footprint.SetActive(false);
        }

        Debug.Log($"Scanner State: {isActive}");
    }

    private void UpdateTooltipDialogueVisibility()
    {
        if (toolTipPanel == null)
            return;

        bool dialogueActive = DialogueEditor.ConversationManager.Instance != null &&
                              (DialogueEditor.ConversationManager.Instance.inConversation ||
                               DialogueEditor.ConversationManager.Instance.IsConversationActive);

        if (dialogueActive)
        {
            if (!toolTipSuppressedByDialogue)
            {
                toolTipWasActiveBeforeDialogue = toolTipPanel.activeSelf;
                toolTipPanel.SetActive(false);
                toolTipSuppressedByDialogue = true;
            }

            return;
        }

        if (toolTipSuppressedByDialogue)
        {
            toolTipPanel.SetActive(toolTipWasActiveBeforeDialogue);
            toolTipSuppressedByDialogue = false;
        }
    }

    public void SetTooltipParentForLoupe(bool loupeActive)
    {
        if (toolTipPanel == null)
            return;

        GameObject targetCanvas = loupeActive ? magnififierGlassCanvas : toolTipCanvas;
        if (targetCanvas == null || toolTipPanel.transform.parent == targetCanvas.transform)
            return;

        toolTipPanel.transform.SetParent(targetCanvas.transform);
    }

    private void ResetQuestionFxScanWave()
    {
        currentQuestionFxScanRadius = 0f;
        scannedInteractables.Clear();
        scannerCollider.radius = 0f;
    }

    private void ExpandQuestionFxScanWave()
    {
        currentQuestionFxScanRadius = Mathf.MoveTowards(
            currentQuestionFxScanRadius,
            questionFxScanRange,
            questionFxScanExpansionSpeed * Time.unscaledDeltaTime);

        scannerCollider.radius = currentQuestionFxScanRadius;

        if (outlinedObjectsLayer < 0)
            return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            GetQuestionFxScanOrigin(),
            currentQuestionFxScanRadius,
            questionFxScanHits,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int index = 0; index < hitCount; index++)
        {
            Collider hit = questionFxScanHits[index];
            if (hit == null)
                continue;

            Interactable interactable = hit.GetComponentInParent<Interactable>();
            if (interactable == null)
                continue;

            if (!UsesOutlinedObjectsLayer(hit, interactable))
                continue;

            if (!scannedInteractables.Add(interactable))
                continue;

            interactable.SetQuestionFXEagleVisionState(true);
        }
    }

    private bool UsesOutlinedObjectsLayer(Collider hit, Interactable interactable)
    {
        return hit.gameObject.layer == outlinedObjectsLayer ||
               interactable.gameObject.layer == outlinedObjectsLayer;
    }

    private Vector3 GetQuestionFxScanOrigin()
    {
        if (scannerCollider != null)
            return scannerCollider.transform.TransformPoint(scannerCollider.center);

        return playerTransform != null ? playerTransform.position : transform.position;
    }

    private void SetAllQuestionFXState(bool eagleVisionActive)
    {
        Interactable[] interactables = FindObjectsByType<Interactable>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (Interactable interactable in interactables)
            interactable.SetQuestionFXEagleVisionState(eagleVisionActive);

        if (!eagleVisionActive)
            scannedInteractables.Clear();
    }

    private void FindshaderFootPrints()

    {

        if (playerTransform != null && footprintMaterials != null && footprintMaterials.Length > 0)
        {
            Vector3 pos = playerTransform.position;

            Material background = footprintMaterials[0];
            if (background != null)
            {
                background.SetVector("_PlayerPosition", pos);
                background.SetFloat("_VisibleRadius", radius + bacgroundTreshold);
            }

            for (int i = 1; i < footprintMaterials.Length; i++)
            {
                if (footprintMaterials[i] != null)
                {
                    footprintMaterials[i].SetVector("_PlayerPosition", pos);
                    footprintMaterials[i].SetFloat("_VisibleRadius", radius);
                }
            }
        }

    }

    private void OnDrawGizmosSelected()
    {
        if (!drawQuestionFxScanGizmo)
            return;

        SphereCollider gizmoCollider = scannerCollider != null
            ? scannerCollider
            : GetComponent<SphereCollider>();

        Vector3 center = gizmoCollider != null
            ? gizmoCollider.transform.TransformPoint(gizmoCollider.center)
            : playerTransform != null ? playerTransform.position : transform.position;

        Gizmos.color = new Color(1f, 0.8f, 0.15f, 0.75f);
        Gizmos.DrawWireSphere(center, questionFxScanRange);

        if (!Application.isPlaying || !isScanning)
            return;

        Gizmos.color = new Color(0.25f, 1f, 0.45f, 0.9f);
        Gizmos.DrawWireSphere(center, currentQuestionFxScanRadius);
        Gizmos.DrawSphere(center, 0.08f);
    }

}
