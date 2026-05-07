using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private string currentZoomState;
    [SerializeField] private int currentZoomIndex;

    private CinemachineCamera cineCamera;
    private CinemachineOrbitalFollow orbitalFollow;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float rotationDamping = 0.15f;

    private float targetRotationInput;
    private float currentRotationInput;
    private float rotationInputVelocity;

    [Header("Zoom")]
    [SerializeField] private float zoomSmoothSpeed = 8f;

    [SerializeField]
    private ZoomPreset[] zoomPresets =
    {
        new ZoomPreset("Top",    22f, 24f, 20f, 20f, 18f, 16f),
        new ZoomPreset("Wide",   18f, 20f, 16f, 16f, 14f, 12f),
        new ZoomPreset("Medium", 14f, 16f, 12f, 12f, 10f, 9f),
        new ZoomPreset("Narrow", 10f, 12f, 8f,  8f,  7f,  6f),
    };

    [SerializeField] private int startZoomIndex = 2;

    private int targetZoomIndex;
    private int previousZoomIndex = -1;

    private void Awake()
    {
        cineCamera = GetComponent<CinemachineCamera>();
        orbitalFollow = cineCamera.GetComponentInChildren<CinemachineOrbitalFollow>();

        if (orbitalFollow == null)
        {
            Debug.LogError("CinemachineOrbitalFollow not found");
            enabled = false;
            return;
        }

        targetZoomIndex = Mathf.Clamp(startZoomIndex, 0, zoomPresets.Length - 1);
        ApplyZoomPresetImmediate(zoomPresets[targetZoomIndex]);
    }

    private void Update()
    {
        SmoothZoomToPreset(zoomPresets[targetZoomIndex]);
        SmoothRotate();
        UpdateDebugZoomState();
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {
        if (!context.performed || orbitalFollow == null)
            return;

        float scrollDelta = context.ReadValue<Vector2>().y;

        if (Mathf.Abs(scrollDelta) < 0.01f)
            return;

        int direction = scrollDelta > 0 ? 1 : -1;

        targetZoomIndex = Mathf.Clamp(
            targetZoomIndex + direction,
            0,
            zoomPresets.Length - 1
        );

        UpdateDebugZoomState();

    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        if (orbitalFollow == null)
            return;

        Vector2 value = context.ReadValue<Vector2>();

        targetRotationInput = value.x;
    }

    private void SmoothRotate()
    {
        if (Mathf.Abs(targetRotationInput) > 0.01f)
        {
            currentRotationInput = targetRotationInput;
            rotationInputVelocity = 0f;
        }
        else
        {
            currentRotationInput = Mathf.SmoothDamp(
                currentRotationInput,
                0f,
                ref rotationInputVelocity,
                rotationDamping
            );
        }

        orbitalFollow.HorizontalAxis.Value += currentRotationInput * rotationSpeed * Time.deltaTime;
    }

    private void SmoothZoomToPreset(ZoomPreset preset)
    {
        float t = 1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime);

        var top = orbitalFollow.Orbits.Top;
        var center = orbitalFollow.Orbits.Center;
        var bottom = orbitalFollow.Orbits.Bottom;

        top.Radius = Mathf.Lerp(top.Radius, preset.topRadius, t);
        top.Height = Mathf.Lerp(top.Height, preset.topHeight, t);

        center.Radius = Mathf.Lerp(center.Radius, preset.centerRadius, t);
        center.Height = Mathf.Lerp(center.Height, preset.centerHeight, t);

        bottom.Radius = Mathf.Lerp(bottom.Radius, preset.bottomRadius, t);
        bottom.Height = Mathf.Lerp(bottom.Height, preset.bottomHeight, t);

        orbitalFollow.Orbits.Top = top;
        orbitalFollow.Orbits.Center = center;
        orbitalFollow.Orbits.Bottom = bottom;
    }

    private void ApplyZoomPresetImmediate(ZoomPreset preset)
    {
        var top = orbitalFollow.Orbits.Top;
        var center = orbitalFollow.Orbits.Center;
        var bottom = orbitalFollow.Orbits.Bottom;

        top.Radius = preset.topRadius;
        top.Height = preset.topHeight;

        center.Radius = preset.centerRadius;
        center.Height = preset.centerHeight;

        bottom.Radius = preset.bottomRadius;
        bottom.Height = preset.bottomHeight;

        orbitalFollow.Orbits.Top = top;
        orbitalFollow.Orbits.Center = center;
        orbitalFollow.Orbits.Bottom = bottom;
    }

    [Serializable]
    private struct ZoomPreset
    {
        public string name;

        public float topRadius;
        public float topHeight;

        public float centerRadius;
        public float centerHeight;

        public float bottomRadius;
        public float bottomHeight;

        public ZoomPreset(
            string name,
            float topRadius,
            float topHeight,
            float centerRadius,
            float centerHeight,
            float bottomRadius,
            float bottomHeight)
        {
            this.name = name;

            this.topRadius = topRadius;
            this.topHeight = topHeight;

            this.centerRadius = centerRadius;
            this.centerHeight = centerHeight;

            this.bottomRadius = bottomRadius;
            this.bottomHeight = bottomHeight;
        }
    }

    public void SetZoomState(CameraZoomState state)
    {
        SetZoomIndex((int)state);
    }

    public void SetZoomIndex(int index)
    {
        if (zoomPresets == null || zoomPresets.Length == 0)
            return;

        int clampedIndex = Mathf.Clamp(index, 0, zoomPresets.Length - 1);

        if (clampedIndex == targetZoomIndex)
            return;

        previousZoomIndex = targetZoomIndex;
        targetZoomIndex = clampedIndex;

        UpdateDebugZoomState();
    }
    public void ReturnToPreviousZoomState()
    {
        if (previousZoomIndex < 0)
            return;

        int indexToReturn = previousZoomIndex;

        previousZoomIndex = targetZoomIndex;
        targetZoomIndex = indexToReturn;

        UpdateDebugZoomState();
    }


    private void UpdateDebugZoomState()
    {
        currentZoomIndex = targetZoomIndex;

        if (zoomPresets == null || zoomPresets.Length == 0)
        {
            currentZoomState = "No zoom presets";
            return;
        }

        currentZoomState = zoomPresets[targetZoomIndex].name;
    }


    //FIX
}

public enum CameraZoomState
{
    Top,
    Wide,
    Medium,
    Narrow
}