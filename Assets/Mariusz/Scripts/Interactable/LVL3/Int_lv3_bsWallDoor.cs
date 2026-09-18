using System.Collections;
using UnityEngine;

public class Int_lv3_bsWallDoor : Lvl3LoupeWallPuzzleBase
{
    [Header("Basement Exit Objective")]
    [Tooltip("Enable on any wall that should introduce the objective to escape the basement room.")]
    [SerializeField] private bool puzzleDoor;
    [Tooltip("Waits for this wall's inspection text and audio before adding the basement exit objective.")]
    [SerializeField, Min(0f)] private float puzzleDoorDialogueDuration = 4f;

    [Header("Light Requirement")]
    [Tooltip("At least one of these lamps must be active to inspect or trace this wall.")]
    [SerializeField] private GameObject[] heldLamps;
    [SerializeField, TextArea] private string darknessText = "Nic nie zobaczę w tych ciemnościach.";
    [Tooltip("Played as Sherlock's dialogue line together with Darkness Text.")]
    [SerializeField] private AudioClip darknessVoiceClip;

    [Header("Watson On Puzzle Solved")]
    [SerializeField, Min(0f)] private float watsonApproachDistance = 2f;
    [SerializeField, Min(0.05f)] private float watsonApproachSpeedMultiplier = 1f;
    [SerializeField, Min(0.05f)] private float watsonRotationSpeedMultiplier = 1f;
    [SerializeField] private DetectiveIdeaPoint secretDoorIdeaPoint;

    [Header("Inspection Dialogue")]
    [SerializeField] private AudioClip sherlockInspectionVoiceClip;
    [SerializeField, TextArea] private string sherlockInspectionText = "Przyjrzyjmy się bliżej tej ścianie.";

    protected override InteractionType PuzzleInteractionType => InteractionType.Int_lv3_bsWallDoor;
    protected override bool CanTraceLoupePattern => HasActiveLamp() && IsSherlockActive();
    protected override bool ShouldShowLoupePattern => HasActiveLamp() && IsSherlockActive();
    protected override bool ShowPatternOnlyInEagleVision => true;
    protected override bool RotateWatsonOnSolved => true;
    protected override float WatsonApproachDistanceOnSolved => watsonApproachDistance;
    protected override float WatsonApproachSpeedMultiplierOnSolved => watsonApproachSpeedMultiplier;
    protected override float WatsonRotationSpeedMultiplierOnSolved => watsonRotationSpeedMultiplier;

    protected override void OnPuzzleSolved()
    {
        secretDoorIdeaPoint?.RevealFromExternalSource();
    }

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Tutaj, Watsonie. Ta ceglana ściana widocznie wisi na ruchomym mechanizmie.",
            duration = 4f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Ach, tak.",
            duration = 2f
        }
    };

    private bool basementExitObjectiveQueued;

    public new void PerformInteraction(PlayerController player)
    {
        if (!HasActiveLamp())
        {
            PlaySherlockDialogueLine(player, darknessText, darknessVoiceClip);
            return;
        }

        if (puzzleDoor && !basementExitObjectiveQueued)
        {
            basementExitObjectiveQueued = true;
            StartCoroutine(AddBasementExitObjectiveAfterInspection());
        }

        PlaySherlockDialogueLine(
            player,
            sherlockInspectionText,
            sherlockInspectionVoiceClip,
            puzzleDoorDialogueDuration);
    }

    private IEnumerator AddBasementExitObjectiveAfterInspection()
    {
        float waitDuration = sherlockInspectionVoiceClip != null
            ? Mathf.Max(puzzleDoorDialogueDuration, sherlockInspectionVoiceClip.length)
            : puzzleDoorDialogueDuration;

        if (waitDuration > 0f)
            yield return new WaitForSeconds(waitDuration);

        CluesLog.Instance?.AddFindBasementExitObjective();
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

    private static bool IsSherlockActive()
    {
        return SwitchCharacter.Instance == null || SwitchCharacter.Instance.activePlayerIndex == 0;
    }

    private void PlaySherlockDialogueLine(
        PlayerController player,
        string text,
        AudioClip voiceClip,
        float minimumDuration = 0f)
    {
        float defaultDuration = PlayerTopText.Instance != null
            ? PlayerTopText.Instance.textTime
            : 3f;
        float voiceDuration = voiceClip != null ? voiceClip.length : 0f;
        float duration = Mathf.Max(minimumDuration, voiceDuration, defaultDuration);

        PlayDialogue(player, new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = text,
                voiceClip = voiceClip,
                duration = duration
            }
        });
    }

    private void Reset() => SetupPuzzle();
    private void OnValidate() => SetupPuzzle();
}
