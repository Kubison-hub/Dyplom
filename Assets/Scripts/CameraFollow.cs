using UnityEngine;

public class CameraFollowSmooth : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 10, -10);
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;

    [Header("Cinematic Zoom")]
    public float zoomAmount = 5f;
    public float zoomSpeed = 2f;
    public float zoomHeight = 2f;

    private Vector3 currentOffset;
    private bool zooming = false;
    private float zoomProgress = 0f;       // 0 = start, 1 = zakoñczone
    private Vector3 zoomStartOffset;

    void Awake()
    {
        currentOffset = offset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // obs³uga zoomu z easing
        if (zooming)
        {
            zoomProgress += Time.deltaTime * zoomSpeed;
            float t = Mathf.Clamp01(zoomProgress);
            t = Mathf.SmoothStep(0f, 1f, t);  // easing funkcja

            // interpolacja offsetu
            currentOffset = Vector3.Lerp(zoomStartOffset, offset, t);

            if (t >= 1f)
                zooming = false;
        }
        else
        {
            // standardowe p³ynne pod¹¿anie
            currentOffset = Vector3.Lerp(currentOffset, offset, followSpeed * Time.deltaTime);
        }

        // ruch kamery
        Vector3 desiredPosition = target.position + currentOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // obrót kamery
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        StartZoom();
    }

    private void StartZoom()
    {
        zooming = true;
        zoomProgress = 0f;
        zoomStartOffset = offset + new Vector3(0, zoomHeight, -zoomAmount);  // startowy offset dla efektu
    }
}
