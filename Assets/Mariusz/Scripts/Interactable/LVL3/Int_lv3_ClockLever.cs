using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ClockLever : Lvl3ClockworkInteraction
{
    [SerializeField] private Lvl3ClockworkPuzzleController puzzleController;
    [SerializeField] private Transform lever;
    [SerializeField] private Vector3 pulledLocalEulerOffset = new Vector3(-45f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float leverAnimationDuration = 0.2f;
    [SerializeField, Min(0.1f)] private float holdRange = 1.25f;
    [SerializeField] private AudioSource leverAudioSource;

    private PlayerController holder;
    private Quaternion releasedLocalRotation;
    private Quaternion pulledLocalRotation;
    private Coroutine animationRoutine;

    protected override void Awake()
    {
        base.Awake();
        if (lever == null)
            lever = transform;
        releasedLocalRotation = lever.localRotation;
        pulledLocalRotation = releasedLocalRotation * Quaternion.Euler(pulledLocalEulerOffset);
    }

    private void Update()
    {
        if (holder != null && Vector3.Distance(holder.transform.position, GetHoldPoint().position) > holdRange)
            ForceRelease(true);
    }

    private void OnDisable()
    {
        ForceRelease(true);
    }

    public override void PerformInteraction(PlayerController player)
    {
        if (holder != null)
            return;

        if (puzzleController == null || !puzzleController.TryHoldLever(this, player))
        {
            ShowTopText("Dźwignia nie odpowiada.", "Mechanizm musi najpierw ruszyć.");
            ClearPlayerInteraction(player);
            return;
        }

        holder = player;
        AnimateLever(pulledLocalRotation);
        leverAudioSource?.Play();
        ClearPlayerInteraction(player);
    }

    public void ForceRelease(bool notifyController)
    {
        if (holder == null)
            return;

        PlayerController previousHolder = holder;
        holder = null;
        AnimateLever(releasedLocalRotation);

        if (notifyController && puzzleController != null)
            puzzleController.ReleaseLever(this, previousHolder);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        if (Interactable != null)
            Interactable.isInteractableActive = isEnabled;
    }

    private Transform GetHoldPoint()
    {
        return Interactable != null && Interactable.interactabePoint != null
            ? Interactable.interactabePoint
            : transform;
    }

    private void AnimateLever(Quaternion targetRotation)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateLeverRoutine(targetRotation));
    }

    private IEnumerator AnimateLeverRoutine(Quaternion targetRotation)
    {
        Quaternion startRotation = lever.localRotation;
        float elapsed = 0f;

        while (elapsed < leverAnimationDuration)
        {
            elapsed += Time.deltaTime;
            lever.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / leverAnimationDuration);
            yield return null;
        }

        lever.localRotation = targetRotation;
        animationRoutine = null;
    }
}
