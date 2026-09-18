using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class FireLightFlicker : MonoBehaviour
{
    [Header("Migotanie intensywnosci")]
    [SerializeField, Tooltip("Wlacza nieregularne zmiany jasnosci swiatla.")]
    private bool flickerIntensity = true;

    [SerializeField, Min(0f), Tooltip("Maksymalne odchylenie od intensywnosci zapisanej przy uruchomieniu.")]
    private float intensityVariation = 0.25f;

    [SerializeField, Min(0.01f), Tooltip("Szybkosc zmian jasnosci.")]
    private float intensityNoiseSpeed = 3.5f;

    [Header("Ruch zrodla i cienia")]
    [SerializeField, Tooltip("Delikatnie przesuwa zrodlo swiatla, dzieki czemu poruszaja sie cienie.")]
    private bool moveLight = true;

    [SerializeField, Tooltip("Maksymalny ruch lokalny na kazdej osi. Dla kominka zwykle wystarcza 0.02-0.06 m.")]
    private Vector3 movementAmplitude = new Vector3(0.035f, 0.045f, 0.025f);

    [SerializeField, Min(0.01f), Tooltip("Szybkosc nieregularnego ruchu zrodla swiatla.")]
    private float movementNoiseSpeed = 1.8f;

    [Header("Zmiana barwy")]
    [SerializeField, Tooltip("Wlacza subtelne przechodzenie pomiedzy dwoma kolorami ognia.")]
    private bool flickerColor;

    [SerializeField, ColorUsage(false, true)]
    private Color coolerColor = new Color(1f, 0.42f, 0.12f, 1f);

    [SerializeField, ColorUsage(false, true)]
    private Color hotterColor = new Color(1f, 0.72f, 0.3f, 1f);

    [SerializeField, Range(0f, 1f), Tooltip("Jak mocno kolory ognia zastapia bazowy kolor Light.")]
    private float colorVariation = 0.35f;

    [SerializeField, Min(0.01f), Tooltip("Szybkosc zmian barwy.")]
    private float colorNoiseSpeed = 1.25f;

    private Light controlledLight;
    private Vector3 baseLocalPosition;
    private float baseIntensity;
    private Color baseColor;
    private Vector3 noiseOffset;
    private bool baseStateCaptured;

    private void Awake()
    {
        controlledLight = GetComponent<Light>();
        CaptureBaseState();

        float seed = Mathf.Abs(GetInstanceID()) * 0.0137f;
        noiseOffset = new Vector3(seed + 11.3f, seed + 37.7f, seed + 73.1f);
    }

    private void OnEnable()
    {
        if (controlledLight == null)
            controlledLight = GetComponent<Light>();

        CaptureBaseState();
    }

    private void LateUpdate()
    {
        if (controlledLight == null)
            return;

        float time = Time.time;

        controlledLight.intensity = flickerIntensity
            ? Mathf.Max(0f, baseIntensity + SignedNoise(noiseOffset.x, time * intensityNoiseSpeed) * intensityVariation)
            : baseIntensity;

        if (moveLight)
        {
            float movementTime = time * movementNoiseSpeed;
            Vector3 offset = new Vector3(
                SignedNoise(noiseOffset.x + 101f, movementTime) * movementAmplitude.x,
                SignedNoise(noiseOffset.y + 211f, movementTime * 1.07f) * movementAmplitude.y,
                SignedNoise(noiseOffset.z + 307f, movementTime * 0.93f) * movementAmplitude.z);

            transform.localPosition = baseLocalPosition + offset;
        }
        else
        {
            transform.localPosition = baseLocalPosition;
        }

        if (flickerColor)
        {
            float colorBlend = Mathf.PerlinNoise(noiseOffset.z, time * colorNoiseSpeed);
            Color fireColor = Color.Lerp(coolerColor, hotterColor, colorBlend);
            controlledLight.color = Color.Lerp(baseColor, fireColor, colorVariation);
        }
        else
        {
            controlledLight.color = baseColor;
        }
    }

    private void OnDisable()
    {
        RestoreBaseState();
    }

    [ContextMenu("Zapisz aktualne ustawienia jako bazowe")]
    private void CaptureBaseState()
    {
        if (controlledLight == null)
            return;

        baseLocalPosition = transform.localPosition;
        baseIntensity = controlledLight.intensity;
        baseColor = controlledLight.color;
        baseStateCaptured = true;
    }

    private void RestoreBaseState()
    {
        if (!baseStateCaptured || controlledLight == null)
            return;

        transform.localPosition = baseLocalPosition;
        controlledLight.intensity = baseIntensity;
        controlledLight.color = baseColor;
    }

    private static float SignedNoise(float offset, float time)
    {
        return Mathf.PerlinNoise(offset, time) * 2f - 1f;
    }
}
