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

            var movement = players[i].GetComponent<PointAndClickController>(); // your movement script
            var agent = players[i].GetComponent<UnityEngine.AI.NavMeshAgent>();

            bool isActive = (i == index);

            if (movement != null)
                movement.enabled = isActive;

            if (agent != null)
                agent.enabled = isActive; // disable NavMeshAgent on inactive players
        }

        activeIndex = index;

        // Camera target update
        camFollow.SetTarget(players[activeIndex].transform);
    }
}
