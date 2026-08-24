using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_bsWallButton : Lvl3ClockworkInteraction
{
    [Header("Light Requirement")]
    [Tooltip("At least one assigned lamp must be active for this brick to be interactable.")]
    [SerializeField] private GameObject[] heldLamps;

    [Header("Loupe QuestionFX Discovery")]
    [SerializeField] private MagnifierGlassController magnifier;
    [Tooltip("Leave empty to use this interaction's collider.")]
    [SerializeField] private Collider loupeDetectionCollider;
    [Tooltip("Leave empty to use Interactable.Question VFX.")]
    [SerializeField] private GameObject questionFxObject;
    [SerializeField, Min(0.1f)] private float loupeHoldDuration = 1f;
    [SerializeField] private string unrevealedQuestionFxLayer = "Clues";
    [SerializeField] private string revealedQuestionFxLayer = "Hidden";

    [Header("Secret Wall Door")]
    [SerializeField] private Int_lv3_SecretWallDoor secretWallDoor;

    [Header("Brick")]
    [SerializeField] private Transform brick;
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, 0f, -0.06f);
    [SerializeField, Min(0.01f)] private float pressAnimationDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float holdRange = 1.25f;
    [SerializeField] private AudioSource buttonPressAudioSource;

    private PlayerController holder;
    private Vector3 releasedLocalPosition;
    private Vector3 pressedLocalPosition;
    private Coroutine animationRoutine;
    private Collider[] colliders;
    private bool[] colliderInitialStates;
    private bool interactionEnabledByPuzzle = true;
    private bool lightAvailabilityInitialized;
    private bool lightAvailable;
    private bool questionFxRevealed;
    private float loupeHoldStartedAt = -1f;

    protected override void Awake()
    {
        base.Awake();

        if (brick == null)
            brick = transform;

        releasedLocalPosition = brick.localPosition;
        pressedLocalPosition = releasedLocalPosition + pressedLocalOffset;

        interactionEnabledByPuzzle = Interactable == null || Interactable.isInteractableActive;
        CacheColliders();
        ResolveLoupeDiscoveryReferences();
        SetQuestionFxLayer(unrevealedQuestionFxLayer);
        UpdateLightAvailability();
    }

    private void Update()
    {
        UpdateLightAvailability();
        UpdateLoupeQuestionFxDiscovery();

        if (holder != null && Vector3.Distance(holder.transform.position, GetHoldPoint().position) > holdRange)
            ForceRelease(true);
    }

    private void OnDisable()
    {
        ForceRelease(false);
    }

    public override void PerformInteraction(PlayerController player)
    {
        if (holder != null || secretWallDoor == null)
            return;

        if (!secretWallDoor.TryHoldBrick(this, player))
        {
            ShowTopText("Potrzebne są dwie osoby.", "Jedna osoba nie utrzyma obu cegieł.");
            ClearPlayerInteraction(player);
            return;
        }

        holder = player;
        buttonPressAudioSource?.Play();
        AnimateBrick(pressedLocalPosition);
        ClearPlayerInteraction(player);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        interactionEnabledByPuzzle = isEnabled;
        ApplyInteractionAvailability();
    }

    private void CacheColliders()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        colliderInitialStates = new bool[colliders.Length];

        for (int index = 0; index < colliders.Length; index++)
            colliderInitialStates[index] = colliders[index] != null && colliders[index].enabled;
    }

    private void UpdateLightAvailability()
    {
        bool shouldBeAvailable = HasActiveLamp();
        if (lightAvailabilityInitialized && lightAvailable == shouldBeAvailable)
            return;

        lightAvailabilityInitialized = true;
        lightAvailable = shouldBeAvailable;

        if (!lightAvailable)
            ForceRelease(true);

        if (!lightAvailable)
            loupeHoldStartedAt = -1f;

        ApplyInteractionAvailability();
    }

    private void UpdateLoupeQuestionFxDiscovery()
    {
        if (questionFxRevealed || !lightAvailable || !IsSherlockActive())
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        ResolveLoupeDiscoveryReferences();
        if (magnifier == null || loupeDetectionCollider == null)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        bool loupeOverButton = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                               IsLoupeHitOnButton(hit.collider);
        if (!loupeOverButton)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        if (loupeHoldStartedAt < 0f)
            loupeHoldStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoldStartedAt < loupeHoldDuration)
            return;

        questionFxRevealed = true;
        loupeHoldStartedAt = -1f;
        SetQuestionFxLayer(revealedQuestionFxLayer);
    }

    private void ResolveLoupeDiscoveryReferences()
    {
        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (loupeDetectionCollider == null)
            loupeDetectionCollider = GetComponent<Collider>();

        if (questionFxObject == null && Interactable != null && Interactable.questionVFX != null)
        {
            questionFxObject = Interactable.questionVFX.gameObject;
            if (!questionFxRevealed)
                SetQuestionFxLayer(unrevealedQuestionFxLayer);
        }
    }

    private bool IsSherlockActive()
    {
        return SwitchCharacter.Instance == null || SwitchCharacter.Instance.activePlayerIndex == 0;
    }

    private bool IsLoupeHitOnButton(Collider hitCollider)
    {
        return hitCollider == loupeDetectionCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeDetectionCollider.transform);
    }

    private void SetQuestionFxLayer(string layerName)
    {
        if (questionFxObject == null || string.IsNullOrWhiteSpace(layerName))
            return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"{name}: layer '{layerName}' does not exist.", this);
            return;
        }

        foreach (Transform current in questionFxObject.GetComponentsInChildren<Transform>(true))
            current.gameObject.layer = layer;
    }

    private void ApplyInteractionAvailability()
    {
        bool isAvailable = interactionEnabledByPuzzle && lightAvailable;

        if (Interactable != null)
        {
            Interactable.isInteractableActive = isAvailable;

            if (!isAvailable)
            {
                Interactable.SetQuestionFXEagleVisionState(false);

                if (Interactable.interactiveShader != null)
                    Interactable.interactiveShader.SetActive(false);
            }
        }

        if (colliders == null || colliderInitialStates == null)
            return;

        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] != null)
                colliders[index].enabled = isAvailable && colliderInitialStates[index];
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

    private void ForceRelease(bool playReleaseAudio)
    {
        if (holder == null)
            return;

        PlayerController previousHolder = holder;
        holder = null;
        if (playReleaseAudio)
            buttonPressAudioSource?.Play();
        AnimateBrick(releasedLocalPosition);

        if (secretWallDoor != null)
            secretWallDoor.ReleaseBrick(this, previousHolder);
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
