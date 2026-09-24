using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Video;

[RequireComponent(typeof(Interactable))]
public class Int_WatsonSwitchTutorial : MonoBehaviour
{
    private static bool tutorialCompleted;
    private static bool tutorialInProgress;

    [Header("Tutorial Trigger")]
    [SerializeField] private bool startsSwitchTutorial = true;
    [SerializeField] private bool representsWatson = true;
    [SerializeField, Min(0)] private int sherlockPlayerIndex = 0;
    [SerializeField, Min(0)] private int watsonPlayerIndex = 1;
    [SerializeField] private GameObject questionMarkToHide;
    [SerializeField] private GameObject deactivateOnTutorialComplete;
    [SerializeField] private Collider tutorialCollider;

    [Header("Watson Escort Unlock")]
    [SerializeField] private WatsonEscortNPC watsonEscortNpcToActivate;

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField, Min(0f)] private float cameraPresetTransitionDuration = 2.5f;

    [Header("Immediate Watson Reaction")]
    [SerializeField, Min(1f)] private float watsonTurnSpeed = 360f;
    [SerializeField, Range(0f, 1f)] private float watsonTurnStartProgress = 0.5f;

    [Header("Sherlock Approach")]
    [SerializeField, Min(0.1f)] private float sherlockApproachDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float sherlockApproachNavMeshSampleRadius = 1.2f;

    [Header("Opening Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] openingDialogueLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Watsonie, czas spojrzeć na tę sprawę z innej perspektywy.",
            duration = 3f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "W rzeczy samej, Sherlocku. Jestem do usług.",
            duration = 3f
        }
    };

    [Header("Tutorial Panel")]
    [Tooltip("Unique ID used by TutorialManager so this panel is only shown once.")]
    [SerializeField] private string tutorialPanelId = "WatsonSwitchTutorial";
    [SerializeField, TextArea] private string tutorialText =
        "W grze możesz sterować także Watsonem. Wciśnij spację, aby przełączać się pomiędzy postaciami.";

    [Header("After Popup Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] afterPopupDialogueLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Spróbuj wyciągnąć jakieś informacje, Watsonie.",
            duration = 3f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "W rzeczy samej, Sherlock.",
            duration = 3f
        }
    };
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;

    [Header("After Switching Popup")]
    [SerializeField] private string afterSwitchTutorialTitle = "Watson";
    [SerializeField, TextArea] private string afterSwitchTutorialText =
        "Watson może rozmawiać ze świadkami i pomagać Sherlockowi w śledztwie.";
    [SerializeField] private VideoClip afterSwitchTutorialVideoClip;

    [Header("Repeat Lines")]
    [SerializeField, TextArea] private string watsonRepeatText = "Sherlock?";
    [SerializeField, TextArea] private string sherlockRepeatText = "Watson?";

    private Interactable interactable;
    private Coroutine watsonTurnCoroutine;
    private Coroutine delayedWatsonTurnCoroutine;
    private bool watsonTurnPending;

    private void OnEnable()
    {
        if (watsonEscortNpcToActivate != null)
            watsonEscortNpcToActivate.enabled = true;
    }

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_WatsonSwitchTutorial);

        if (tutorialCollider == null)
            tutorialCollider = GetComponent<Collider>();

        if (deactivateOnTutorialComplete == null)
            deactivateOnTutorialComplete = gameObject;

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (tutorialCompleted)
        {
            ShowRepeatLine();
            ClearCurrentInteraction(player);
            return;
        }

        if (!startsSwitchTutorial || tutorialInProgress)
        {
            ClearCurrentInteraction(player);
            return;
        }

        questionMarkToHide?.SetActive(false);
        StartCoroutine(RunTutorial(player));
    }

    public void NotifyInteractionSelected(PlayerController player)
    {
        if (tutorialCompleted || tutorialInProgress || player == null ||
            player.playerCharacter != PlayerCharacter.Sherlock)
        {
            return;
        }

        PlayerController watson = FindWatson();
        if (watson == null)
            return;

        watsonTurnPending = true;
    }

    public bool RequiresWatsonReactionBeforeApproach(PlayerController player)
    {
        return !tutorialCompleted &&
               !tutorialInProgress &&
               player != null &&
               player.playerCharacter == PlayerCharacter.Sherlock &&
               watsonTurnPending;
    }

    public void MovePlayerAfterWatsonReaction(PlayerController player)
    {
        if (player == null)
            return;

        PlayerController watson = FindWatson();
        float initialDistanceToWatson = GetHorizontalDistance(player, watson);
        float finalDistanceToWatson = sherlockApproachDistance;

        if (TryGetSherlockApproachPoint(player, out Vector3 approachPoint))
        {
            player.currentInteractionPoint = null;
            player.SetAutoInteractionApproachPoint(approachPoint);
            finalDistanceToWatson = GetHorizontalDistance(approachPoint, watson.transform.position);
        }

        player.SetWaitingForInteractionReaction(false);
        player.MoveToInteractable();

        if (watsonTurnPending && watson != null)
        {
            watsonTurnPending = false;

            if (delayedWatsonTurnCoroutine != null)
                StopCoroutine(delayedWatsonTurnCoroutine);

            float turnDistance = Mathf.Lerp(
                initialDistanceToWatson,
                finalDistanceToWatson,
                watsonTurnStartProgress);
            delayedWatsonTurnCoroutine = StartCoroutine(
                RotateWatsonWhenSherlockReachesDistance(player, watson, turnDistance));
        }
    }

    private IEnumerator RunTutorial(PlayerController player)
    {
        tutorialInProgress = true;
        watsonTurnPending = false;
        ClearCurrentInteraction(player);

        yield return FinishWatsonFacingPlayer(player);

        if (cameraController != null)
            cameraController.SetZoomInOneStep(cameraPresetTransitionDuration);

        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null)
            switchCharacter.canSwitch = false;

        if (tutorialCollider != null)
            tutorialCollider.enabled = false;

        yield return PlayDialogueLines(openingDialogueLines);

        TutorialManager tutorialManager = TutorialManager.Instance;
        if (tutorialManager != null)
        {
            tutorialManager.PokazTutorial(tutorialText, tutorialPanelId);
            yield return null;

            while (tutorialManager.BlocksWorldInput)
                yield return null;
        }

        if (switchCharacter == null)
        {
            Debug.LogWarning($"{name}: SwitchCharacter is missing; Watson tutorial cannot enable switching.", this);
            tutorialInProgress = false;
            yield break;
        }

        switchCharacter.canSwitch = true;

        while (switchCharacter.activePlayerIndex != watsonPlayerIndex)
            yield return null;

        yield return PlayDialogueLines(afterPopupDialogueLines);

        TutorialTimeline timeline = TutorialTimeline.Instance;
        if (timeline != null)
        {
            timeline.ShowGameplayTutorialPopup(
                afterSwitchTutorialTitle,
                afterSwitchTutorialText,
                afterSwitchTutorialVideoClip);
            yield return null;

            while (timeline.BlocksWorldInput)
                yield return null;
        }

        tutorialCompleted = true;
        GetComponent<Interactable>()?.MarkCompleted();
        tutorialInProgress = false;
        deactivateOnTutorialComplete?.SetActive(false);
    }

    private void ShowRepeatLine()
    {
        if (PlayerTopText.Instance == null || SwitchCharacter.Instance == null)
            return;

        if (representsWatson && SwitchCharacter.Instance.activePlayerIndex == sherlockPlayerIndex)
            PlayerTopText.Instance.ShowTopText("", watsonRepeatText);
        else if (!representsWatson && SwitchCharacter.Instance.activePlayerIndex == watsonPlayerIndex)
            PlayerTopText.Instance.ShowTopText(sherlockRepeatText, "");
    }

    private IEnumerator PlayDialogueLines(Lvl3DialogueLine[] dialogueLines)
    {
        if (dialogueLines == null)
            yield break;

        foreach (Lvl3DialogueLine line in dialogueLines)
        {
            string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
            string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
            PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);

            AudioSource voiceSource = line.speaker == Lvl3DialogueSpeaker.Sherlock
                ? sherlockVoiceSource
                : watsonVoiceSource;
            if (voiceSource != null && line.voiceClip != null)
            {
                voiceSource.Stop();
                voiceSource.PlayOneShot(line.voiceClip);
            }

            yield return new WaitForSeconds(line.duration > 0f ? line.duration : 3f);
            PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
        }
    }

    private static void ClearCurrentInteraction(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;
    }

    private IEnumerator RotateWatsonToward(Transform sherlock, PlayerController watson)
    {
        if (sherlock == null || watson == null)
            yield break;

        bool restoreAgentRotation = watson.navMeshAgent != null && watson.navMeshAgent.updateRotation;
        if (watson.navMeshAgent != null)
            watson.navMeshAgent.updateRotation = false;

        while (sherlock != null && watson != null)
        {
            Vector3 direction = sherlock.position - watson.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                break;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            watson.transform.rotation = Quaternion.RotateTowards(
                watson.transform.rotation,
                targetRotation,
                watsonTurnSpeed * Time.deltaTime);

            if (Quaternion.Angle(watson.transform.rotation, targetRotation) <= 0.5f)
                break;

            yield return null;
        }

        if (watson != null && watson.navMeshAgent != null)
            watson.navMeshAgent.updateRotation = restoreAgentRotation;

        watsonTurnCoroutine = null;
    }

    private bool TryGetSherlockApproachPoint(PlayerController sherlock, out Vector3 approachPoint)
    {
        approachPoint = default;

        PlayerController watson = FindWatson();
        if (sherlock == null || watson == null || sherlock.navMeshAgent == null)
            return false;

        Vector3 directionFromWatson = sherlock.transform.position - watson.transform.position;
        directionFromWatson.y = 0f;
        if (directionFromWatson.sqrMagnitude <= 0.001f)
            directionFromWatson = -watson.transform.forward;

        Vector3 desiredPoint = watson.transform.position +
                               directionFromWatson.normalized * sherlockApproachDistance;

        if (!NavMesh.SamplePosition(
                desiredPoint,
                out NavMeshHit navMeshHit,
                sherlockApproachNavMeshSampleRadius,
                NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMeshWallGuard.TryGetClearPath(
                sherlock.navMeshAgent,
                navMeshHit.position,
                out NavMeshPath path))
        {
            return false;
        }

        approachPoint = navMeshHit.position;
        return true;
    }

    private IEnumerator FinishWatsonFacingPlayer(PlayerController sherlock)
    {
        while (delayedWatsonTurnCoroutine != null)
            yield return null;

        while (watsonTurnCoroutine != null)
            yield return null;

        PlayerController watson = FindWatson();
        if (sherlock == null || watson == null)
            yield break;

        watsonTurnCoroutine = StartCoroutine(RotateWatsonToward(sherlock.transform, watson));
        while (watsonTurnCoroutine != null)
            yield return null;
    }

    private IEnumerator RotateWatsonWhenSherlockReachesDistance(
        PlayerController sherlock,
        PlayerController watson,
        float turnDistance)
    {
        while (sherlock != null && watson != null &&
               GetHorizontalDistance(sherlock, watson) > turnDistance)
        {
            if (sherlock.currentInteractable != interactable)
                break;

            yield return null;
        }

        if (sherlock != null && watson != null &&
            sherlock.currentInteractable == interactable)
        {
            watsonTurnCoroutine = StartCoroutine(RotateWatsonToward(sherlock.transform, watson));
            while (watsonTurnCoroutine != null)
                yield return null;
        }

        delayedWatsonTurnCoroutine = null;
    }

    private static float GetHorizontalDistance(PlayerController first, PlayerController second)
    {
        return first != null && second != null
            ? GetHorizontalDistance(first.transform.position, second.transform.position)
            : 0f;
    }

    private static float GetHorizontalDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;
        return Vector3.Distance(first, second);
    }

    private static PlayerController FindWatson()
    {
        foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.playerCharacter == PlayerCharacter.Watson)
                return candidate;
        }

        return null;
    }
}
