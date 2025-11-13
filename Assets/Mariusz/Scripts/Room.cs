using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ka¿de pomieszcze ma klasê Room, która zapisuje siê w RoomManagerze i obs³uguje odkrywane przez gracza pomieszczenia.
/// Zachowanie pomieszczeñ zale¿y od tego czy gracz odkry³ pomieszczenie, czy postaæ lub postacie s¹ w pomieszczeniu, albo w pomieszczeniach s¹siaduj¹cych z otwartymi drzwiami.
/// Do ka¿dego pomiesczenia przypisane s¹ pomieszczenia s¹siaduj¹ce i ich drzwi.
/// </summary>

public class Room : MonoBehaviour
{
    public bool isActive;
    public bool discovered = false;

    [Space]
    public GameObject root;
    [SerializeField] private List<NeighborRoom> neighborRooms = new List<NeighborRoom>();

    private int playersInside = 0;

    private float currentAlpha;
    private float targetAlpha;
    [SerializeField] private MeshRenderer fogRenderer;


    private void Start()
    {
        RoomManager.Instance.RegisterRoom(this);

        currentAlpha = RoomManager.Instance.roomFadeAlpha;
        targetAlpha = RoomManager.Instance.roomFadeAlpha;

        ApplyVisualState();
    }

    private void Update()
    {
        HandleFogAlpha();
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            playersInside++;
            RoomManager.Instance.UpdateAllRooms();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            playersInside--;
            RoomManager.Instance.UpdateAllRooms();
        }
    }

    public void UpdateRoomState()
    {
        bool roomOccupied = playersInside > 0;
        bool playerVisibleFromNeighbor = IsVisibleFromNeighborRoom();

        bool newActive = roomOccupied || playerVisibleFromNeighbor;

        if (newActive != isActive)
        {
            isActive = newActive;

            if (isActive && !discovered)
            {
                discovered = true;
            }

            ApplyVisualState();
            
        }

        //Debug.Log($"{gameObject.name}  isActive: {isActive}, " +
        //  $"playersInside: {playersInside}, discovered: {discovered}");
    }

    public void ApplyVisualState()
    {

        if (isActive)
        {
            // Room Active
            root.SetActive(true);
            targetAlpha = 0f;

        }
        else if (!isActive && discovered)
        {
            // Room NotActive -> discovered
            root.SetActive(true);
            targetAlpha = RoomManager.Instance.roomFadeAlpha;
        }
        else if (!isActive && !discovered)
        {
            //Room NotActive -> notDiscovered
            root.SetActive(false);
            targetAlpha = 1;
        }
        
    }

    private bool IsVisibleFromNeighborRoom()
    {
        foreach (var neighbor in neighborRooms)
        {
            if (neighbor.room == null && neighbor.connectingDoor == null)
                continue;

            if (neighbor.connectingDoor.IsOpen() && neighbor.room.playersInside > 0)
            {
                return true;
            }
        }

        return false;
    }


    private void HandleFogAlpha()
    {
        if (fogRenderer == null) return;

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, RoomManager.Instance.roomFadeSpeed * Time.deltaTime);

        Color c = fogRenderer.material.color;
        c.a = currentAlpha;
        fogRenderer.material.color = c;
    }

}

[System.Serializable]
public class NeighborRoom
{
    public Room room;
    public Door connectingDoor;
}
