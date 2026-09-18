using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ClockBrickButton : Lvl3ClockworkInteraction
{
    [SerializeField] private Lvl3ClockworkPuzzleController puzzleController;
    [SerializeField] private Transform brick;
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, 0f, -0.06f);
    [SerializeField, Min(0.01f)] private float pressAnimationDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float holdRange = 1.25f;
    [SerializeField] private AudioSource brickAudioSource;

    private PlayerController holder;
    private Vector3 releasedLocalPosition;
    private Vector3 pressedLocalPosition;
    private Coroutine animationRoutine;

    protected override void Awake()
    {
        base.Awake();
        if (brick == null)
            brick = transform;
        releasedLocalPosition = brick.localPosition;
        pressedLocalPosition = releasedLocalPosition + pressedLocalOffset;

        InitializeQuestionFX();
    }

    private void Update()
    {
        if (holder == null)
            return;

        if (Vector3.Distance(holder.transform.position, GetHoldPoint().position) > holdRange)
            ForceRelease(true, true);
    }

    private void OnDisable()
    {
        ForceRelease(true, false);
    }

    public override void PerformInteraction(PlayerController player)
    {
        if (holder != null || puzzleController == null)
            return;

        if (!puzzleController.TryHoldBrick(this, player))
        {
            ShowTopText("Potrzebujemy obu cegieł.", "Jedna osoba nie utrzyma ich jednocześnie.");
            ClearPlayerInteraction(player);
            return;
        }

        holder = player;
        brickAudioSource?.Play();
        AnimateBrick(pressedLocalPosition);
        ClearPlayerInteraction(player);
        puzzleController.NotifyBrickPressed();
    }

    public void ForceRelease(bool notifyController, bool playReleaseAudio = false)
    {
        if (holder == null)
            return;

        PlayerController previousHolder = holder;
        holder = null;
        if (playReleaseAudio)
            brickAudioSource?.Play();
        AnimateBrick(releasedLocalPosition);

        if (notifyController && puzzleController != null)
            puzzleController.ReleaseBrick(this, previousHolder);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        if (Interactable == null)
            return;

        Interactable.isInteractableActive = isEnabled;

        if (isEnabled)
        {
            InitializeQuestionFX();
            return;
        }

        Interactable.SetQuestionFXEagleVisionState(false);
        if (Interactable.questionVFX != null)
            Interactable.questionVFX.gameObject.SetActive(false);
    }

    private void InitializeQuestionFX()
    {
        if (Interactable == null)
            return;

        if (Interactable.questionVFX == null)
        {
            Transform questionFxTransform = transform.Find("QuestionFX");
            if (questionFxTransform != null)
                Interactable.questionVFX = questionFxTransform.GetComponent<UnityEngine.VFX.VisualEffect>();
        }

        if (Interactable.questionVFX == null)
            return;

        Interactable.questionVFX.gameObject.SetActive(true);
        Interactable.SetQuestionFXRate(0f);
    }

    private Transform GetHoldPoint()
    {
        return Interactable != null && Interactable.interactabePoint != null
            ? Interactable.interactabePoint
            : transform;
    }

    private void AnimateBrick(Vector3 targetPosition)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateBrickRoutine(targetPosition));
    }

    private IEnumerator AnimateBrickRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = brick.localPosition;
        float elapsed = 0f;

        while (elapsed < pressAnimationDuration)
        {
            elapsed += Time.deltaTime;
            brick.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / pressAnimationDuration);
            yield return null;
        }

        brick.localPosition = targetPosition;
        animationRoutine = null;
    }
}
