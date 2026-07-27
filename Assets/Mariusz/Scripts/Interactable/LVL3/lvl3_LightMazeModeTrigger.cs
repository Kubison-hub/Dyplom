using UnityEngine;

public class lvl3_LightMazeModeTrigger : MonoBehaviour
{
    [SerializeField] private bool activateOnEnter = true;
    [SerializeField] private bool deactivateOnExit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerA"))
            return;

        if (lvl3_GameProgress.Instance == null)
        {
            Debug.LogWarning($"{name}: lvl3_GameProgress.Instance is null.");
            return;
        }

        lvl3_GameProgress.Instance.SetLightMazeMode(activateOnEnter);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!deactivateOnExit)
            return;

        if (!other.CompareTag("PlayerA"))
            return;

        if (lvl3_GameProgress.Instance == null)
            return;

        lvl3_GameProgress.Instance.SetLightMazeMode(false);
    }
}
