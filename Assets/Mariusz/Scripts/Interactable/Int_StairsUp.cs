using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Int_StairsUp : MonoBehaviour
{

    [Header("Debug Bypass")]
    [Tooltip("When enabled, Sherlock can use the stairs immediately, ignoring the story gate and Selma validation.")]
    public bool canGoUpStairs = false;
    public bool performed = false;

    public GameObject SherlockGO;

    public Transform moveDestination;

    private Interactable interactable;

    public Transform level2StartingPoint;
    public GameObject level_1;
    public GameObject level_2;
    [Header("Stairs Completion")]
    [SerializeField] private Int_lv1_SherlockWatsonSelmaDialog sherlockWatsonSelmaDialogToDisable;

    [Header("Level 2 Arrival")]
    [SerializeField] private PlayerController watsonPlayer;
    [SerializeField] private Transform watsonLevel2StartingPoint;
    [SerializeField] private GameObject blackboardToEnableOnArrival;
    [SerializeField] private GameObject[] gameObjectsToEnableOnArrival;

    [Header("Level 2 Arrival Music")]
    [SerializeField] private GameMusicManager gameMusicManager;
    [SerializeField] private AudioClip level2ArrivalMusic;
    [Tooltip("When disabled, the playlist resumes after this track finishes.")]
    [SerializeField] private bool loopLevel2ArrivalMusic;

    [Header("Availability")]
    [Tooltip("Enables the Selma monitoring and the full stairs interaction flow.")]
    public bool isSherlockWantToGoUpstairs = false;
    [SerializeField] private Lvl3DialogueLine stairsUnavailableDialogueLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Nie mam jeszcze powodu, by iść na górę.",
        duration = 3f
    };
    [Tooltip("Activated when Selma notices Sherlock at the stairs for the first time.")]
    [FormerlySerializedAs("activateOnFirstUnavailableStairsAttempt")]
    [SerializeField] private GameObject activateOnFirstStairsDiscovery;

    [Header("Selma Stairs Gate")]
    [Tooltip("Assign Selma's object with WatsonEscortNPC and NavMeshAgent.")]
    [SerializeField] private Transform selmaPositionTarget;
    [SerializeField] private Transform selmaEyePoint;
    [Tooltip("The physical area in which Selma directly blocks the stairs.")]
    [SerializeField] private Collider stairsRoomVolume;
    [Tooltip("Optional area Selma can watch from outside the room. Falls back to Stairs Room Volume.")]
    [SerializeField] private Collider stairsObservationVolume;
    [Tooltip("Area beside the stairs where a visible Sherlock is finally stopped. Falls back to Stairs Room Volume when empty.")]
    [SerializeField] private Collider sherlockStairsStopVolume;
    [Tooltip("When configured, moving Selma away is enough to open the stairs without the older canGoUpStairs quest flag.")]
    [SerializeField] private bool allowStairsWhenSelmaClear = true;
    [SerializeField, Min(0.1f)] private float selmaVisionRange = 10f;
    [SerializeField, Range(1f, 360f)] private float selmaFieldOfView = 100f;
    [SerializeField] private LayerMask selmaVisionObstructionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction selmaVisionTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private Lvl3DialogueLine[] selmaBlocksStairsDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Muszę przekraść się jakoś na górę.",
            duration = 3f
        }
    };
    [Header("First Discovery Sequence")]
    [Tooltip("Plays only when Selma notices Sherlock while he is already walking to the stairs.")]
    [SerializeField] private Lvl3DialogueLine[] firstDiscoveryDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] firstDiscoveryResolutionDialogueLines;
    [Tooltip("Plays after the first-discovery camera reaches its Wide preset.")]
    [SerializeField] private Lvl3DialogueLine[] afterDiscoveryCameraDialogueLines;
    [Tooltip("One line is picked at random when Selma catches Sherlock walking to the stairs again.")]
    [SerializeField] private Lvl3DialogueLine[] repetitiveDiscoveryDialogueLines;
    [SerializeField, Min(1f)] private float discoveryRotationSpeed = 220f;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private string discoveryCameraPreset = "Wide";
    [SerializeField, Min(0.01f)] private float discoveryCameraTransitionSpeed = 1f;
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioSource selmaVoiceSource;

    [Header("Debug")]
    [SerializeField] private bool debugIsSelmaInStairsRoom;
    [SerializeField] private bool debugIsStairsObserved;
    [SerializeField] private bool debugSelmaGateRedirected;
    [SerializeField] private bool firstDiscovery;
    [SerializeField] private bool debugSherlockApproachingStairs;
    [SerializeField] private bool debugSherlockInStairsStopVolume;

    private Coroutine selmaGateDialogueCoroutine;
    private PlayerController sherlockPlayer;
    private bool isFirstDiscoverySequencePlaying;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        SherlockGO = GameObject.Find("Sherlock");
        sherlockPlayer = SherlockGO != null
            ? SherlockGO.GetComponent<PlayerController>() ?? SherlockGO.GetComponentInChildren<PlayerController>()
            : null;
        if (watsonPlayer == null && SwitchCharacter.Instance != null && SwitchCharacter.Instance.watsonTransform != null)
            watsonPlayer = SwitchCharacter.Instance.watsonTransform.GetComponent<PlayerController>();

        RefreshSelmaStairsAwareness();
    }
    private void Update()
    {
        if (canGoUpStairs)
        {
            if (interactable != null)
                interactable.interactabePoint = moveDestination;

            return;
        }

        if (!isSherlockWantToGoUpstairs)
        {
            if (interactable != null && SherlockGO != null)
                interactable.interactabePoint = SherlockGO.transform;

            return;
        }

        MonitorSherlockApproachingStairs();

        if (CanUseStairs())
        {
            interactable.interactabePoint = moveDestination;
        }
        else if (SherlockGO != null)
        {
            interactable.interactabePoint = SherlockGO.transform;
        }
    }

    public bool RedirectWhenSelmaGuardsStairs(PlayerController player)
    {
        if (canGoUpStairs && player != null && player.playerCharacter == PlayerCharacter.Sherlock)
            return false;

        if (!isSherlockWantToGoUpstairs)
        {
            if (player != null)
            {
                player.currentInteractable = null;
                player.navMeshAgent?.ResetPath();
            }

            debugSherlockApproachingStairs = false;
            PlayStairsUnavailableDialogue();
            return true;
        }

        RefreshSelmaStairsAwareness();
        debugSherlockInStairsStopVolume = IsSherlockInStairsStopVolume(player);
        debugSelmaGateRedirected = (debugIsSelmaInStairsRoom || debugIsStairsObserved) &&
                                   debugSherlockInStairsStopVolume;

        if (!debugSelmaGateRedirected)
        {
            if (interactable != null)
                interactable.interactabePoint = CanUseStairs() ? moveDestination : SherlockGO != null ? SherlockGO.transform : null;

            return false;
        }

        if (isFirstDiscoverySequencePlaying)
            return true;

        if (player != null)
        {
            player.currentInteractable = null;
            player.navMeshAgent?.ResetPath();
        }
        debugSherlockApproachingStairs = false;

        if (selmaGateDialogueCoroutine != null)
            StopCoroutine(selmaGateDialogueCoroutine);

        selmaGateDialogueCoroutine = StartCoroutine(PlaySelmaGateDialogue());
        return true;
    }

    public void PerformInteraction(PlayerController player)
    {

        debugSherlockApproachingStairs = false;

        if (canGoUpStairs && player != null && player.playerCharacter == PlayerCharacter.Sherlock)
        {
            performed = true;
            player.currentInteractable = null;
            WatsonEscortController.Instance?.ForceFarewell();
            StartCoroutine(GoUpStairs(player));
            return;
        }

        if (!isSherlockWantToGoUpstairs)
        {
            PlayStairsUnavailableDialogue();
            return;
        }

        if (RedirectWhenSelmaGuardsStairs(player))
            return;

        if (CanUseStairs())
        {
            performed = true;

            
            player.currentInteractable = null;

            if (player != null && player.playerCharacter == PlayerCharacter.Sherlock)
                WatsonEscortController.Instance?.ForceFarewell();
            StartCoroutine(GoUpStairs(player));
        }
        else
        {
            PlayerTopText.Instance.ShowTopText("Madam Selma pilnuje schodów na górę, interesujące", 
                "Na razie nie przejdziemy, rozejrzyjmy się po domu.");
            player.currentInteractable = null;
        }
  
    }

    private IEnumerator GoUpStairs(PlayerController player)
    {
        if (SwitchCharacter.Instance != null)
            SwitchCharacter.Instance.canSwitch = false;

        CluesLog.Instance?.RemoveCrimeSceneObjective();
        CluesLog.Instance?.CompleteFindWayUpstairsObjective();
        CluesLog.Instance?.BeginUpperFloorEvidenceObjective();

        level_2.SetActive(true);
        yield return null;

        TeleportToLevel2Start(player, level2StartingPoint);
        if (watsonPlayer != null && watsonPlayer != player)
            TeleportToLevel2Start(watsonPlayer, watsonLevel2StartingPoint);

        PlayLevel2ArrivalMusic();

        yield return null;

        level_1.SetActive(false);
        sherlockWatsonSelmaDialogToDisable?.DisableAfterStairsUp();
        blackboardToEnableOnArrival?.SetActive(true);

        if (gameObjectsToEnableOnArrival != null)
        {
            foreach (GameObject gameObjectToEnable in gameObjectsToEnableOnArrival)
            {
                if (gameObjectToEnable != null)
                    gameObjectToEnable.SetActive(true);
            }
        }

        player.currentInteractable = null;
        interactable?.MarkCompleted();

        yield return null;
    }

    private void PlayLevel2ArrivalMusic()
    {
        if (level2ArrivalMusic == null)
            return;

        if (gameMusicManager == null)
            gameMusicManager = FindFirstObjectByType<GameMusicManager>();

        gameMusicManager?.ChangeMusic(level2ArrivalMusic, loopLevel2ArrivalMusic);
    }

    private static void TeleportToLevel2Start(PlayerController actor, Transform destination)
    {
        if (actor == null || destination == null)
            return;

        if (actor.navMeshAgent != null)
        {
            actor.navMeshAgent.ResetPath();
            if (actor.navMeshAgent.isOnNavMesh)
                actor.navMeshAgent.Warp(destination.position);
            else
                actor.transform.position = destination.position;
        }
        else
        {
            actor.transform.position = destination.position;
        }

        actor.transform.rotation = destination.rotation;
    }

    public void RefreshSelmaStairsAwareness()
    {
        Transform positionTarget = GetSelmaPositionTarget();
        if (positionTarget == null)
        {
            debugIsSelmaInStairsRoom = false;
            debugIsStairsObserved = false;
            return;
        }

        if (selmaEyePoint == null)
            selmaEyePoint = positionTarget;

        debugIsSelmaInStairsRoom = stairsRoomVolume != null &&
                                   IsInsideVolume(stairsRoomVolume, positionTarget.position);
        debugIsStairsObserved = debugIsSelmaInStairsRoom || CanSeeStairsObservationVolume();
    }

    private Transform GetSelmaPositionTarget()
    {
        return selmaPositionTarget;
    }

    private bool CanUseStairs()
    {
        if (canGoUpStairs)
            return true;

        if (!isSherlockWantToGoUpstairs)
            return false;

        return allowStairsWhenSelmaClear &&
               selmaPositionTarget != null &&
               (stairsRoomVolume != null || stairsObservationVolume != null) &&
               !IsSelmaBlockingSherlockAtStairs();
    }

    private void MonitorSherlockApproachingStairs()
    {
        if (!isSherlockWantToGoUpstairs || isFirstDiscoverySequencePlaying || !debugSherlockApproachingStairs ||
            sherlockPlayer == null || sherlockPlayer.navMeshAgent == null)
        {
            return;
        }

        // A different interaction deliberately cancels this stairs approach.
        if (sherlockPlayer.currentInteractable != null && sherlockPlayer.currentInteractable != interactable)
        {
            debugSherlockApproachingStairs = false;
            return;
        }

        bool isWalkingToStairs = sherlockPlayer.navMeshAgent.pathPending ||
                                sherlockPlayer.navMeshAgent.hasPath ||
                                sherlockPlayer.navMeshAgent.velocity.sqrMagnitude > 0.01f;
        if (!isWalkingToStairs)
            return;

        RefreshSelmaStairsAwareness();
        debugSherlockInStairsStopVolume = IsSherlockInStairsStopVolume(sherlockPlayer);
        if ((!debugIsSelmaInStairsRoom && !debugIsStairsObserved) || !debugSherlockInStairsStopVolume)
            return;

        isFirstDiscoverySequencePlaying = true;
        debugSherlockApproachingStairs = false;
        sherlockPlayer.navMeshAgent.ResetPath();
        sherlockPlayer.currentInteractable = null;

        if (selmaGateDialogueCoroutine != null)
            StopCoroutine(selmaGateDialogueCoroutine);

        if (firstDiscovery)
        {
            selmaGateDialogueCoroutine = StartCoroutine(PlayRepetitiveDiscoverySequence());
        }
        else
        {
            firstDiscovery = true;
            selmaGateDialogueCoroutine = StartCoroutine(PlayFirstDiscoverySequence());
        }
    }

    public void NotifySherlockStairsApproach(PlayerController player)
    {
        if (!isSherlockWantToGoUpstairs || player == null ||
            (player.playerCharacter != PlayerCharacter.Sherlock && !player.CompareTag("PlayerA")))
        {
            return;
        }

        sherlockPlayer = player;
        debugSherlockApproachingStairs = true;
    }

    private void PlayStairsUnavailableDialogue()
    {
        if (selmaGateDialogueCoroutine != null)
            StopCoroutine(selmaGateDialogueCoroutine);

        selmaGateDialogueCoroutine = StartCoroutine(PlayStairsUnavailableDialogueSequence());
    }

    private IEnumerator PlayStairsUnavailableDialogueSequence()
    {
        yield return PlayDialogueLines(new[] { stairsUnavailableDialogueLine });
        selmaGateDialogueCoroutine = null;
    }

    private bool IsSelmaBlockingSherlockAtStairs()
    {
        return (debugIsSelmaInStairsRoom || debugIsStairsObserved) &&
               IsSherlockInStairsStopVolume(sherlockPlayer);
    }

    private bool IsSherlockInStairsStopVolume(PlayerController player)
    {
        Collider stopVolume = sherlockStairsStopVolume != null
            ? sherlockStairsStopVolume
            : stairsRoomVolume;
        if (stopVolume == null)
            return false;

        Transform target = player != null ? player.transform : SherlockGO != null ? SherlockGO.transform : null;
        return target != null && IsInsideVolume(stopVolume, target.position);
    }

    private IEnumerator PlayFirstDiscoverySequence()
    {
        activateOnFirstStairsDiscovery?.SetActive(true);

        Transform selma = GetSelmaPositionTarget();
        if (selma != null && SherlockGO != null)
        {
            WatsonEscortNPC escortNpc = selma.GetComponent<WatsonEscortNPC>() ??
                                         selma.GetComponentInParent<WatsonEscortNPC>();
            if (escortNpc != null)
                yield return escortNpc.RotateTowards(SherlockGO.transform.position);
            else
                yield return RotateTransformTowards(selma, SherlockGO.transform.position);
        }

        yield return PlayDialogueLines(firstDiscoveryDialogueLines);
        CluesLog.Instance?.AddFindEthelUpstairsObjective();

        Transform watson = SwitchCharacter.Instance != null ? SwitchCharacter.Instance.watsonTransform : null;
        if (watson != null && SherlockGO != null)
            yield return RotateTransformTowards(watson, SherlockGO.transform.position);

        if (SherlockGO != null && selma != null)
            yield return RotateTransformTowards(SherlockGO.transform, selma.position);

        yield return PlayDialogueLines(firstDiscoveryResolutionDialogueLines);

        GetCameraController()?.SetZoomPreset(discoveryCameraPreset, discoveryCameraTransitionSpeed);

        yield return PlayDialogueLines(afterDiscoveryCameraDialogueLines);

        isFirstDiscoverySequencePlaying = false;
        selmaGateDialogueCoroutine = null;
    }

    private IEnumerator PlayRepetitiveDiscoverySequence()
    {
        Transform selma = GetSelmaPositionTarget();
        if (selma != null && SherlockGO != null)
        {
            WatsonEscortNPC escortNpc = selma.GetComponent<WatsonEscortNPC>() ??
                                         selma.GetComponentInParent<WatsonEscortNPC>();
            if (escortNpc != null)
                yield return escortNpc.RotateTowards(SherlockGO.transform.position);
            else
                yield return RotateTransformTowards(selma, SherlockGO.transform.position);
        }

        Transform watson = SwitchCharacter.Instance != null ? SwitchCharacter.Instance.watsonTransform : null;
        if (watson != null && SherlockGO != null)
            yield return RotateTransformTowards(watson, SherlockGO.transform.position);

        yield return PlayRandomDialogueLine(repetitiveDiscoveryDialogueLines);

        isFirstDiscoverySequencePlaying = false;
        selmaGateDialogueCoroutine = null;
    }

    private IEnumerator RotateTransformTowards(Transform actor, Vector3 targetPosition)
    {
        if (actor == null)
            yield break;

        Vector3 direction = targetPosition - actor.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        NavMeshAgent agent = actor.GetComponent<NavMeshAgent>() ??
                             actor.GetComponentInParent<NavMeshAgent>() ??
                             actor.GetComponentInChildren<NavMeshAgent>();
        bool restoreAgentRotation = agent != null;
        bool previousAgentUpdateRotation = restoreAgentRotation && agent.updateRotation;
        if (restoreAgentRotation)
            agent.updateRotation = false;

        while (Quaternion.Angle(actor.rotation, targetRotation) > 0.5f)
        {
            actor.rotation = Quaternion.RotateTowards(
                actor.rotation,
                targetRotation,
                discoveryRotationSpeed * Time.deltaTime);
            yield return null;
        }

        actor.rotation = targetRotation;

        if (restoreAgentRotation)
            agent.updateRotation = previousAgentUpdateRotation;
    }

    private IEnumerator PlayDialogueLines(Lvl3DialogueLine[] lines)
    {
        if (lines == null)
            yield break;

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
    }

    private IEnumerator PlayRandomDialogueLine(Lvl3DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            yield break;

        yield return PlayDialogueLines(new[] { lines[UnityEngine.Random.Range(0, lines.Length)] });
    }

    private CameraController GetCameraController()
    {
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        return cameraController;
    }

    private IEnumerator PlaySelmaGateDialogue()
    {
        yield return PlayDialogueLines(selmaBlocksStairsDialogue);

        selmaGateDialogueCoroutine = null;
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

    private bool CanSeeStairsObservationVolume()
    {
        Collider observationVolume = stairsObservationVolume != null
            ? stairsObservationVolume
            : stairsRoomVolume;

        if (selmaEyePoint == null || observationVolume == null)
            return false;

        Bounds bounds = observationVolume.bounds;
        Vector3 center = bounds.center;
        Vector3[] samplePoints =
        {
            center,
            new Vector3(bounds.min.x, center.y, bounds.min.z),
            new Vector3(bounds.min.x, center.y, bounds.max.z),
            new Vector3(bounds.max.x, center.y, bounds.min.z),
            new Vector3(bounds.max.x, center.y, bounds.max.z)
        };

        foreach (Vector3 point in samplePoints)
        {
            if (CanSeeStairsPoint(point))
                return true;
        }

        return false;
    }

    private bool CanSeeStairsPoint(Vector3 point)
    {
        Vector3 origin = selmaEyePoint.position;
        Vector3 direction = point - origin;
        float distance = direction.magnitude;
        if (distance > selmaVisionRange || distance < 0.001f)
            return false;

        Vector3 normalizedDirection = direction / distance;
        if (Vector3.Angle(selmaEyePoint.forward, normalizedDirection) > selmaFieldOfView * 0.5f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            normalizedDirection,
            distance,
            selmaVisionObstructionMask,
            selmaVisionTriggerInteraction);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsPartOfSelma(hit.collider.transform) ||
                IsPartOfStairsObservation(hit.collider.transform))
                continue;

            return false;
        }

        return true;
    }

    private bool IsPartOfSelma(Transform candidate)
    {
        Transform selmaTransform = GetSelmaPositionTarget();
        return selmaTransform != null && candidate != null &&
               (candidate == selmaTransform || candidate.IsChildOf(selmaTransform) ||
                selmaTransform.IsChildOf(candidate));
    }

    private bool IsPartOfStairsObservation(Transform candidate)
    {
        Collider observationVolume = stairsObservationVolume != null
            ? stairsObservationVolume
            : stairsRoomVolume;
        return observationVolume != null && candidate != null &&
               (candidate == observationVolume.transform ||
                candidate.IsChildOf(observationVolume.transform) ||
                observationVolume.transform.IsChildOf(candidate));
    }

    private static bool IsInsideVolume(Collider volume, Vector3 position)
    {
        Vector3 closestPoint = volume.ClosestPoint(position);
        return (closestPoint - position).sqrMagnitude < 0.0001f;
    }

    private void OnDrawGizmosSelected()
    {
        if (selmaEyePoint == null)
            return;

        Gizmos.color = debugIsStairsObserved
            ? new Color(1f, 0.35f, 0.2f, 0.6f)
            : new Color(0.2f, 0.9f, 0.4f, 0.45f);
        Gizmos.DrawWireSphere(selmaEyePoint.position, selmaVisionRange);
        Gizmos.DrawLine(selmaEyePoint.position, selmaEyePoint.position + selmaEyePoint.forward * selmaVisionRange);
    }
}
