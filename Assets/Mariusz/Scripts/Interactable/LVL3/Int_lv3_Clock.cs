using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_Clock : Lvl3ClockworkInteraction
{
    [Header("Clock Hands")]
    [SerializeField] private Transform hourHand;
    [SerializeField] private Transform minuteHand;
    [Header("Time")]
    [SerializeField, Range(1, 12)] private int defaultHour = 1;
    [SerializeField, Range(1, 12)] private int correctHour = 5;
    [Tooltip("Visual rotation in global Z applied to the hour hand on one click.")]
    [SerializeField, Min(0.1f)] private float hourHandDegreesPerClick = 30f;
    [Tooltip("Visual rotation in global Z applied to the minute hand on one click.")]
    [SerializeField, Min(0.1f)] private float minuteHandDegreesPerClick = 360f;
    [SerializeField, Min(0.05f)] private float turnDuration = 0.35f;

    [Header("First Interaction")]
    [TextArea]
    [SerializeField] private string firstInteractionText = "Stary zegar. Jego wskazówki wciąż działają.";
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioClip firstInteractionAudio;
    [SerializeField] private DetectiveIdeaPoint brickClockIdeaPoint;

    [Header("Feedback")]
    [SerializeField] private AudioSource tickAudioSource;
    [SerializeField] private AudioSource correctTimeAudioSource;
    [SerializeField] private bool triggerCorrectTimeOnlyOnce = true;
    [SerializeField] private UnityEvent onCorrectTime;

    [Header("Debug")]
    [Tooltip("Runtime state: true when the logical clock hour matches Correct Hour.")]
    [SerializeField] private bool isCorrectHourDebug;

    private Vector3 initialHourHandWorldEuler;
    private Vector3 initialMinuteHandWorldEuler;
    private int currentHour;
    private int completedTurns;
    private bool isTurning;
    private bool correctTimeTriggered;
    private bool hasBeenExamined;

    public bool IsCorrectTime => isCorrectHourDebug;
    public int CurrentHour => currentHour;

    protected override void Awake()
    {
        base.Awake();

        if (hourHand != null)
            initialHourHandWorldEuler = hourHand.eulerAngles;

        if (minuteHand != null)
            initialMinuteHandWorldEuler = minuteHand.eulerAngles;

        currentHour = defaultHour;
        UpdateCorrectHourDebugState();
    }

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        if (isTurning)
            return;

        if (!hasBeenExamined)
        {
            hasBeenExamined = true;
            ShowTopText(firstInteractionText);

            if (sherlockVoiceSource != null && firstInteractionAudio != null)
                sherlockVoiceSource.PlayOneShot(firstInteractionAudio);

            brickClockIdeaPoint?.RevealFromExternalSource();

            return;
        }

        if (hourHand == null && minuteHand == null)
        {
            Debug.LogWarning($"{nameof(Int_lv3_Clock)} on '{name}' has no assigned clock hands.", this);
            return;
        }

        StartCoroutine(TurnClockForward());
    }

    private IEnumerator TurnClockForward()
    {
        isTurning = true;
        tickAudioSource?.Play();

        int previousTurns = completedTurns;
        completedTurns++;
        currentHour = currentHour == 12 ? 1 : currentHour + 1;
        UpdateCorrectHourDebugState();

        Quaternion hourTargetRotation = GetHourRotation(completedTurns);
        Quaternion minuteTargetRotation = GetMinuteRotation(completedTurns);

        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);

            if (hourHand != null)
                hourHand.rotation = GetHourRotation(previousTurns + progress);

            // The minute hand makes a full visual turn for every hour advanced.
            if (minuteHand != null)
                minuteHand.rotation = GetMinuteRotation(previousTurns + progress);

            yield return null;
        }

        if (hourHand != null)
            hourHand.rotation = hourTargetRotation;

        if (minuteHand != null)
            minuteHand.rotation = minuteTargetRotation;

        isTurning = false;

        if (IsCorrectTime && (!triggerCorrectTimeOnlyOnce || !correctTimeTriggered))
        {
            correctTimeTriggered = true;
            correctTimeAudioSource?.Play();
            onCorrectTime?.Invoke();
        }
    }

    private Quaternion GetHourRotation(float turns)
    {
        return Quaternion.Euler(
            initialHourHandWorldEuler.x,
            initialHourHandWorldEuler.y,
            initialHourHandWorldEuler.z + turns * hourHandDegreesPerClick);
    }

    private Quaternion GetMinuteRotation(float turns)
    {
        return Quaternion.Euler(
            initialMinuteHandWorldEuler.x,
            initialMinuteHandWorldEuler.y,
            initialMinuteHandWorldEuler.z + turns * minuteHandDegreesPerClick);
    }

    private void UpdateCorrectHourDebugState()
    {
        isCorrectHourDebug = currentHour == correctHour;
    }
}
