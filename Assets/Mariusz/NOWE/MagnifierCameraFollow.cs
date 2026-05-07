using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MagnifierCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (sourceCamera == null || targetCamera == null)
            return;

        transform.position = sourceCamera.transform.position;
        transform.rotation = sourceCamera.transform.rotation;

        targetCamera.fieldOfView = sourceCamera.fieldOfView;
        targetCamera.orthographic = sourceCamera.orthographic;
        targetCamera.orthographicSize = sourceCamera.orthographicSize;
        targetCamera.nearClipPlane = sourceCamera.nearClipPlane;
        targetCamera.farClipPlane = sourceCamera.farClipPlane;
    }
}