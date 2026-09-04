using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves Sherlock and Watson through an opened hatch onto basement NavMesh markers.
/// </summary>
public class Int_lv4_HatchToBassement : MonoBehaviour
{
    [Header("Basement Start Positions")]
    [SerializeField] private Transform sherlockBasementStartPoz;
    [SerializeField] private Transform watsonBasementStartPoz;
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;
    [SerializeField] private float transitionDelay = 0.1f;
    [SerializeField, Min(0.05f)] private float rotationLockDuration = 0.5f;
    [SerializeField] private Interactable interactable;

    [Header("Basement Exposure")]
    [SerializeField] private BasementExposureController basementExposureController;

    [Header("Basement Arrival Music")]
    [SerializeField] private GameMusicManager gameMusicManager;
    [SerializeField] private AudioClip basementArrivalMusic;
    [Tooltip("When disabled, the playlist resumes after this track finishes.")]
    [SerializeField] private bool loopBasementArrivalMusic;

    [Header("Level Transition")]
    [Tooltip("Root GameObject of Level 3, enabled before the characters are teleported into the basement.")]
    [SerializeField] private GameObject levelThreeRoot;
    [Tooltip("Root GameObject of Level 2, disabled after the characters reach Level 3.")]
    [SerializeField] private GameObject levelTwoRoot;

    private bool isTransitioning;
    private bool basementArrivalMusicPlayed;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        StartCoroutine(MoveCharactersToBasement(player));
    }

    private IEnumerator MoveCharactersToBasement(PlayerController interactingPlayer)
    {
        // PlayerController finishes the interaction on this frame. Clear its old
        // target on the following frame before the character changes location.
        yield return new WaitForEndOfFrame();

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController candidate in players)
            candidate?.CancelInteractionForTeleport();

        if (levelThreeRoot != null)
            levelThreeRoot.SetActive(true);

        if (transitionDelay > 0f)
            yield return new WaitForSeconds(transitionDelay);

        TeleportPlayer(PlayerCharacter.Sherlock, sherlockBasementStartPoz);
        TeleportPlayer(PlayerCharacter.Watson, watsonBasementStartPoz);

        foreach (PlayerController candidate in players)
            candidate?.ResumeAfterTeleport();

        Transform activeMarker = interactingPlayer != null &&
                                 interactingPlayer.playerCharacter == PlayerCharacter.Watson
            ? watsonBasementStartPoz
            : sherlockBasementStartPoz;
        if (interactingPlayer != null && activeMarker != null)
            interactingPlayer.LockRotationAfterTeleport(activeMarker.rotation, rotationLockDuration);

        if (basementExposureController == null)
            basementExposureController = FindFirstObjectByType<BasementExposureController>();

        basementExposureController?.EnterBasement();
        PlayBasementArrivalMusic();
        CluesLog.Instance?.AddFindEthelBasementObjective();
        CluesLog.Instance?.RemoveEthelPassageObjective();
        CluesLog.Instance?.BeginBasementEvidenceObjective();

        if (levelTwoRoot != null)
            levelTwoRoot.SetActive(false);

        isTransitioning = false;
    }

    private void PlayBasementArrivalMusic()
    {
        if (basementArrivalMusicPlayed || basementArrivalMusic == null)
            return;

        if (gameMusicManager == null)
            gameMusicManager = FindFirstObjectByType<GameMusicManager>();

        if (gameMusicManager == null)
            return;

        basementArrivalMusicPlayed = true;
        gameMusicManager.ChangeMusic(basementArrivalMusic, loopBasementArrivalMusic);
    }

    private void TeleportPlayer(PlayerCharacter character, Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning($"{name}: missing basement start position for {character}.", this);
            return;
        }

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController candidate in players)
        {
            if (candidate == null || candidate.playerCharacter != character)
                continue;

            Vector3 targetPosition = destination.position;
            if (NavMesh.SamplePosition(destination.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                targetPosition = hit.position;
            else
                Debug.LogWarning($"{name}: {character} basement marker is outside NavMesh.", destination);

            NavMeshAgent agent = candidate.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.updateRotation = false;
                agent.velocity = Vector3.zero;
                agent.Warp(targetPosition);
                agent.ResetPath();
                agent.nextPosition = targetPosition;
            }
            else
            {
                candidate.transform.position = targetPosition;
            }

            candidate.transform.rotation = destination.rotation;
            return;
        }

        Debug.LogWarning($"{name}: could not find {character} PlayerController.", this);
    }
}
