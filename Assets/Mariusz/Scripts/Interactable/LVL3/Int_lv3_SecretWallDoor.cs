using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_SecretWallDoor : Lvl3ClockworkInteraction
{
    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private AudioSource doorOpenAudioSource;
    [SerializeField] private CinemachineImpulseSource cameraShakeImpulseSource;
    [SerializeField, Range(0f, 1f)] private float cameraShakeForce = 0.18f;

    [Header("Sherlock Dialogue")]
    [SerializeField] private AudioSource sherlockVoiceAudioSource;
    [SerializeField] private AudioClip sherlockVoiceAudioClip;
    [SerializeField] private string sherlockTitle = "Sherlock Holmes";
    [SerializeField] private string sherlockText = "Musi być jakiś sposób, aby przesunąć tę ścianę.";

    [Header("Dual Brick Buttons")]
    [SerializeField] private Int_lv3_bsWallButton brickOne;
    [SerializeField] private Int_lv3_bsWallButton brickTwo;

    [Header("Open Result")]
    [Tooltip("Room content activated immediately before the secret door starts opening.")]
    [SerializeField] private GameObject roomToActivate;
    [Tooltip("Colliders blocked by the closed secret wall. They are disabled once the door opens.")]
    [SerializeField] private Collider[] collidersToDisableOnOpen;
    [Tooltip("Temporary blackboard that hides this room before the secret wall opens.")]
    [SerializeField] private GameObject blackBoardToDisableOnOpen;
    [SerializeField] private Material blackBoardFadeMaterial;
    [Tooltip("Delay after triggering the door Open animation before the blackboard begins to fade.")]
    [SerializeField, Min(0f)] private float blackBoardFadeDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float blackBoardFadeDuration = 1f;

    private PlayerController brickOneHolder;
    private PlayerController brickTwoHolder;

    public bool IsOpened { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if (cameraShakeImpulseSource == null)
            cameraShakeImpulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public override bool CanPlayerUse(PlayerController player)
    {
        if (player == null)
            return false;

        return player.playerCharacter == PlayerCharacter.Sherlock ||
               player.CompareTag("PlayerA") ||
               (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 0);
    }

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        if (IsOpened)
            return;

        if (sherlockVoiceAudioSource != null && sherlockVoiceAudioClip != null)
            sherlockVoiceAudioSource.PlayOneShot(sherlockVoiceAudioClip);

        ShowTopText(sherlockTitle, sherlockText);
    }

    public bool TryHoldBrick(Int_lv3_bsWallButton brickButton, PlayerController player)
    {
        if (IsOpened || brickButton == null || player == null)
            return false;

        if (brickButton == brickOne)
        {
            if (brickTwoHolder == player)
                return false;

            brickOneHolder = player;
        }
        else if (brickButton == brickTwo)
        {
            if (brickOneHolder == player)
                return false;

            brickTwoHolder = player;
        }
        else
        {
            Debug.LogWarning($"{name}: Brick button is not assigned to this secret wall door.", brickButton);
            return false;
        }

        if (brickOneHolder != null && brickTwoHolder != null)
            OpenDoor();

        return true;
    }

    public void ReleaseBrick(Int_lv3_bsWallButton brickButton, PlayerController player)
    {
        if (brickButton == brickOne && brickOneHolder == player)
            brickOneHolder = null;
        else if (brickButton == brickTwo && brickTwoHolder == player)
            brickTwoHolder = null;
    }

    private void OpenDoor()
    {
        if (IsOpened)
            return;

        IsOpened = true;
        Interactable?.MarkCompleted();
        CluesLog.Instance?.RemoveFindBasementHiddenDoorObjective();

        if (roomToActivate != null)
            roomToActivate.SetActive(true);

        if (doorAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            doorAnimator.SetTrigger(openTrigger);

        doorOpenAudioSource?.Play();

        if (cameraShakeImpulseSource != null && cameraShakeForce > 0f)
            cameraShakeImpulseSource.GenerateImpulseWithForce(cameraShakeForce);

        if (brickOne != null)
            brickOne.SetInteractionEnabled(false);

        if (brickTwo != null)
            brickTwo.SetInteractionEnabled(false);

        foreach (Collider targetCollider in collidersToDisableOnOpen)
        {
            if (targetCollider != null)
                targetCollider.enabled = false;
        }

        StartCoroutine(FadeBlackBoardAfterDoorOpen());

        if (Interactable != null)
            Interactable.isInteractableActive = false;
    }

    private void FadeBlackBoard()
    {
        if (blackBoardToDisableOnOpen == null)
            return;

        Renderer blackBoardRenderer = blackBoardToDisableOnOpen.GetComponent<Renderer>();
        if (blackBoardRenderer == null)
        {
            blackBoardToDisableOnOpen.SetActive(false);
            return;
        }

        if (blackBoardFadeMaterial != null)
            blackBoardRenderer.material = blackBoardFadeMaterial;

        StartCoroutine(FadeBlackBoardAlpha(blackBoardRenderer.material));
    }

    private IEnumerator FadeBlackBoardAfterDoorOpen()
    {
        if (blackBoardFadeDelay > 0f)
            yield return new WaitForSeconds(blackBoardFadeDelay);

        FadeBlackBoard();
    }

    private IEnumerator FadeBlackBoardAlpha(Material material)
    {
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (!material.HasProperty(colorProperty))
        {
            blackBoardToDisableOnOpen.SetActive(false);
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < blackBoardFadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / blackBoardFadeDuration);
            material.SetColor(colorProperty, color);
            yield return null;
        }

        color.a = 0f;
        material.SetColor(colorProperty, color);
        blackBoardToDisableOnOpen.SetActive(false);
    }
}
