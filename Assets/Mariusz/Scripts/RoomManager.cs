using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RoomManager to Singleton zawieraj¹cy listê wszystkich pomieszczeñ w levelu aby aktualizowaæ aktywacje i wyciemnienia pomieszczeñ.
/// </summary>

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    public float roomFadeSpeed = 5f;
    public float roomFadeAlpha = 0.85f;

    [SerializeField] private List<Room> allRooms;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateAllRooms();
    }

    public void UpdateAllRooms()
    {
        foreach (Room room in allRooms)
        {
            room.UpdateRoomState();
        }
       
    }

    public void RegisterRoom(Room room)
    {
        if (!allRooms.Contains(room))
            allRooms.Add(room);
    }
}
