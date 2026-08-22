using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Próbki DŸwiêkowe")]
    [Tooltip("Dodaj tutaj 3-4 pliki pojedynczych kroków")]
    public AudioClip[] footstepClips;

    [Range(0f, 1f)]
    public float baseVolume = 0.8f;

    [Header("Losowoœæ (Dla naturalnoœci)")]
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    public float volumeRandomization = 0.15f;

    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource != null)
        {
            // Upewniamy siê, ¿e AudioSource jest poprawnie skonfigurowane do kroków
            audioSource.spatialBlend = 1f; // Pe³ne 3D
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
        else
        {
            Debug.LogError("Audio source jest null");
        }
    }

    /// <summary>
    /// Metoda wywo³ywana przez Animation Event w oknie animacji.
    /// </summary>
    public void OnFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            // Jawne wskazanie UnityEngine.Debug
            UnityEngine.Debug.LogWarning("Brak przypisanych dŸwiêków kroków w PlayerFootstepAudio!");
            return;
        }

        // 1. Losowanie próbki (Jawne wskazanie UnityEngine.Random)
        int randomIndex = UnityEngine.Random.Range(0, footstepClips.Length);
        AudioClip clipToPlay = footstepClips[randomIndex];

        // 2. Losowa zmiana tonacji i g³oœnoœci (Jawne wskazanie UnityEngine.Random)
        audioSource.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        float currentVolume = baseVolume - UnityEngine.Random.Range(0f, volumeRandomization);

        // 3. Odtworzenie dŸwiêku bez przerywania poprzedniego (pozwala na naturalne wybrzmienie)
        audioSource.PlayOneShot(clipToPlay, Mathf.Clamp01(currentVolume));
    }
}