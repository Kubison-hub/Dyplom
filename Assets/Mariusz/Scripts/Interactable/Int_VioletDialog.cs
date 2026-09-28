using DialogueEditor;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class Int_VioletDialog : Lvl3InteractionDialogueBase
{
    public SmartNPC smartNPC;
    public Interactable interactable;
    public GameObject VioletGO;
    public bool performed = false;
    public CinemachineCamera dialogCam;
    public CinemachineSplineDolly splineDolly;
    public Transform cardPosition;

    public static Int_VioletDialog Instance;
    public bool dialogPerformed = false;
    public GameObject[] nextInteractions;

    public bool isVioletInRoom = true;

    [Header("Face Player Before Dialogue")]
    [SerializeField] private bool facePlayerBeforeDialogue = true;
    [SerializeField, Min(1f)] private float facePlayerTurnSpeed = 360f;
    [SerializeField, Min(0.1f)] private float facePlayerTolerance = 1f;
    [SerializeField] private bool restoreRotationAfterDialogue = true;
    [SerializeField, Min(1f)] private float restoreRotationTurnSpeed = 240f;

    [Header("Intro Dialogue")]
    [Tooltip("Played after Violet faces the player and before the SmartNPC conversation begins.")]
    [SerializeField] private Lvl3DialogueLine[] introDialogue;

    [Header("Watson Escort Unlock")]
    [Tooltip("When enabled, Violet's normal dialogue cannot be started until Watson has escorted her to a valid destination.")]
    [SerializeField] private bool requireSuccessfulWatsonEscort = true;
    [SerializeField] private bool automaticallyStartDialogueAfterEscort = true;

    [Header("After Watson Escort Dialogue")]
    [SerializeField, Min(1f)] private float sherlockFaceVioletTurnSpeed = 300f;
    [SerializeField, Min(0.1f)] private float sherlockFaceVioletTolerance = 1f;
    [SerializeField] private Lvl3DialogueLine[] sherlockAfterEscortDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Teraz mam szans\u0119 przeszuka\u0107 bibliotek\u0119.",
            duration = 3f
        }
    };

    [Header("Library Awareness")]
    [Tooltip("Assign the Violet GameObject that actually carries WatsonEscortNPC and NavMeshAgent.")]
    [SerializeField] private Transform violetPositionTarget;
    [SerializeField] private Transform eyePoint;
    [SerializeField] private Collider libraryRoomVolume;
    [SerializeField] private Collider libraryObservationVolume;
    [SerializeField, Min(0.1f)] private float libraryVisionRange = 10f;
    [SerializeField, Range(1f, 360f)] private float libraryFieldOfView = 100f;
    [SerializeField] private LayerMask libraryVisionObstructionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction libraryVisionTriggerInteraction = QueryTriggerInteraction.Ignore;

    public bool IsLibraryObserved => libraryRoomVolume == null ? isVioletInRoom : isLibraryObserved;
    public bool IsOutsideLibraryButObserving => !isVioletInRoom && IsLibraryObserved;

    public Transform moveDestination;
    private Vector3 destination;
    private bool isLibraryObserved;
    private bool dialogueStarting;
    private bool dialogueUnlocked;
    private bool dialogueStartedByEscort;
    private bool switchToSherlockAfterEscortDialogue = true;

    public bool IsDialogueAvailable => !requireSuccessfulWatsonEscort || dialogueUnlocked;
    public bool IsDialogueInProgress => dialogueStarting;
    public bool IsDialogueAvailableFor(PlayerController player)
    {
        return IsDialogueAvailable || !IsWatson(player);
    }
    public bool WillAutomaticallyStartAfterEscort => automaticallyStartDialogueAfterEscort;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => introDialogue;

    private void Start()
    {

        dialogueUnlocked = !requireSuccessfulWatsonEscort;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        interactable = GetComponent<Interactable>();

        if (dialogCam != null)
        {
            splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();
        }

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

        VioletGO = GameObject.Find("VIOLET_NPC");
        if (VioletGO != null)
        {
            transform.parent = VioletGO.transform;
            transform.position = VioletGO.transform.position;
            transform.rotation = VioletGO.transform.localRotation;
        }
        else
        {
            Debug.LogWarning("VIOLET_NPC was not found for Violet dialog interaction.");
        }

        if (smartNPC == null && VioletGO != null)
            smartNPC = VioletGO.GetComponent<SmartNPC>();

        RefreshLibraryAwareness();
    }

    

    public void PerformInteraction(PlayerController player)
    {
        if (!IsDialogueAvailableFor(player))
        {
            if (player != null)
                player.currentInteractable = null;
            return;
        }

        if (!dialogueStarting && ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            if (!performed)
            {
                performed = true;
                interactable.AddClue(0, cardPosition);
            }

            destination = moveDestination.position;
            player.currentInteractable = null;
            StartCoroutine(BeginDialogue(player));

        }
        else
        {
            Debug.LogWarning("Cannot start Violet dialog while another conversation is active.");
            player.currentInteractable = null;
        }




        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private static bool IsWatson(PlayerController player)
    {
        if (player == null || SwitchCharacter.Instance == null ||
            SwitchCharacter.Instance.players == null || SwitchCharacter.Instance.players.Length < 2 ||
            SwitchCharacter.Instance.players[1] == null)
            return false;

        return SwitchCharacter.Instance.players[1].GetComponent<PlayerController>() == player;
    }

    public bool UnlockAndStartDialogueAfterEscort(PlayerController player, bool switchToSherlockAfterDialogue = true)
    {
        dialogueUnlocked = true;

        if (!automaticallyStartDialogueAfterEscort || dialogueStarting ||
            ConversationManager.Instance == null || ConversationManager.Instance.IsConversationActive)
            return false;

        dialogueStartedByEscort = true;
        switchToSherlockAfterEscortDialogue = switchToSherlockAfterDialogue;
        PerformInteraction(player);
        return true;
    }

    public void PlaySherlockAfterEscortDialogue(PlayerController sherlock)
    {
        if (sherlock != null && sherlockAfterEscortDialogue != null && sherlockAfterEscortDialogue.Length > 0)
            PlayDialogue(sherlock, sherlockAfterEscortDialogue);
    }

    private IEnumerator BeginDialogue(PlayerController player)
    {
        dialogueStarting = true;
        Transform dialogueTransform = smartNPC != null ? smartNPC.transform : null;
        Quaternion rotationBeforeDialogue = dialogueTransform != null
            ? dialogueTransform.rotation
            : Quaternion.identity;

        if (facePlayerBeforeDialogue && dialogueTransform != null)
        {
            yield return NpcDialogueFacingUtility.FacePlayer(
                dialogueTransform,
                player != null ? player.transform : null,
                facePlayerTurnSpeed,
                facePlayerTolerance);
        }

        if (introDialogue != null && introDialogue.Length > 0)
        {
            PlayDialogue(player, introDialogue);
            while (IsDialoguePlaying)
                yield return null;
        }

        if (dialogueStartedByEscort && dialogueTransform != null &&
            SwitchCharacter.Instance != null && SwitchCharacter.Instance.players != null &&
            SwitchCharacter.Instance.players.Length > 0)
        {
            PlayerController sherlock = SwitchCharacter.Instance.players[0].GetComponent<PlayerController>();
            if (sherlock != null)
            {
                yield return NpcDialogueFacingUtility.FacePlayer(
                    sherlock.transform,
                    dialogueTransform,
                    sherlockFaceVioletTurnSpeed,
                    sherlockFaceVioletTolerance);
            }
        }

        smartNPC?.SprawdzIZacznijRozmowe();

        // ConversationManager is activated by SmartNPC. Wait one frame so the
        // conversation can enter its active state before waiting for its end.
        yield return null;
        bool conversationWasStarted = ConversationManager.Instance != null &&
                                      ConversationManager.Instance.IsConversationActive;
        while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            yield return null;

        if (conversationWasStarted)
            CluesLog.Instance?.RegisterSessionWitnessInterview(smartNPC);

        if (restoreRotationAfterDialogue && dialogueTransform != null)
        {
            while (Quaternion.Angle(dialogueTransform.rotation, rotationBeforeDialogue) > facePlayerTolerance)
            {
                dialogueTransform.rotation = Quaternion.RotateTowards(
                    dialogueTransform.rotation,
                    rotationBeforeDialogue,
                    restoreRotationTurnSpeed * Time.deltaTime);
                yield return null;
            }

            dialogueTransform.rotation = rotationBeforeDialogue;
        }

        bool dialogueWasStartedByEscort = dialogueStartedByEscort;
        bool redFigureCollected = InventoryManager.Instance != null &&
                                  InventoryManager.Instance.items.Contains(ItemType.Czerwona);
        bool returnControlToSherlock = dialogueWasStartedByEscort &&
                                       switchToSherlockAfterEscortDialogue &&
                                       !redFigureCollected;
        dialogueStartedByEscort = false;
        switchToSherlockAfterEscortDialogue = true;
        dialogueStarting = false;

        if (dialogueWasStartedByEscort && redFigureCollected)
        {
            WatsonEscortController escortController = WatsonEscortController.Instance;
            WatsonEscortNPC escortNpc = VioletGO != null
                ? VioletGO.GetComponentInChildren<WatsonEscortNPC>(true)
                : null;

            escortController?.ForceFarewell();

            while (escortController != null && escortController.IsEscorting)
                yield return null;

            if (escortNpc != null)
                yield return escortNpc.WaitForDialogueToFinish();

            escortController?.RestoreSwitchingAndSelectSherlock();
            yield break;
        }

        if (returnControlToSherlock)
        {
            PlayerController sherlock = WatsonEscortController.Instance?.RestoreSwitchingAndSelectSherlock();

        }
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            if (splineDolly != null)
            {
                splineDolly.CameraPosition = .8f;
            }

            dialogCam.Priority = 50;
        }
    }

    public void MoveToPosition()
    {
        if (smartNPC == null)
        {
            Debug.LogWarning("Cannot move Violet because SmartNPC is missing.");
            return;
        }

        smartNPC.GoToPoint(destination, () => ActiveNextInteractions(true));
    }

    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(active);
        }
    }

    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }

    public void RefreshLibraryAwareness()
    {
        Transform positionTarget = GetVioletPositionTarget();
        if (libraryRoomVolume == null || positionTarget == null)
            return;

        if (eyePoint == null)
            eyePoint = positionTarget;

        isVioletInRoom = IsInsideVolume(libraryRoomVolume, positionTarget.position);
        isLibraryObserved = isVioletInRoom || CanSeeLibraryObservationVolume();
    }

    public bool WouldLibraryBeObservedFrom(Vector3 position, Quaternion rotation)
    {
        Collider observationVolume = GetLibraryObservationVolume();
        Transform positionTarget = GetVioletPositionTarget();
        if (observationVolume == null || positionTarget == null)
            return false;

        if (IsInsideVolume(observationVolume, position))
            return true;

        Transform visionOrigin = eyePoint != null ? eyePoint : positionTarget;
        Quaternion rotationDelta = rotation * Quaternion.Inverse(positionTarget.rotation);
        Vector3 origin = position + rotationDelta * (visionOrigin.position - positionTarget.position);
        Vector3 forward = rotationDelta * visionOrigin.forward;

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
            if (CanSeeLibraryPoint(point, origin, forward))
                return true;
        }

        return false;
    }

    private Transform GetVioletPositionTarget()
    {
        if (violetPositionTarget != null)
            return violetPositionTarget;

        if (VioletGO == null)
            return null;

        WatsonEscortNPC escortNpc = VioletGO.GetComponentInChildren<WatsonEscortNPC>(true);
        violetPositionTarget = escortNpc != null ? escortNpc.transform : VioletGO.transform;
        return violetPositionTarget;
    }

    private bool CanSeeLibraryObservationVolume()
    {
        Collider observationVolume = GetLibraryObservationVolume();
        if (eyePoint == null || observationVolume == null)
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
            if (CanSeeLibraryPoint(point))
                return true;
        }

        return false;
    }

    private bool CanSeeLibraryPoint(Vector3 point)
    {
        return CanSeeLibraryPoint(point, eyePoint.position, eyePoint.forward);
    }

    private bool CanSeeLibraryPoint(Vector3 point, Vector3 origin, Vector3 forward)
    {
        Vector3 direction = point - origin;
        float distance = direction.magnitude;
        if (distance > libraryVisionRange || distance < 0.001f)
            return false;

        Vector3 normalizedDirection = direction / distance;
        if (Vector3.Angle(forward, normalizedDirection) > libraryFieldOfView * 0.5f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            normalizedDirection,
            distance,
            libraryVisionObstructionMask,
            libraryVisionTriggerInteraction);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsPartOfViolet(hit.collider.transform) ||
                IsPartOfLibraryObservation(hit.collider.transform))
                continue;

            return false;
        }

        return true;
    }

    private static bool IsInsideVolume(Collider volume, Vector3 position)
    {
        Vector3 closestPoint = volume.ClosestPoint(position);
        return (closestPoint - position).sqrMagnitude < 0.0001f;
    }

    private bool IsPartOfViolet(Transform candidate)
    {
        Transform violetTransform = VioletGO != null ? VioletGO.transform : null;
        return violetTransform != null && candidate != null &&
               (candidate == violetTransform || candidate.IsChildOf(violetTransform));
    }

    private bool IsPartOfLibraryObservation(Transform candidate)
    {
        Collider observationVolume = GetLibraryObservationVolume();
        return observationVolume != null && candidate != null &&
               (candidate == observationVolume.transform ||
                candidate.IsChildOf(observationVolume.transform));
    }

    private Collider GetLibraryObservationVolume()
    {
        return libraryObservationVolume != null ? libraryObservationVolume : libraryRoomVolume;
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null)
            return;

        Gizmos.color = IsLibraryObserved
            ? new Color(1f, 0.35f, 0.2f, 0.6f)
            : new Color(0.2f, 0.9f, 0.4f, 0.45f);
        Gizmos.DrawWireSphere(eyePoint.position, libraryVisionRange);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + eyePoint.forward * libraryVisionRange);
    }

    

}
