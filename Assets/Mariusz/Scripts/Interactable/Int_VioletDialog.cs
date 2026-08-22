using DialogueEditor;
using Unity.Cinemachine;
using UnityEngine;

public class Int_VioletDialog : MonoBehaviour
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

    public Transform moveDestination;
    private Vector3 destination;
    private bool isLibraryObserved;
    private void Start()
    {

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
        if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            if (!performed)
            {
                performed = true;
                interactable.AddClue(0, cardPosition);
            }

            smartNPC.SprawdzIZacznijRozmowe();
           
            destination = moveDestination.position;
            
            player.currentInteractable = null;

        }
        else
        {
            Debug.LogWarning("Cannot start Violet dialog while another conversation is active.");
            player.currentInteractable = null;
        }




        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
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
        Vector3 origin = eyePoint.position;
        Vector3 direction = point - origin;
        float distance = direction.magnitude;
        if (distance > libraryVisionRange || distance < 0.001f)
            return false;

        Vector3 normalizedDirection = direction / distance;
        if (Vector3.Angle(eyePoint.forward, normalizedDirection) > libraryFieldOfView * 0.5f)
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
