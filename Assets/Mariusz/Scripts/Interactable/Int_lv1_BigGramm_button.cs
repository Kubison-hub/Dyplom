using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_BigGramm_button : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Int_lv1_BigGramm bigGramm;

    [Header("Button Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressTriggerName = "Press";

    [Header("Next Interactions")]
    [SerializeField] private GameObject[] nextInteractions;

    [Header("Completion Cutscene")]
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField] private VideoPlayer cutsceneVideoPlayer;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private bool hideCutsceneRootOnStart = true;
    [SerializeField, Min(0f)] private float cutsceneStartDelay = 0.15f;
    [SerializeField] private float cutsceneHorizontalAxis = -160f;

    [Header("Post-Cutscene NPC Placement")]
    [SerializeField] private Transform sherlockDestination;
    [SerializeField] private Transform watsonDestination;
    [SerializeField] private Transform violetDestination;
    [SerializeField] private Transform selmaDestination;

    [SerializeField] private NavMeshAgent sherlockAgent;
    [SerializeField] private NavMeshAgent watsonAgent;
    [SerializeField] private NavMeshAgent violetAgent;
    [SerializeField] private NavMeshAgent selmaAgent;

    [Header("Narration")]
    [SerializeField, TextArea] private string missingSetupText = "Czegos tu brakuje.";

    private Interactable interactable;
    private Collider interactionCollider;
    private bool completed;
    private bool cutscenePlaying;
    private PlayerController cutscenePlayer;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        interactable.SetInteractionType(InteractionType.Int_lv1_BigGramm_button);

        if (cutsceneVideoPlayer != null)
        {
            cutsceneVideoPlayer.playOnAwake = false;
            cutsceneVideoPlayer.loopPointReached += HandleCutsceneFinished;
        }

        if (hideCutsceneRootOnStart && cutsceneRoot != null)
            cutsceneRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (cutsceneVideoPlayer != null)
            cutsceneVideoPlayer.loopPointReached -= HandleCutsceneFinished;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed)
            return;

        if (bigGramm == null || !bigGramm.IsReady)
        {
            if (PlayerTopText.Instance != null)
                PlayerTopText.Instance.ShowTopText(missingSetupText, "");

            if (player != null)
                player.currentInteractable = null;

            return;
        }

        if (audioSource != null && buttonClickClip != null)
            audioSource.PlayOneShot(buttonClickClip);

        if (buttonAnimator != null && !string.IsNullOrWhiteSpace(pressTriggerName))
            buttonAnimator.SetTrigger(pressTriggerName);

        completed = true;
        Debug.Log("KONIEC");
        DisableInteraction();
        ActivateNextInteractions();
        PlayCompletionCutscene(player);

        if (player != null)
            player.currentInteractable = null;
    }

    private void DisableInteraction()
    {
        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }

    private void ActivateNextInteractions()
    {
        if (nextInteractions == null)
            return;

        foreach (GameObject nextInteraction in nextInteractions)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }
    }

    private void PlayCompletionCutscene(PlayerController player)
    {
        if (cutsceneVideoPlayer == null || cutscenePlaying)
            return;

        cutscenePlaying = true;
        cutscenePlayer = player;

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(true);

        if (cutscenePlayer != null)
            cutscenePlayer.SetTutorialInputLocked(true);

        cutsceneVideoPlayer.Stop();
        StartCoroutine(BeginCutsceneAfterCoverIsVisible(player));
    }

    private IEnumerator BeginCutsceneAfterCoverIsVisible(PlayerController player)
    {
        yield return new WaitForEndOfFrame();

        if (cutsceneStartDelay > 0f)
            yield return new WaitForSecondsRealtime(cutsceneStartDelay);

        if (!cutscenePlaying)
            yield break;

        MoveNpcsForNextScene(player);
        cameraController?.SetZoomState(CameraZoomState.Wide);
        cameraController?.SetHorizontalRotation(cutsceneHorizontalAxis);
        cameraController?.LockCurrentHorizontalRotation();
        cutsceneVideoPlayer.Play();
    }

    private void MoveNpcsForNextScene(PlayerController interactingPlayer)
    {
        Transform sherlockTransform = sherlockAgent != null
            ? sherlockAgent.transform
            : SwitchCharacter.Instance != null
                ? SwitchCharacter.Instance.sherlockTransform
                : interactingPlayer != null && interactingPlayer.playerCharacter == PlayerCharacter.Sherlock
                    ? interactingPlayer.transform
                    : null;

        if (sherlockTransform != null && sherlockDestination != null)
            PlaceNpcOnNavMesh(sherlockAgent, sherlockDestination);

        PlaceNpcOnNavMesh(watsonAgent, watsonDestination);
        PlaceNpcOnNavMesh(violetAgent, violetDestination);
        PlaceNpcOnNavMesh(selmaAgent, selmaDestination);
    }

    private void PlaceNpcOnNavMesh(NavMeshAgent agent, Transform destination)
    {
        if (agent == null || destination == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: NPC agent or destination marker is missing, or the agent is outside the NavMesh.", this);
            return;
        }

        if (!NavMesh.SamplePosition(destination.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Destination for {agent.name} is not on the NavMesh.", this);
            return;
        }

        agent.isStopped = false;
        agent.ResetPath();
        agent.Warp(hit.position);
        agent.transform.rotation = destination.rotation;
    }

    private void HandleCutsceneFinished(VideoPlayer source)
    {
        if (!cutscenePlaying)
            return;

        cutscenePlaying = false;
        source.Stop();

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(false);

        cameraController?.UnlockHorizontalRotation();
        cameraController?.SetZoomState(CameraZoomState.Wide);
        cameraController?.SetHorizontalRotation(cutsceneHorizontalAxis);

        if (cutscenePlayer != null)
        {
            cutscenePlayer.SetTutorialInputLocked(false);
            cutscenePlayer = null;
        }
    }
}
