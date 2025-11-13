using UnityEngine;

/// <summary>
/// Drzwi obs³uguj¹ DotProdukt odnoœnie kierunku Playera i switchuj¹ otwarte/zamkniête.
/// Koreluj¹ z pomieszczeniami. Do Ka¿dych drzwi przypisane s¹ pomieszczenia których dotycz¹cz¹, dziêki czemu maj¹ wp³yw na odkrywanie pomieszczeñ i wyciemnienia.
/// </summary>

public class DoorTrigger : MonoBehaviour
{
    [Header("Drzwi")]
    public GameObject door;
    public bool isLocked = false;
    public float openAngle = 90f;
    public float openSpeed = 5f;

    [Header("Pointy interakcji")]
    public Transform interactionPointFront;
    public Transform interactionPointBack;

    public bool isOpen = false;
    public bool isAnimating = false;

    private float currentAngleDirection = 1f;
    private Quaternion closedRotation;
    private Quaternion targetRotation;

    private PlayerController expectedPlayer = null;

    private Interactable interactable;

    [SerializeField] Room[] rooms;

    private void Start()
    {
        if (door != null)
        {
            closedRotation = door.transform.rotation;
            targetRotation = closedRotation;
        }

        interactable = GetComponent<Interactable>();
    }

    public void Interact(PlayerController player)
    {

        expectedPlayer = null;

        if (isLocked && !isOpen)
        {
            Debug.Log("Door locked");
            return;
        }

        MovePlayerToInteractionPoint(player);

    }

    private void Update()
    {
        if (isAnimating && door != null)
        {
            door.transform.rotation = Quaternion.Lerp(
                door.transform.rotation,
                targetRotation,
                Time.deltaTime * openSpeed
            );

            if (Quaternion.Angle(door.transform.rotation, targetRotation) < 0.5f)
            {
                door.transform.rotation = targetRotation;
                isAnimating = false;
            }
        }
    }



    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Debug.Log("MovePlayerToInteractionPoint");

        if (door != null)
        {
            var data = GetInteractionData(player.transform);
            player.targetPosition = data.position;
            currentAngleDirection = data.angleDirection;

            player.currentInteractable = interactable;
            player.isWalking = true;

            expectedPlayer = player;
        }
    }

    public void PerformInteraction()
    {
        Debug.Log("Perform Interaction " + gameObject.name);

        isOpen = !isOpen;

        if (isOpen)
        {
            targetRotation = Quaternion.Euler(0f, currentAngleDirection * openAngle, 0f) * closedRotation;

            RoomManager.Instance.UpdateAllRooms();
        }
        else
        {
            targetRotation = closedRotation;

            RoomManager.Instance.UpdateAllRooms();
        }

        isAnimating = true;
    }

    public (Vector3 position, float angleDirection) GetInteractionData(Transform playerTransform)
    {
        Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toPlayer);

        float angleDir = (dot > 0f) ? -1f : 1f;
        Vector3 pos = (dot > 0f) ? interactionPointFront.position : interactionPointBack.position;

        return (pos, angleDir);
    }

    public bool IsOpen() => isOpen;

}
