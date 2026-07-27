using UnityEngine;

public class lvl3_GameProgress : MonoBehaviour
{
    public static lvl3_GameProgress Instance { get; private set; }

    public bool lampPickedUp = false;
    public bool lightMazeMode = false;
    public float lightMazeRange = 4f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetLightMazeMode(bool active)
    {
        lightMazeMode = active;
    }
}
