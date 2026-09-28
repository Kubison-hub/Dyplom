using UnityEngine;

[DisallowMultipleComponent]
public sealed class MainMenuHandheldCamera : MonoBehaviour
{
    [Header("Position Wiggle")]
    [SerializeField] private Vector3 positionAmplitude = new Vector3(0.015f, 0.01f, 0.008f);
    [SerializeField, Min(0f)] private float positionFrequency = 0.35f;

    [Header("Rotation Wiggle")]
    [SerializeField] private Vector3 rotationAmplitude = new Vector3(0.2f, 0.3f, 0.1f);
    [SerializeField, Min(0f)] private float rotationFrequency = 0.25f;

    [Header("Motion")]
    [Tooltip("Higher values follow the generated motion more tightly. Set to 0 for no smoothing.")]
    [SerializeField, Min(0f)] private float smoothing = 8f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool restoreTransformOnDisable = true;

    [Header("Noise")]
    [SerializeField] private int noiseSeed = 1926;

    private readonly float[] noiseOffsets = new float[6];
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 currentPositionOffset;
    private Vector3 currentRotationOffset;
    private float elapsedTime;
    private bool initialized;

    private void Awake()
    {
        CaptureCurrentTransform();
        InitializeNoise();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            CaptureCurrentTransform();
            InitializeNoise();
        }

        elapsedTime = 0f;
        currentPositionOffset = Vector3.zero;
        currentRotationOffset = Vector3.zero;
    }

    private void LateUpdate()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsedTime += deltaTime;

        Vector3 targetPositionOffset = Vector3.Scale(SampleNoise(positionFrequency, 0), positionAmplitude);
        Vector3 targetRotationOffset = Vector3.Scale(SampleNoise(rotationFrequency, 3), rotationAmplitude);
        float blend = smoothing > 0f ? 1f - Mathf.Exp(-smoothing * deltaTime) : 1f;

        currentPositionOffset = Vector3.Lerp(currentPositionOffset, targetPositionOffset, blend);
        currentRotationOffset = Vector3.Lerp(currentRotationOffset, targetRotationOffset, blend);

        transform.localPosition = baseLocalPosition + currentPositionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentRotationOffset);
    }

    private void OnDisable()
    {
        if (!initialized || !restoreTransformOnDisable)
            return;

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
    }

    [ContextMenu("Capture Current Transform As Base")]
    public void CaptureCurrentTransform()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        initialized = true;
    }

    private void InitializeNoise()
    {
        var random = new System.Random(noiseSeed);
        for (int i = 0; i < noiseOffsets.Length; i++)
            noiseOffsets[i] = (float)random.NextDouble() * 1000f;
    }

    private Vector3 SampleNoise(float frequency, int offsetIndex)
    {
        float time = elapsedTime * frequency;
        return new Vector3(
            ToSignedNoise(noiseOffsets[offsetIndex], time),
            ToSignedNoise(noiseOffsets[offsetIndex + 1], time),
            ToSignedNoise(noiseOffsets[offsetIndex + 2], time));
    }

    private static float ToSignedNoise(float offset, float time)
    {
        return Mathf.PerlinNoise(offset, time) * 2f - 1f;
    }
}
