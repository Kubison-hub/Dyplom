using System.Collections;
using UnityEngine;

public class Lvl3ClockworkPuzzleController : MonoBehaviour
{
    [Header("Clock Requirement")]
    [SerializeField] private Int_lv3_Clock clock;
    [SerializeField] private bool requireCorrectClockTime = true;

    [Header("Puzzle Elements")]
    [SerializeField] private Int_lv3_ClockBrickButton leftBrickButton;
    [SerializeField] private Int_lv3_ClockBrickButton rightBrickButton;
    [SerializeField] private Int_lv3_ClockLever lever;
    [SerializeField] private Int_lv3_ClockSecretPassage secretPassage;
    [SerializeField] private Transform mannequin;
    [SerializeField] private GameObject secretBarrier;

    [Header("Clock Sequence")]
    [SerializeField, Min(1)] private int clockStrikes = 8;
    [SerializeField, Min(0f)] private float rotationsPerStrike = 2f;
    [SerializeField, Min(0.05f)] private float spinDurationPerStrike = 0.9f;
    [SerializeField, Min(0f)] private float pauseBetweenStrikes = 0.1f;
    [SerializeField, Min(0f)] private float finalRotations = 4f;
    [SerializeField, Min(0.05f)] private float finalSpinDuration = 1.8f;
    [SerializeField] private AudioSource clockAudioSource;
    [SerializeField] private AudioClip clockStrikeClip;

    private PlayerController leftBrickHolder;
    private PlayerController rightBrickHolder;
    private PlayerController leverHolder;
    private Coroutine clockSequenceRoutine;

    public bool IsSolved { get; private set; }
    public bool IsMannequinSpinning { get; private set; }
    public bool IsSecretOpeningAllowed => !IsSolved && IsMannequinSpinning && leverHolder != null;
    public bool IsClockTimeCorrect => !requireCorrectClockTime ||
                                      (clock != null && clock.isActiveAndEnabled && clock.IsCorrectTime);

    private void Start()
    {
        SetSecretBarrierLocked(true);
    }

    public bool TryHoldBrick(Int_lv3_ClockBrickButton button, PlayerController player)
    {
        if (IsSolved || clockSequenceRoutine != null || button == null || player == null)
            return false;

        if (button == leftBrickButton)
        {
            if (rightBrickHolder == player)
                return false;

            leftBrickHolder = player;
            return true;
        }

        if (button == rightBrickButton)
        {
            if (leftBrickHolder == player)
                return false;

            rightBrickHolder = player;
            return true;
        }

        Debug.LogWarning($"{name}: Brick button is not assigned to this controller.", button);
        return false;
    }

    public void ReleaseBrick(Int_lv3_ClockBrickButton button, PlayerController player)
    {
        if (button == null)
            return;

        bool bothBricksWereHeld = leftBrickHolder != null && rightBrickHolder != null;

        if (button == leftBrickButton && leftBrickHolder == player)
            leftBrickHolder = null;
        else if (button == rightBrickButton && rightBrickHolder == player)
            rightBrickHolder = null;

        if (bothBricksWereHeld && !IsSolved && clockSequenceRoutine == null && IsClockTimeCorrect)
            clockSequenceRoutine = StartCoroutine(ClockSequence());
    }

    public bool TryHoldLever(Int_lv3_ClockLever requestedLever, PlayerController player)
    {
        if (requestedLever != lever || IsSolved || !IsMannequinSpinning || leverHolder != null)
            return false;

        leverHolder = player;
        SetSecretBarrierLocked(false);
        return true;
    }

    public void ReleaseLever(Int_lv3_ClockLever releasedLever, PlayerController player)
    {
        if (releasedLever != lever || leverHolder != player)
            return;

        leverHolder = null;

        if (!IsSolved)
            SetSecretBarrierLocked(true);
    }

    public void CompletePuzzle()
    {
        if (IsSolved)
            return;

        IsSolved = true;
        IsMannequinSpinning = false;
        SetSecretBarrierLocked(false);

        if (clockSequenceRoutine != null)
        {
            StopCoroutine(clockSequenceRoutine);
            clockSequenceRoutine = null;
        }

        leftBrickHolder = null;
        rightBrickHolder = null;
        leverHolder = null;

        if (leftBrickButton != null)
        {
            leftBrickButton.ForceRelease(false);
            leftBrickButton.SetInteractionEnabled(false);
        }

        if (rightBrickButton != null)
        {
            rightBrickButton.ForceRelease(false);
            rightBrickButton.SetInteractionEnabled(false);
        }

        if (lever != null)
        {
            lever.ForceRelease(false);
            lever.SetInteractionEnabled(false);
        }
    }

    private IEnumerator ClockSequence()
    {
        leftBrickHolder = null;
        rightBrickHolder = null;

        if (leftBrickButton != null)
            leftBrickButton.ForceRelease(false);

        if (rightBrickButton != null)
            rightBrickButton.ForceRelease(false);

        IsMannequinSpinning = true;
        SetSecretBarrierLocked(false);
        secretPassage?.OpenFromPuzzle(null);

        for (int strike = 0; strike < clockStrikes; strike++)
        {
            PlayClockStrike();
            yield return RotateMannequin(rotationsPerStrike, spinDurationPerStrike);

            if (pauseBetweenStrikes > 0f)
                yield return new WaitForSeconds(pauseBetweenStrikes);
        }

        if (finalRotations > 0f)
            yield return RotateMannequin(finalRotations, finalSpinDuration);

        IsMannequinSpinning = false;
        clockSequenceRoutine = null;
        CompletePuzzle();
    }

    private IEnumerator RotateMannequin(float rotations, float duration)
    {
        if (mannequin == null || rotations <= 0f)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float targetDegrees = rotations * 360f;
        float rotatedDegrees = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float nextDegrees = Mathf.Lerp(0f, targetDegrees, progress);
            mannequin.Rotate(Vector3.up, nextDegrees - rotatedDegrees, Space.Self);
            rotatedDegrees = nextDegrees;
            elapsed += Time.deltaTime;
            yield return null;
        }

        mannequin.Rotate(Vector3.up, targetDegrees - rotatedDegrees, Space.Self);
    }

    private void PlayClockStrike()
    {
        if (clockAudioSource != null && clockStrikeClip != null)
            clockAudioSource.PlayOneShot(clockStrikeClip);
    }

    private void SetSecretBarrierLocked(bool isLocked)
    {
        if (secretBarrier != null)
            secretBarrier.SetActive(isLocked);
    }
}
