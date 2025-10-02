using UnityEngine;

public class PlayerSwitcher : MonoBehaviour
{
    [Header("Players")]
    public GameObject[] players;

    [Header("Switching")]
    public KeyCode switchKey = KeyCode.Tab;

    private int activeIndex = 0;
    private CameraFollow camFollow;

    private void Start()
    {
        if (players.Length == 0)
        {
            Debug.LogError("PlayerSwitcher: No players assigned!");
            enabled = false;
            return;
        }

        camFollow = Camera.main.GetComponent<CameraFollow>();
        if (camFollow == null)
        {
            Debug.LogError("Main Camera must have CameraFollow script!");
            enabled = false;
            return;
        }

        SetActivePlayer(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            int nextIndex = (activeIndex + 1) % players.Length;
            SetActivePlayer(nextIndex);
        }
    }

    private void SetActivePlayer(int index)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            // Movement control
            var movement = players[i].GetComponent<PointAndClickController>();
            if (movement != null)
                movement.enabled = (i == index);

            // Follower control
            var follower = players[i].GetComponent<PlayerFollower>();
            if (follower != null)
            {
                bool isActive = (i == index);
                follower.SetActive(isActive, players[index].transform);
            }
        }

        activeIndex = index;

        // Camera follows the new active player
        camFollow.SetTarget(players[activeIndex].transform);
    }
}
