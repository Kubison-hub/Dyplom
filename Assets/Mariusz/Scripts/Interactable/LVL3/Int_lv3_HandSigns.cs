using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_HandSigns : Lvl3InteractionDialogueBase
{
    [Header("Light Requirement")]
    [Tooltip("At least one assigned lamp must be active for this clue to be available.")]
    [SerializeField] private GameObject[] heldLamps;
    [Tooltip("Spline visuals shown only while at least one lamp is active. Leave empty to use child objects whose name contains 'Spline'.")]
    [FormerlySerializedAs("spline")]
    [SerializeField] private GameObject[] splines;

    [Header("Loupe Discovery")]
    [SerializeField] private Collider loupeCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.1f)] private float fallbackLoupeHoldDuration = 1.5f;

    [Header("Result")]
    [SerializeField] private DetectiveIdeaPoint ideaPoint;

    private Interactable interactable;
    private float loupeHoldStartedAt = -1f;
    private bool discovered;
    private bool lightAvailable;
    private bool lightAvailabilityInitialized;
    private readonly List<GameObject> splineObjects = new List<GameObject>();

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Widzisz te ślady, Watsonie? Zdradzają one...",
            duration = 3f
        }
    };

    private void Reset() => SetupInteractable(InteractionType.Int_lv3_HandSigns);
    private void OnValidate() => SetupInteractable(InteractionType.Int_lv3_HandSigns);
    private void Awake()
    {
        SetupInteractable(InteractionType.Int_lv3_HandSigns);
        CacheSplineObjects();
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (loupeCollider == null)
            loupeCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (ideaPoint != null)
            ideaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;

        UpdateLightAvailability();
    }

    private void Update()
    {
        UpdateLightAvailability();

        if (discovered || !lightAvailable || magnifier == null || loupeCollider == null)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        bool isLoupeOverHandSigns = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                                    IsLoupeHitOnHandSigns(hit.collider);

        if (!isLoupeOverHandSigns)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        if (loupeHoldStartedAt < 0f)
            loupeHoldStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoldStartedAt >= GetLoupeHoldDuration())
            ResolveLoupeDiscovery();
    }

    public void PerformInteraction(PlayerController player)
    {
        // This clue is intentionally discovered only by holding the loupe over the hand signs.
        if (player != null)
            player.currentInteractable = null;
    }

    private float GetLoupeHoldDuration()
    {
        return DetectiveIdeaManager.Instance != null
            ? DetectiveIdeaManager.Instance.magnifierDiscoveryDuration
            : fallbackLoupeHoldDuration;
    }

    private void UpdateLightAvailability()
    {
        bool hasActiveLamp = HasActiveLamp();
        SetSplinesActive(hasActiveLamp);

        bool shouldBeAvailable = !discovered && hasActiveLamp;
        if (lightAvailabilityInitialized && lightAvailable == shouldBeAvailable)
            return;

        lightAvailabilityInitialized = true;
        lightAvailable = shouldBeAvailable;
        loupeHoldStartedAt = -1f;

        if (interactable != null)
        {
            interactable.isInteractableActive = shouldBeAvailable;

            if (!shouldBeAvailable)
            {
                interactable.SetQuestionFXEagleVisionState(false);

                if (interactable.interactiveShader != null)
                    interactable.interactiveShader.SetActive(false);
            }
        }

        if (loupeCollider != null)
            loupeCollider.enabled = shouldBeAvailable;
    }

    private bool HasActiveLamp()
    {
        if (heldLamps == null)
            return false;

        foreach (GameObject lamp in heldLamps)
        {
            if (lamp != null && lamp.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void CacheSplineObjects()
    {
        splineObjects.Clear();

        if (splines != null)
        {
            foreach (GameObject spline in splines)
                AddSplineObject(spline);
        }

        // The current Level 3 setup keeps the visual spline as a child of this interaction.
        // This fallback preserves that workflow when the inspector array has not been filled in.
        if (splineObjects.Count > 0)
            return;

        foreach (Transform child in transform)
        {
            if (child.name.IndexOf("Spline", System.StringComparison.OrdinalIgnoreCase) >= 0)
                AddSplineObject(child.gameObject);
        }
    }

    private void AddSplineObject(GameObject spline)
    {
        if (spline != null && !splineObjects.Contains(spline))
            splineObjects.Add(spline);
    }

    private void SetSplinesActive(bool shouldBeActive)
    {
        if (splineObjects.Count == 0)
            CacheSplineObjects();

        foreach (GameObject spline in splineObjects)
        {
            if (spline != null && spline.activeSelf != shouldBeActive)
                spline.SetActive(shouldBeActive);
        }
    }

    private bool IsLoupeHitOnHandSigns(Collider hitCollider)
    {
        return hitCollider == loupeCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeCollider.transform);
    }

    private void ResolveLoupeDiscovery()
    {
        if (discovered)
            return;

        discovered = true;
        loupeHoldStartedAt = -1f;

        ideaPoint?.RevealFromExternalSource();
        PlayInteractionDialogue(null);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);
        }

        loupeCollider.enabled = false;
    }
}
