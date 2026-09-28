using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CameraPresetReturnZone : MonoBehaviour
{
    private Interactable owner;
    private Transform trackedPlayer;
    private float radius;
    private bool playerEntered;
    private bool restoreRequested;

    public void Initialize(Interactable zoneOwner, Transform player, float zoneRadius)
    {
        owner = zoneOwner;
        trackedPlayer = player;
        radius = Mathf.Max(0.1f, zoneRadius);

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = radius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (BelongsToTrackedPlayer(other))
            playerEntered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!BelongsToTrackedPlayer(other))
            return;

        TryRestoreAfterExit();
    }

    private void Update()
    {
        if (playerEntered)
            TryRestoreAfterExit();
    }

    private void TryRestoreAfterExit()
    {
        if (restoreRequested || !playerEntered || IsTrackedPlayerInside())
            return;

        restoreRequested = true;
        owner?.RestoreCameraPresetAfterExit(this);
    }

    private bool BelongsToTrackedPlayer(Collider other)
    {
        return trackedPlayer != null && other != null &&
               (other.transform == trackedPlayer || other.transform.IsChildOf(trackedPlayer));
    }

    private bool IsTrackedPlayerInside()
    {
        return trackedPlayer != null &&
               Vector3.Distance(trackedPlayer.position, transform.position) <= radius;
    }
}
