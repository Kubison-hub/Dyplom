using UnityEngine;

public class LookAtCameraUI : MonoBehaviour
{
    [SerializeField] private bool lockYOnly = true;
    [SerializeField] private bool flipForward = true;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    private Transform target;

    public void SetCamera(Camera camera)
    {
        if (camera == null)
        {
            Debug.LogWarning("LookAtCameraUI: Camera is null.");
            return;
        }

        target = camera.transform;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;

        if (lockYOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (flipForward)
            targetRotation *= Quaternion.Euler(0f, 180f, 0f);

        targetRotation *= Quaternion.Euler(rotationOffset);

        transform.rotation = targetRotation;
    }
}