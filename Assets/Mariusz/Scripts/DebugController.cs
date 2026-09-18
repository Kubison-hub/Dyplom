using UnityEngine;

public sealed class DebugController : MonoBehaviour
{
    public static DebugController Instance { get; private set; }

    [Header("Ustawienia ogólne")]
    [Tooltip("Włącza stosowanie ustawień debugowych. Po wyłączeniu przywracane są wartości zapisane w komponentach sceny.")]
    [SerializeField] private bool debugModeEnabled;

    [Header("Sekwencja początkowa")]
    [Tooltip("W trybie debugowania określa, czy TutorialTimeline ma uruchomić początkową rozmowę i sekwencję notatnika.")]
    [SerializeField] private bool runOpeningConversationSequence = true;

    [Header("Schody na piętro")]
    [Tooltip("W trybie debugowania pozwala wejść po schodach niezależnie od normalnych warunków fabularnych.")]
    [SerializeField] private bool debugCanGoUpStairs;

    private TutorialTimeline tutorialTimeline;
    private Int_StairsUp stairsUp;
    private bool originalOpeningConversationSequence;
    private bool originalCanGoUpStairs;
    private bool hasOpeningConversationOriginal;
    private bool hasCanGoUpStairsOriginal;
    private bool openingConversationOverrideApplied;
    private bool stairsOverrideApplied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Register(FindFirstObjectByType<TutorialTimeline>());
        Register(FindFirstObjectByType<Int_StairsUp>());
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        RestoreOriginalValues();
        Instance = null;
    }

    private void OnEnable()
    {
        if (Instance == this)
            ApplyAllOverrides();
    }

    private void OnDisable()
    {
        if (Instance == this)
            RestoreOriginalValues();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyAllOverrides();
    }

    public void Register(TutorialTimeline target)
    {
        if (target == null)
            return;

        if (tutorialTimeline == target && hasOpeningConversationOriginal)
        {
            ApplyOpeningConversationOverride();
            return;
        }

        tutorialTimeline = target;
        originalOpeningConversationSequence = target.RunOpeningConversationSequence;
        hasOpeningConversationOriginal = true;
        openingConversationOverrideApplied = false;
        ApplyOpeningConversationOverride();
    }

    public void Unregister(TutorialTimeline target)
    {
        if (tutorialTimeline != target)
            return;

        tutorialTimeline = null;
        hasOpeningConversationOriginal = false;
        openingConversationOverrideApplied = false;
    }

    public void Register(Int_StairsUp target)
    {
        if (target == null)
            return;

        if (stairsUp == target && hasCanGoUpStairsOriginal)
        {
            ApplyStairsOverride();
            return;
        }

        stairsUp = target;
        originalCanGoUpStairs = target.CanGoUpStairs;
        hasCanGoUpStairsOriginal = true;
        stairsOverrideApplied = false;
        ApplyStairsOverride();
    }

    public void Unregister(Int_StairsUp target)
    {
        if (stairsUp != target)
            return;

        stairsUp = null;
        hasCanGoUpStairsOriginal = false;
        stairsOverrideApplied = false;
    }

    public bool TryGetRunOpeningConversationSequence(out bool value)
    {
        value = runOpeningConversationSequence;
        return isActiveAndEnabled && debugModeEnabled;
    }

    public bool TryGetCanGoUpStairs(out bool value)
    {
        value = debugCanGoUpStairs;
        return isActiveAndEnabled && debugModeEnabled;
    }

    public void SetRunOpeningConversationSequence(bool value)
    {
        runOpeningConversationSequence = value;
        ApplyOpeningConversationOverride();
    }

    public void SetCanGoUpStairs(bool value)
    {
        debugCanGoUpStairs = value;
        ApplyStairsOverride();
    }

    private void ApplyAllOverrides()
    {
        ApplyOpeningConversationOverride();
        ApplyStairsOverride();
    }

    private void ApplyOpeningConversationOverride()
    {
        if (tutorialTimeline == null || !hasOpeningConversationOriginal)
            return;

        if (TryGetRunOpeningConversationSequence(out bool value))
        {
            tutorialTimeline.RunOpeningConversationSequence = value;
            openingConversationOverrideApplied = true;
        }
        else if (openingConversationOverrideApplied)
        {
            tutorialTimeline.RunOpeningConversationSequence = originalOpeningConversationSequence;
            openingConversationOverrideApplied = false;
        }
    }

    private void ApplyStairsOverride()
    {
        if (stairsUp == null || !hasCanGoUpStairsOriginal)
            return;

        if (TryGetCanGoUpStairs(out bool value))
        {
            stairsUp.CanGoUpStairs = value;
            stairsOverrideApplied = true;
        }
        else if (stairsOverrideApplied)
        {
            stairsUp.CanGoUpStairs = originalCanGoUpStairs;
            stairsOverrideApplied = false;
        }
    }

    private void RestoreOriginalValues()
    {
        if (tutorialTimeline != null && hasOpeningConversationOriginal && openingConversationOverrideApplied)
            tutorialTimeline.RunOpeningConversationSequence = originalOpeningConversationSequence;

        if (stairsUp != null && hasCanGoUpStairsOriginal && stairsOverrideApplied)
            stairsUp.CanGoUpStairs = originalCanGoUpStairs;
    }
}
