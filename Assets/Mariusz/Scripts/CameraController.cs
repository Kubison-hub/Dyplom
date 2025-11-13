using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Tutaj Zrobi³em Zoom i Rotacjê kamery, bo nie potrafi³em tego ogarn¹æ w edytorze w Cinemachine, ale jest to pewnie optymalne.
/// </summary>

public class CameraController : MonoBehaviour
{
    private CinemachineCamera cineCamera;
    [SerializeField] private float zoomSpeed = 30f;
    [SerializeField] private float rotationSpeed = 2f;

    [SerializeField] private float minRadius = 10f;
    [SerializeField] private float maxRadius = 20f;

    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 20f;

    private CinemachineOrbitalFollow orbitalFollow;

    private void Start()
    {
        cineCamera = GetComponent<CinemachineCamera>();
        orbitalFollow = cineCamera.GetComponentInChildren<CinemachineOrbitalFollow>();

        if (orbitalFollow == null)
        {
            Debug.LogError("CinemachineOrbitalFollow not found");
        }
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {

        float scrollDelta = context.ReadValue<Vector2>().y;

        var top = orbitalFollow.Orbits.Top;
        var center = orbitalFollow.Orbits.Center;
        var bottom = orbitalFollow.Orbits.Bottom;

        top.Radius -= scrollDelta * zoomSpeed * Time.deltaTime;
        top.Radius = Mathf.Clamp(top.Radius, minRadius, maxRadius);
        top.Height -= scrollDelta * (zoomSpeed / 2f) * Time.deltaTime;
        top.Height = Mathf.Clamp(top.Height, minHeight, maxHeight);

        center.Radius -= scrollDelta * zoomSpeed * Time.deltaTime;
        center.Radius = Mathf.Clamp(center.Radius, minRadius, maxRadius);
        center.Height -= scrollDelta * (zoomSpeed / 2f) * Time.deltaTime;
        center.Height = Mathf.Clamp(center.Height, minHeight, maxHeight);

        bottom.Radius -= scrollDelta * zoomSpeed * Time.deltaTime;
        bottom.Radius = Mathf.Clamp(bottom.Radius, minRadius, maxRadius);
        bottom.Height -= scrollDelta * (zoomSpeed / 2f) * Time.deltaTime;
        bottom.Height = Mathf.Clamp(bottom.Height, minHeight, maxHeight);

        orbitalFollow.Orbits.Top = top;
        orbitalFollow.Orbits.Center = center;
        orbitalFollow.Orbits.Bottom = bottom;
    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();

        orbitalFollow.HorizontalAxis.Value += value.x * rotationSpeed;
    }
}
