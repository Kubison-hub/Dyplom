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
    [Tooltip("Bazowa predkosc rozszerzania skanu i ringu w metrach na sekunde.")]
    [SerializeField, Min(0.01f)] private float scannerSpeed = 15f;
    [Tooltip("Przebieg promienia w czasie: os X to czas 0-1, os Y to procent zasiegu 0-1.")]
    [SerializeField] private AnimationCurve scannerSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField] private bool drawQuestionFxScanGizmo = true;

    [Header("Manual Scan Range Ring")]
    [SerializeField] private LineRenderer scanRangeRing;
    [SerializeField] private Material scanRangeRingMaterial;
    [SerializeField] private Color scanRangeRingColor = new Color(0.25f, 1f, 0.45f, 0.8f);
    [SerializeField, Min(0.001f)] private float scanRangeRingWidth = 0.04f;
    [SerializeField, Range(16, 128)] private int scanRangeRingSegments = 64;
    [SerializeField] private float scanRangeRingHeight = 0.03f;

    private ParticleSystem scannerPS;
    private SphereCollider scannerCollider;
    private readonly Collider[] questionFxScanHits = new Collider[128];
    private readonly HashSet<Interactable> scannedInteractables = new HashSet<Interactable>();
    private int outlinedObjectsLayer;
    private float currentQuestionFxScanRadius;
    private float questionFxScanProgress;
    private bool manualScanRingActive;
    private Material runtimeScanRangeRingMaterial;

    public float duration = 5;
    public float size = 30;

    public bool isScanning = false;
    public float CurrentQuestionFxScanRadius => currentQuestionFxScanRadius;
    public Vector3 QuestionFxScanOrigin => GetQuestionFxScanOrigin();
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

        InitializeScanRangeRing();
    }

    private void Update()
    {
        UpdateTooltipDialogueVisibility();
        FindshaderFootPrints();

        if (isScanning)
            ExpandQuestionFxScanWave();

        UpdateScanRangeRing();
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

            SetManualScanRingActive(false);
        }

        Debug.Log($"Scanner State: {isActive}");
    }

    public void SetManualScanRingActive(bool isActive)
    {
        manualScanRingActive = isActive && isScanning;

        if (scanRangeRing != null && !manualScanRingActive)
        {
            scanRangeRing.enabled = false;
            scanRangeRing.positionCount = 0;
        }
    }

    private void UpdateTooltipDialogueVisibility()
    {
        if (toolTipPanel == null)
            return;

        bool dialogueActive = DialogueEditor.ConversationManager.Instance != null &&
                              (DialogueEditor.ConversationManager.Instance.inConversation ||
                               DialogueEditor.ConversationManager.Instance.IsConversationActive);
        bool notebookOpen = NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen;
        bool tutorialBlocksInput = TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput ||
                                   TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput;

        if (dialogueActive || notebookOpen || tutorialBlocksInput)
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
        questionFxScanProgress = 0f;
        scannedInteractables.Clear();
        scannerCollider.radius = 0f;
    }

    private void ExpandQuestionFxScanWave()
    {
        float progressSpeed = questionFxScanRange > 0f
            ? scannerSpeed / questionFxScanRange
            : 1f;
        questionFxScanProgress = Mathf.MoveTowards(
            questionFxScanProgress,
            1f,
            progressSpeed * Time.unscaledDeltaTime);

        float easedProgress = scannerSpeedCurve != null && scannerSpeedCurve.length > 0
            ? Mathf.Clamp01(scannerSpeedCurve.Evaluate(questionFxScanProgress))
            : questionFxScanProgress;
        currentQuestionFxScanRadius = questionFxScanRange * easedProgress;

        if (questionFxScanProgress >= 1f)
            currentQuestionFxScanRadius = questionFxScanRange;

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

    private void InitializeScanRangeRing()
    {
        if (scanRangeRing == null)
        {
            GameObject ringObject = new GameObject("Sherlock Scan Range Ring");
            ringObject.transform.SetParent(transform, false);
            scanRangeRing = ringObject.AddComponent<LineRenderer>();
        }

        int sherlockLayer = LayerMask.NameToLayer("Sherlock");
        if (sherlockLayer >= 0)
            scanRangeRing.gameObject.layer = sherlockLayer;
        else
            Debug.LogWarning("EagleVisionScanner: Layer 'Sherlock' was not found for the scan range ring.", this);

        scanRangeRing.useWorldSpace = true;
        scanRangeRing.loop = true;
        scanRangeRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        scanRangeRing.receiveShadows = false;
        scanRangeRing.textureMode = LineTextureMode.Stretch;
        scanRangeRing.alignment = LineAlignment.View;

        if (scanRangeRingMaterial != null)
        {
            scanRangeRing.material = scanRangeRingMaterial;
        }
        else if (scanRangeRing.sharedMaterial == null)
        {
            Material ringMaterialResource = Resources.Load<Material>("EagleVisionScanRing");
            if (ringMaterialResource != null)
            {
                scanRangeRing.material = ringMaterialResource;
            }
            else
            {
                Shader ringShader = Shader.Find("Mariusz/Eagle Vision Scan Ring");
                if (ringShader == null)
                    ringShader = Shader.Find("Sprites/Default");
                if (ringShader != null)
                {
                    runtimeScanRangeRingMaterial = new Material(ringShader)
                    {
                        name = "Sherlock Scan Range Ring (Runtime)"
                    };
                    scanRangeRing.material = runtimeScanRangeRingMaterial;
                }
            }
        }

        scanRangeRing.enabled = false;
        scanRangeRing.positionCount = 0;
    }

    private void UpdateScanRangeRing()
    {
        if (scanRangeRing == null || !manualScanRingActive || !isScanning)
            return;

        int segmentCount = Mathf.Max(16, scanRangeRingSegments);
        float ringRadius = Mathf.Max(0f, currentQuestionFxScanRadius);
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;
        center.y += scanRangeRingHeight;

        scanRangeRing.enabled = true;
        scanRangeRing.loop = true;
        scanRangeRing.positionCount = segmentCount;
        scanRangeRing.startWidth = scanRangeRingWidth;
        scanRangeRing.endWidth = scanRangeRingWidth;
        scanRangeRing.startColor = scanRangeRingColor;
        scanRangeRing.endColor = scanRangeRingColor;

        for (int index = 0; index < segmentCount; index++)
        {
            float angle = index * Mathf.PI * 2f / segmentCount;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
            scanRangeRing.SetPosition(index, point);
        }
    }

    private void OnDisable()
    {
        manualScanRingActive = false;

        if (scanRangeRing != null)
        {
            scanRangeRing.enabled = false;
            scanRangeRing.positionCount = 0;
        }
    }

    private void OnDestroy()
    {
        if (runtimeScanRangeRingMaterial != null)
            Destroy(runtimeScanRangeRingMaterial);
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
