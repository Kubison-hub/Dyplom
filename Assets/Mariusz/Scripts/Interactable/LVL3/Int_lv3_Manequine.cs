using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_Manequine : Lvl3InteractionDialogueBase
{
    [Header("Repeat Inspection Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] secondInteractionDialogueLines;

    [Header("Inspection Idea Point")]
    [SerializeField] private DetectiveIdeaPoint mannequinIdeaPoint;

    [Header("Interaction Reaction Result")]
    [Tooltip("Activated after both characters finish their standard NavMesh approach and turn toward the mannequin.")]
    [SerializeField] private GameObject activateAfterBothCharactersArrive;
    [SerializeField, Min(0.01f)] private float mannequinLookTurnDuration = 0.35f;

    [Header("Secret Door Trap")]
    [SerializeField] private Animator[] secretDoorAnimators;
    [SerializeField] private string secretDoorCloseTrigger = "Close";
    [SerializeField] private bool setOpenedBoolOnDoors;
    [SerializeField] private string openedBool = "Opened";
    [SerializeField] private AudioSource[] secretDoorAudioSources;
    [SerializeField] private Collider[] collidersToEnableWhenClosed;
    [Tooltip("Object moved out of the room hierarchy after the doors finish closing.")]
    [SerializeField] private Transform objectToReparent;
    [Tooltip("New parent assigned before Room To Deactivate is disabled.")]
    [SerializeField] private Transform targetParent;
    [Tooltip("Room disabled after every secret door animator finishes closing.")]
    [SerializeField] private GameObject roomToDeactivate;
    [Tooltip("Optional marker the characters face when the trap doors close. Defaults to the first door animator.")]
    [SerializeField] private Transform trapDoorLookTarget;
    [SerializeField, Min(0.01f)] private float trapDoorTurnDuration = 0.7f;

    [Header("Blackboard Trap Reveal")]
    [Tooltip("Blackboard covering the area after the trap doors close.")]
    [SerializeField] private GameObject blackBoardToEnableOnTrap;
    [SerializeField] private Material blackBoardFadeMaterial;
    [Tooltip("Material assigned after the blackboard fade finishes. Leave empty to keep the fade material.")]
    [SerializeField] private Material blackBoardTargetMaterial;
    [Tooltip("Delay after the Close trigger before the blackboard fades in.")]
    [SerializeField, Min(0f)] private float blackBoardFadeDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float blackBoardFadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float blackBoardTargetAlpha = 1f;

    [Header("Mannequin Trap Animation")]
    [Tooltip("Leave empty to rotate the object that owns this component.")]
    [SerializeField] private Transform mannequinTransform;
    [SerializeField] private float mannequinLocalYRotation = 360f;
    [SerializeField, Min(0.01f)] private float mannequinRotationDuration = 0.8f;
    [SerializeField] private AudioSource mannequinTrapAudioSource;
    [SerializeField] private AudioClip mannequinTrapAudioClip;

    [Header("Trap Result Dialogue")]
    [SerializeField] private bool disableAfterTrigger = true;
    [SerializeField] private Lvl3DialogueLine[] trapResultDialogueLines;
    [SerializeField] private DetectiveIdeaPoint trapDoorIdeaPoint;

    private bool triggered;
    private bool hasBeenExamined;
    private bool triggerTrapAfterInspectionDialogue;
    private bool revealTrapDoorIdeaAfterDialogue;
    private Coroutine activateAfterApproachRoutine;
    public bool IsFirstInspection => !hasBeenExamined;
    public bool IsTrapTriggered => triggered;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ciekawa konstrukcja. Lepiej jej nie przenosić bez powodu.",
            duration = 3f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Zgadzam się, Sherlocku.",
            duration = 2f
        }
    };

    private void Reset() => Setup();
    private void OnValidate() => Setup();
    private void Awake() => Setup();

    private bool revealBasementExitAfterInspectionDialogue;

    public void PerformInteraction(PlayerController player)
    {
        BeginActivateAfterApproach();

        if (hasBeenExamined)
        {
            PlayDialogue(player, secondInteractionDialogueLines);
            return;
        }

        hasBeenExamined = true;
        mannequinIdeaPoint?.RevealFromExternalSource();
        revealBasementExitAfterInspectionDialogue = true;
        triggerTrapAfterInspectionDialogue = true;
        PlayInteractionDialogue(player);
    }

    private void BeginActivateAfterApproach()
    {
        if (activateAfterBothCharactersArrive == null || activateAfterBothCharactersArrive.activeSelf)
            return;

        if (activateAfterApproachRoutine != null)
            StopCoroutine(activateAfterApproachRoutine);

        activateAfterApproachRoutine = StartCoroutine(ActivateAfterBothCharactersArrive());
    }

    private IEnumerator ActivateAfterBothCharactersArrive()
    {
        Interactable interactable = GetComponent<Interactable>();
        while (interactable != null && interactable.IsCompanionReactionApproachInProgress)
            yield return null;

        yield return RotateCharactersToward(transform, mannequinLookTurnDuration);

        if (activateAfterBothCharactersArrive != null)
            activateAfterBothCharactersArrive.SetActive(true);

        activateAfterApproachRoutine = null;
    }

    public void TriggerWatsonTrap(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;

        if (triggered)
            return;

        triggered = true;

        foreach (Animator doorAnimator in secretDoorAnimators)
        {
            if (doorAnimator == null)
                continue;

            if (!string.IsNullOrWhiteSpace(secretDoorCloseTrigger))
                doorAnimator.SetTrigger(secretDoorCloseTrigger);

            if (setOpenedBoolOnDoors && !string.IsNullOrWhiteSpace(openedBool))
                doorAnimator.SetBool(openedBool, false);
        }

        foreach (AudioSource doorAudioSource in secretDoorAudioSources)
        {
            if (doorAudioSource != null)
                doorAudioSource.Play();
        }

        StartCoroutine(DeactivateRoomAfterDoorsClose());
        StartCoroutine(FadeBlackBoardInAfterDoorClose());
        StartCoroutine(RotateCharactersTowardTrapDoor());

        if (mannequinTrapAudioSource != null && mannequinTrapAudioClip != null)
            mannequinTrapAudioSource.PlayOneShot(mannequinTrapAudioClip);

        StartCoroutine(RotateMannequin());

        foreach (Collider doorCollider in collidersToEnableWhenClosed)
        {
            if (doorCollider != null)
                doorCollider.enabled = true;
        }

        if (disableAfterTrigger)
        {
            Interactable interactable = GetComponent<Interactable>();
            if (interactable != null)
                interactable.isInteractableActive = false;
        }

        revealTrapDoorIdeaAfterDialogue = true;
        PlayDialogue(player, trapResultDialogueLines);
    }

    private IEnumerator DeactivateRoomAfterDoorsClose()
    {
        // Let Animator process the Close trigger before reading its new state.
        yield return null;

        bool doorsAreClosing;
        do
        {
            doorsAreClosing = false;

            foreach (Animator doorAnimator in secretDoorAnimators)
            {
                if (doorAnimator == null || !doorAnimator.isActiveAndEnabled)
                    continue;

                AnimatorStateInfo state = doorAnimator.GetCurrentAnimatorStateInfo(0);
                if (doorAnimator.IsInTransition(0) || state.normalizedTime < 1f)
                {
                    doorsAreClosing = true;
                    break;
                }
            }

            if (doorsAreClosing)
                yield return null;
        }
        while (doorsAreClosing);

        if (objectToReparent != null && targetParent != null)
            objectToReparent.SetParent(targetParent, true);

        if (roomToDeactivate != null)
            roomToDeactivate.SetActive(false);
    }

    private IEnumerator RotateMannequin()
    {
        Transform target = mannequinTransform != null ? mannequinTransform : transform;
        Quaternion startRotation = target.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0f, mannequinLocalYRotation, 0f);
        float elapsed = 0f;

        while (elapsed < mannequinRotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / mannequinRotationDuration);
            // A 360-degree target has the same quaternion as the start rotation,
            // so Slerp would show no movement. Build the local Y turn directly.
            target.localRotation = startRotation * Quaternion.Euler(
                0f,
                mannequinLocalYRotation * progress,
                0f);
            yield return null;
        }

        target.localRotation = endRotation;
    }

    private IEnumerator FadeBlackBoardInAfterDoorClose()
    {
        if (blackBoardFadeDelay > 0f)
            yield return new WaitForSeconds(blackBoardFadeDelay);

        if (blackBoardToEnableOnTrap == null)
            yield break;

        blackBoardToEnableOnTrap.SetActive(true);

        Renderer blackBoardRenderer = blackBoardToEnableOnTrap.GetComponent<Renderer>();
        if (blackBoardRenderer == null)
            yield break;

        if (blackBoardFadeMaterial != null)
            blackBoardRenderer.material = blackBoardFadeMaterial;

        Material material = blackBoardRenderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (!material.HasProperty(colorProperty))
            yield break;

        Color color = material.GetColor(colorProperty);
        color.a = 0f;
        material.SetColor(colorProperty, color);

        float elapsed = 0f;
        while (elapsed < blackBoardFadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, blackBoardTargetAlpha, elapsed / blackBoardFadeDuration);
            material.SetColor(colorProperty, color);
            yield return null;
        }

        color.a = blackBoardTargetAlpha;
        material.SetColor(colorProperty, color);

        if (blackBoardTargetMaterial != null)
            blackBoardRenderer.material = blackBoardTargetMaterial;
    }

    private IEnumerator RotateCharactersTowardTrapDoor()
    {
        Transform lookTarget = trapDoorLookTarget;
        if (lookTarget == null && secretDoorAnimators != null)
        {
            foreach (Animator doorAnimator in secretDoorAnimators)
            {
                if (doorAnimator != null)
                {
                    lookTarget = doorAnimator.transform;
                    break;
                }
            }
        }

        if (lookTarget == null)
            yield break;

        yield return RotateCharactersToward(lookTarget, trapDoorTurnDuration);
    }

    private static IEnumerator RotateCharactersToward(Transform lookTarget, float duration)
    {
        if (lookTarget == null)
            yield break;

        Transform sherlock = SwitchCharacter.Instance != null
            ? SwitchCharacter.Instance.sherlockTransform
            : null;
        Transform watson = SwitchCharacter.Instance != null
            ? SwitchCharacter.Instance.watsonTransform
            : null;

        Quaternion sherlockStart = sherlock != null ? sherlock.rotation : Quaternion.identity;
        Quaternion watsonStart = watson != null ? watson.rotation : Quaternion.identity;
        Quaternion sherlockTarget = GetLookRotation(sherlock, lookTarget, sherlockStart);
        Quaternion watsonTarget = GetLookRotation(watson, lookTarget, watsonStart);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            if (sherlock != null)
                sherlock.rotation = Quaternion.Slerp(sherlockStart, sherlockTarget, progress);

            if (watson != null)
                watson.rotation = Quaternion.Slerp(watsonStart, watsonTarget, progress);

            yield return null;
        }

        if (sherlock != null)
            sherlock.rotation = sherlockTarget;

        if (watson != null)
            watson.rotation = watsonTarget;
    }

    private static Quaternion GetLookRotation(Transform character, Transform lookTarget, Quaternion fallback)
    {
        if (character == null || lookTarget == null)
            return fallback;

        Vector3 direction = lookTarget.position - character.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : fallback;
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (revealBasementExitAfterInspectionDialogue)
        {
            revealBasementExitAfterInspectionDialogue = false;
            CluesLog.Instance?.AddFindBasementExitObjective();
        }

        if (triggerTrapAfterInspectionDialogue && !triggered)
        {
            triggerTrapAfterInspectionDialogue = false;
            TriggerWatsonTrap(null);
            return;
        }

        if (!revealTrapDoorIdeaAfterDialogue || lines != trapResultDialogueLines)
            return;

        revealTrapDoorIdeaAfterDialogue = false;
        trapDoorIdeaPoint?.RevealFromExternalSource();
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_Manequine);

        if (secondInteractionDialogueLines == null || secondInteractionDialogueLines.Length == 0)
        {
            secondInteractionDialogueLines = new[]
            {
                new Lvl3DialogueLine
                {
                    speaker = Lvl3DialogueSpeaker.Sherlock,
                    text = "Lepiej zostawmy go w spokoju, Watsonie.",
                    duration = 2.5f
                }
            };
        }

        if (trapResultDialogueLines != null && trapResultDialogueLines.Length > 0)
            return;

        trapResultDialogueLines = new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = "Watsonie, to była pułapka.",
                duration = 2.5f
            },
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Watson,
                text = "Manekin nie jest tym, czym się wydawał.",
                duration = 3f
            }
        };
    }
}
