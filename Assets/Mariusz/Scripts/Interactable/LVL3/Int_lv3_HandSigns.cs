using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_HandSigns : Lvl3InteractionDialogueBase
{
    [Header("Light Requirement")]
    [Tooltip("The spline is visible while at least one assigned lamp is active.")]
    [SerializeField] private GameObject[] heldLamps;
    [Tooltip("Spline objects controlled by the active lamp condition.")]
    [SerializeField] private GameObject[] splines;
    [Tooltip("Additional spline displayed on the floor while at least one assigned lamp is active.")]
    [SerializeField] private GameObject floorSpline;
    [SerializeField, TextArea] private string darknessText = "Nic nie zobaczę w tych ciemnościach.";
    [Tooltip("Played as Sherlock's dialogue line together with Darkness Text.")]
    [SerializeField] private AudioClip darknessVoiceClip;

    [Header("Loupe Hover")]
    [Tooltip("Collider that must remain under the active loupe to dismiss QuestionFX.")]
    [SerializeField] private Collider loupeHoverCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [Tooltip("How long the loupe must remain over the hand signs.")]
    [SerializeField, Min(0f)] private float loupeHoverDuration = 1f;

    private bool? lastLightState;
    private Interactable interactable;
    private float loupeHoverStartedAt = -1f;
    private bool questionFxDismissed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Awake()
    {
        interactable = GetComponent<Interactable>();

        if (loupeHoverCollider == null)
            loupeHoverCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        ApplySplineVisibility();
    }

    private void Update()
    {
        ApplySplineVisibility();
        UpdateLoupeHover();
    }

    private void UpdateLoupeHover()
    {
        if (questionFxDismissed)
        {
            interactable?.SetQuestionFXEagleVisionState(false);
            return;
        }

        if (!HasActiveLamp() || !HasVisibleHandSpline())
        {
            loupeHoverStartedAt = -1f;
            return;
        }

        bool isHovering = magnifier != null &&
                          loupeHoverCollider != null &&
                          magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                          IsLoupeHoverHit(hit.collider);

        if (!isHovering)
        {
            loupeHoverStartedAt = -1f;
            return;
        }

        if (loupeHoverStartedAt < 0f)
            loupeHoverStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoverStartedAt < loupeHoverDuration)
            return;

        questionFxDismissed = true;
        interactable?.SetQuestionFXEagleVisionState(false);
    }

    private bool IsLoupeHoverHit(Collider hitCollider)
    {
        return hitCollider == loupeHoverCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeHoverCollider.transform);
    }

    private bool HasVisibleHandSpline()
    {
        if (splines != null)
        {
            foreach (GameObject spline in splines)
            {
                if (spline == null || !spline.activeInHierarchy)
                    continue;

                Renderer[] renderers = spline.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer splineRenderer in renderers)
                {
                    if (splineRenderer != null && splineRenderer.enabled)
                        return true;
                }
            }
        }

        return false;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (!HasActiveLamp())
        {
            PlaySherlockDialogueLine(player, darknessText, darknessVoiceClip);
            return;
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private void PlaySherlockDialogueLine(PlayerController player, string text, AudioClip voiceClip)
    {
        float defaultDuration = PlayerTopText.Instance != null
            ? PlayerTopText.Instance.textTime
            : 3f;
        float voiceDuration = voiceClip != null ? voiceClip.length : 0f;

        PlayDialogue(player, new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = text,
                voiceClip = voiceClip,
                duration = Mathf.Max(defaultDuration, voiceDuration)
            }
        });
    }

    private void ApplySplineVisibility()
    {
        bool hasActiveLamp = HasActiveLamp();
        if (lastLightState == hasActiveLamp)
            return;

        lastLightState = hasActiveLamp;

        if (splines != null)
        {
            foreach (GameObject spline in splines)
            {
                SetSplineVisible(spline, hasActiveLamp);
            }
        }

        SetSplineVisible(floorSpline, hasActiveLamp);
    }

    private static void SetSplineVisible(GameObject spline, bool visible)
    {
        if (spline == null)
            return;

        if (!spline.activeSelf)
            spline.SetActive(true);

        Renderer[] renderers = spline.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer splineRenderer in renderers)
        {
            if (splineRenderer != null)
                splineRenderer.enabled = visible;
        }
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
}
