using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_HidenPassageButton : MonoBehaviour
{
    [Header("Secret Door")]
    [SerializeField] private Animator secretDoorAnimator;
    [SerializeField] private string openedBoolName = "Opened";
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";
    [SerializeField] private bool isDoorOpen;

    [Header("Button Animation")]
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressTriggerName = "Press";

    [Header("Room Blackboards")]
    [Tooltip("Volume for the room containing this button and one side of the secret door.")]
    [SerializeField] private Collider roomVolume;
    [Tooltip("Blackboard hiding this room when the active character is on the other side.")]
    [SerializeField] private GameObject roomBlackboard;
    [Tooltip("One blackboard hiding the rest of the rooms when the active character is inside Room Volume.")]
    [SerializeField] private GameObject otherRoomsBlackboard;
    [SerializeField] private bool monitorBlackboardsAfterFirstUse = true;
    [SerializeField] private Material blackboardFadeMaterial;
    [SerializeField, Min(0.01f)] private float blackboardFadeDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugBlackboardLogs = true;

    [Header("First Use Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] firstUseDialogueLines;
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioSource selmaVoiceSource;

    private bool firstUseCompleted;
    private Coroutine dialogueCoroutine;
    private Coroutine roomBlackboardFadeCoroutine;
    private Coroutine otherRoomsBlackboardFadeCoroutine;
    private bool blackboardMonitoringActive;
    private int lastActivePlayerIndex = -1;

    private void Awake()
    {
        GetComponent<Interactable>()?.SetInteractionType(InteractionType.Int_lv1_HidenPassageButton);
    }

    private void Start()
    {
        if (secretDoorAnimator != null && HasAnimatorBool(openedBoolName))
            isDoorOpen = secretDoorAnimator.GetBool(openedBoolName);

        LogBlackboard($"Start. RoomVolume={NameOf(roomVolume)}, RoomBlackboard={NameOf(roomBlackboard)}, OtherRoomsBlackboard={NameOf(otherRoomsBlackboard)}, FadeMaterial={NameOf(blackboardFadeMaterial)}.");
        StartCoroutine(InitializeBlackboards());
    }

    private void Update()
    {
        if (!blackboardMonitoringActive || SwitchCharacter.Instance == null)
            return;

        int activePlayerIndex = SwitchCharacter.Instance.activePlayerIndex;
        if (activePlayerIndex == lastActivePlayerIndex)
            return;

        lastActivePlayerIndex = activePlayerIndex;
        LogBlackboard($"Detected character switch. ActivePlayerIndex={activePlayerIndex}.");
        UpdateBlackboardsForActiveCharacter();
    }

    public void PerformInteraction(PlayerController player)
    {
        PlayButtonPressAnimation();
        ToggleSecretDoor();
        blackboardMonitoringActive = monitorBlackboardsAfterFirstUse && !isDoorOpen;
        lastActivePlayerIndex = SwitchCharacter.Instance != null
            ? SwitchCharacter.Instance.activePlayerIndex
            : -1;
        LogBlackboard($"Button used by {NameOf(player)}. DoorOpen={isDoorOpen}, ActivePlayerIndex={lastActivePlayerIndex}, Monitoring={blackboardMonitoringActive}.");

        if (isDoorOpen)
            HideAllBlackboards();
        else
            UpdateBlackboardsForActiveCharacter(player);

        if (!firstUseCompleted)
        {
            firstUseCompleted = true;
            if (dialogueCoroutine != null)
                StopCoroutine(dialogueCoroutine);

            dialogueCoroutine = StartCoroutine(PlayDialogueLines(firstUseDialogueLines));
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private void ToggleSecretDoor()
    {
        if (secretDoorAnimator == null)
        {
            Debug.LogWarning($"{name}: Secret Door Animator is not assigned.", this);
            return;
        }

        isDoorOpen = !isDoorOpen;
        if (HasAnimatorBool(openedBoolName))
            secretDoorAnimator.SetBool(openedBoolName, isDoorOpen);
        else
            Debug.LogWarning($"{name}: Animator has no bool parameter named '{openedBoolName}'.", this);

        string triggerName = isDoorOpen ? openTriggerName : closeTriggerName;
        if (HasAnimatorTrigger(triggerName))
            secretDoorAnimator.SetTrigger(triggerName);
        else
            Debug.LogWarning($"{name}: Animator has no trigger parameter named '{triggerName}'.", this);
    }

    private void PlayButtonPressAnimation()
    {
        if (buttonAnimator == null || string.IsNullOrWhiteSpace(pressTriggerName))
            return;

        buttonAnimator.SetTrigger(pressTriggerName);
    }

    private bool HasAnimatorBool(string parameterName)
    {
        if (secretDoorAnimator == null || secretDoorAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in secretDoorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private bool HasAnimatorTrigger(string parameterName)
    {
        if (secretDoorAnimator == null || secretDoorAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in secretDoorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private void UpdateBlackboardsForActiveCharacter(PlayerController knownActivePlayer = null)
    {
        if (roomVolume == null)
        {
            LogBlackboard("Cannot update blackboards: Room Volume is not assigned.");
            return;
        }

        PlayerController activePlayer = knownActivePlayer != null
            ? knownActivePlayer
            : GetActivePlayerController();
        if (activePlayer == null)
        {
            LogBlackboard("Cannot update blackboards: active PlayerController was not found.");
            return;
        }

        bool isInsideButtonRoom = IsInsideVolume(roomVolume, activePlayer.transform.position);
        LogBlackboard($"Evaluating {activePlayer.name} at {activePlayer.transform.position}. Inside Room Volume={isInsideButtonRoom}. RoomBlackboard -> {!isInsideButtonRoom}, OtherRoomsBlackboard -> {isInsideButtonRoom}.");

        // The active character's side remains visible; the opposite side receives its blackboard.
        FadeBlackboard(ref roomBlackboardFadeCoroutine, roomBlackboard, !isInsideButtonRoom);
        FadeBlackboard(ref otherRoomsBlackboardFadeCoroutine, otherRoomsBlackboard, isInsideButtonRoom);
    }

    private void HideAllBlackboards()
    {
        LogBlackboard("Door is open. Hiding both blackboards and stopping room-side monitoring.");
        FadeBlackboard(ref roomBlackboardFadeCoroutine, roomBlackboard, false);
        FadeBlackboard(ref otherRoomsBlackboardFadeCoroutine, otherRoomsBlackboard, false);
    }

    private IEnumerator InitializeBlackboards()
    {
        // SwitchCharacter is initialized by another scene object, so wait one frame before reading it.
        yield return null;

        if (isDoorOpen)
        {
            blackboardMonitoringActive = false;
            HideAllBlackboards();
            yield break;
        }

        blackboardMonitoringActive = monitorBlackboardsAfterFirstUse;
        lastActivePlayerIndex = SwitchCharacter.Instance != null
            ? SwitchCharacter.Instance.activePlayerIndex
            : -1;
        UpdateBlackboardsForActiveCharacter();
    }

    private void FadeBlackboard(ref Coroutine fadeCoroutine, GameObject blackboard, bool shouldBeVisible)
    {
        if (blackboard == null)
        {
            LogBlackboard("Skipped blackboard fade: target is not assigned.");
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        LogBlackboard($"Fade request: {blackboard.name} -> visible={shouldBeVisible}, currentlyActive={blackboard.activeSelf}.");
        fadeCoroutine = StartCoroutine(FadeBlackboardRoutine(blackboard, shouldBeVisible));
    }

    private IEnumerator FadeBlackboardRoutine(GameObject blackboard, bool shouldBeVisible)
    {
        bool wasActive = blackboard.activeSelf;
        if (shouldBeVisible && !wasActive)
            blackboard.SetActive(true);

        Renderer[] renderers = blackboard.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            LogBlackboard($"{blackboard.name} has no Renderer. Using immediate SetActive({shouldBeVisible}).");
            blackboard.SetActive(shouldBeVisible);
            yield break;
        }

        if (blackboardFadeMaterial != null)
        {
            foreach (Renderer renderer in renderers)
                renderer.material = blackboardFadeMaterial;
        }

        float startAlpha = shouldBeVisible && !wasActive ? 0f : GetBlackboardAlpha(renderers[0]);
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        SetBlackboardAlpha(renderers, startAlpha);

        float elapsed = 0f;
        while (elapsed < blackboardFadeDuration)
        {
            elapsed += Time.deltaTime;
            SetBlackboardAlpha(renderers, Mathf.Lerp(startAlpha, targetAlpha, elapsed / blackboardFadeDuration));
            yield return null;
        }

        SetBlackboardAlpha(renderers, targetAlpha);
        if (!shouldBeVisible)
            blackboard.SetActive(false);
    }

    private static float GetBlackboardAlpha(Renderer renderer)
    {
        if (renderer == null)
            return 1f;

        Material material = renderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        return material.HasProperty(colorProperty) ? material.GetColor(colorProperty).a : 1f;
    }

    private static void SetBlackboardAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material material = renderer.material;
            string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (!material.HasProperty(colorProperty))
                continue;

            Color color = material.GetColor(colorProperty);
            color.a = alpha;
            material.SetColor(colorProperty, color);
        }
    }

    private static bool IsInsideVolume(Collider volume, Vector3 position)
    {
        Vector3 closestPoint = volume.ClosestPoint(position);
        return (closestPoint - position).sqrMagnitude < 0.0001f;
    }

    private static PlayerController GetActivePlayerController()
    {
        SwitchCharacter switcher = SwitchCharacter.Instance;
        if (switcher == null || switcher.players == null ||
            switcher.activePlayerIndex < 0 || switcher.activePlayerIndex >= switcher.players.Length)
        {
            return null;
        }

        PlayerInput activeInput = switcher.players[switcher.activePlayerIndex];
        return activeInput != null ? activeInput.GetComponent<PlayerController>() : null;
    }

    private void LogBlackboard(string message)
    {
        if (debugBlackboardLogs)
            Debug.Log($"[{name} Blackboard] {message}", this);
    }

    private static string NameOf(Object target)
    {
        return target != null ? target.name : "NULL";
    }

    private IEnumerator PlayDialogueLines(Lvl3DialogueLine[] lines)
    {
        if (lines == null)
        {
            dialogueCoroutine = null;
            yield break;
        }

        foreach (Lvl3DialogueLine line in lines)
        {
            string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
            string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
            string selmaText = line.speaker == Lvl3DialogueSpeaker.Selma ? line.text : string.Empty;

            PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);
            PlayerTopText.Instance?.ShowSelmaTopTextPersistent(selmaText);
            PlayVoice(line);

            float duration = line.duration > 0f
                ? line.duration
                : PlayerTopText.Instance != null ? PlayerTopText.Instance.textTime : 3f;
            yield return new WaitForSeconds(duration);

            PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
            PlayerTopText.Instance?.ClearSelmaTopTextIfMatches(selmaText);
        }

        dialogueCoroutine = null;
    }

    private void PlayVoice(Lvl3DialogueLine line)
    {
        if (line.voiceClip == null)
            return;

        AudioSource source = line.speaker switch
        {
            Lvl3DialogueSpeaker.Sherlock => sherlockVoiceSource,
            Lvl3DialogueSpeaker.Watson => watsonVoiceSource,
            Lvl3DialogueSpeaker.Selma => selmaVoiceSource,
            _ => null
        };

        if (source == null)
            return;

        source.Stop();
        source.PlayOneShot(line.voiceClip);
    }
}
