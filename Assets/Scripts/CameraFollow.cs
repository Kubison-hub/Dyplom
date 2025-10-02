using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Target to follow.")]
    public Transform target;

    [Tooltip("Offset from target position (e.g. (0,10,-10) for isometric view).")]
    public Vector3 offset = new Vector3(0, 10, -10);

    [Tooltip("How fast the camera moves to target.")]
    public float followSpeed = 5f;

    [Tooltip("How fast the camera rotates to look at target.")]
    public float rotationSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Smooth position
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);

        // Smooth rotation
        Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSpeed);
    }

    // Called from PlayerSwitcher when active player changes
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
