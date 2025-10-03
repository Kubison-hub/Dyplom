using UnityEngine;
using UnityEngine.AI;

public class PlayerSwitcher : MonoBehaviour
{
    [Header("Players")]
    public GameObject[] players;        // obiekty graczy (dodaj w Inspector)
    public int activeIndex = 0;

    [Header("Controls")]
    public KeyCode switchKey = KeyCode.Tab;      // zmiana postaci
    public KeyCode followToggleKey = KeyCode.B;  // zmiana trybu dla nieaktywnego gracza

    [Header("Camera")]
    public CameraFollowSmooth camFollow;         // przypisz skrypt p³ynnej kamery

    void Start()
    {
        SetActivePlayer(activeIndex);
    }

    void Update()
    {
        // zmiana aktywnego gracza
        if (Input.GetKeyDown(switchKey))
        {
            int nextIndex = (activeIndex + 1) % players.Length;
            SetActivePlayer(nextIndex);
        }

        // tryb pod¹¿ania dla NIEaktywnego gracza
        if (Input.GetKeyDown(followToggleKey))
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (i == activeIndex) continue; // pomijamy aktywnego

                PlayerFollower follower = players[i].GetComponent<PlayerFollower>();
                if (follower != null)
                    follower.ToggleMode();
            }
        }
    }

    private void SetActivePlayer(int index)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            var controller = players[i].GetComponent<PointAndClickController>();
            var follower = players[i].GetComponent<PlayerFollower>();

            bool isActive = (i == index);

            // w³¹cz sterowanie klikaniem tylko dla aktywnego
            if (controller != null)
                controller.EnableInput(isActive);

            // dla nieaktywnego ustaw target do œledzenia aktywnego
            if (follower != null)
                follower.target = players[index].transform;
        }

        activeIndex = index;

        // ustaw nowy cel dla kamery (p³ynne przejœcie)
        if (camFollow != null)
            camFollow.SetTarget(players[activeIndex].transform);
    }
}
