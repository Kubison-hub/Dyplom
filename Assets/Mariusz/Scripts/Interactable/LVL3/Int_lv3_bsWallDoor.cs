using UnityEngine;

public class Int_lv3_bsWallDoor : Lvl3LoupeWallPuzzleBase
{
    [Header("Light Requirement")]
    [Tooltip("At least one of these lamps must be active to inspect or trace this wall.")]
    [SerializeField] private GameObject[] heldLamps;
    [SerializeField, TextArea] private string darknessText = "Nic nie zobaczę w tych ciemnościach.";
    [Tooltip("Played through Sherlock Inspection Audio Source together with Darkness Text.")]
    [SerializeField] private AudioClip darknessVoiceClip;

    [Header("Camera")]
    [Tooltip("Optional. If empty, the active character's CameraController is used.")]
    [SerializeField] private CameraController cameraController;

    [Header("Watson On Puzzle Solved")]
    [SerializeField, Min(0f)] private float watsonApproachDistance = 2f;
    [SerializeField, Min(0.05f)] private float watsonApproachSpeedMultiplier = 1f;
    [SerializeField, Min(0.05f)] private float watsonRotationSpeedMultiplier = 1f;
    [SerializeField] private DetectiveIdeaPoint secretDoorIdeaPoint;

    [Header("Inspection Dialogue")]
    [SerializeField] private AudioSource sherlockInspectionAudioSource;
    [SerializeField] private AudioClip sherlockInspectionVoiceClip;
    [SerializeField, TextArea] private string sherlockInspectionText = "Przyjrzyjmy się bliżej tej ścianie.";

    protected override InteractionType PuzzleInteractionType => InteractionType.Int_lv3_bsWallDoor;
    protected override bool CanTraceLoupePattern => HasActiveLamp();
    protected override bool ShouldShowLoupePattern => HasActiveLamp();
    protected override bool ShowPatternOnlyInEagleVision => true;
    protected override bool RotateWatsonOnSolved => true;
    protected override float WatsonApproachDistanceOnSolved => watsonApproachDistance;
    protected override float WatsonApproachSpeedMultiplierOnSolved => watsonApproachSpeedMultiplier;
    protected override float WatsonRotationSpeedMultiplierOnSolved => watsonRotationSpeedMultiplier;

    protected override void OnPuzzleSolved()
    {
        GetCameraController()?.SetZoomState(CameraZoomState.Medium);
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

    public new void PerformInteraction(PlayerController player)
    {
        if (!HasActiveLamp())
        {
            ShowTopTextForPlayer(player, darknessText);

            if (sherlockInspectionAudioSource != null && darknessVoiceClip != null)
                sherlockInspectionAudioSource.PlayOneShot(darknessVoiceClip);

            if (player != null)
                player.currentInteractable = null;
            return;
        }

        GetCameraController()?.SetZoomState(CameraZoomState.Narrow);

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(sherlockInspectionText, string.Empty);

        if (sherlockInspectionAudioSource != null && sherlockInspectionVoiceClip != null)
            sherlockInspectionAudioSource.PlayOneShot(sherlockInspectionVoiceClip);

        if (player != null)
            player.currentInteractable = null;
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

    private CameraController GetCameraController()
    {
        if (cameraController != null)
            return cameraController;

        if (SwitchCharacter.Instance == null || SwitchCharacter.Instance.playersCamera == null)
            return null;

        int activePlayerIndex = SwitchCharacter.Instance.activePlayerIndex;
        if (activePlayerIndex < 0 || activePlayerIndex >= SwitchCharacter.Instance.playersCamera.Length)
            return null;

        var activeCamera = SwitchCharacter.Instance.playersCamera[activePlayerIndex];
        return activeCamera != null ? activeCamera.GetComponent<CameraController>() : null;
    }

    private static void ShowTopTextForPlayer(PlayerController player, string text)
    {
        if (PlayerTopText.Instance == null)
            return;

        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
        if (isWatson)
            PlayerTopText.Instance.ShowWatsonTopText(text);
        else
            PlayerTopText.Instance.ShowTopText(text, string.Empty);
    }

    private void Reset() => SetupPuzzle();
    private void OnValidate() => SetupPuzzle();
}
