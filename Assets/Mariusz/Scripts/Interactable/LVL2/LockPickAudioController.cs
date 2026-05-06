using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LockPickAudioController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private bool audioEnabled = true;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    [Header("Clips")]
    [SerializeField] private AudioClip moveWorkClip;
    [SerializeField] private AudioClip tumblerClickClip;
    [SerializeField] private AudioClip testCompleteClip;
    [SerializeField] private AudioClip resetClip;
    [SerializeField] private AudioClip unlockClip;

    [Header("Move Work")]
    [SerializeField] private float moveWorkCooldown = 0.045f;
    [SerializeField] private Vector2 moveWorkPitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private Vector2 clickPitchRange = new Vector2(0.96f, 1.06f);

    private AudioSource audioSource;
    private float lastMoveWorkTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 3D audio
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 8f;
    }

    public void PlayMoveWork(float distance01, float movement01)
    {
        if (!CanPlay(moveWorkClip))
            return;

        movement01 = Mathf.Clamp01(movement01);

        // Kursor stoi albo prawie stoi — nie graj.
        if (movement01 <= 0.02f)
            return;

        // Im szybszy ruch, tym krótszy cooldown.
        float dynamicCooldown = Mathf.Lerp(
            moveWorkCooldown * 2.2f,
            moveWorkCooldown * 0.45f,
            movement01
        );

        if (Time.time - lastMoveWorkTime < dynamicCooldown)
            return;

        lastMoveWorkTime = Time.time;

        distance01 = Mathf.Clamp01(distance01);

        float volumeFromDistance = Mathf.Lerp(0.20f, 0.60f, distance01);
        float volumeFromMovement = Mathf.Lerp(0.20f, 1.00f, movement01);

        float volume = volumeFromDistance * volumeFromMovement;

        Vector2 pitchRange = new Vector2(
            Mathf.Lerp(moveWorkPitchRange.x, 1.0f, movement01),
            Mathf.Lerp(1.0f, moveWorkPitchRange.y, movement01)
        );

        PlayClip(moveWorkClip, volume, pitchRange);
    }

    public void PlayTumblerClick(int stepIndex)
    {
        if (!CanPlay(tumblerClickClip))
            return;

        float volume = Mathf.Clamp01(0.75f + stepIndex * 0.03f);
        PlayClip(tumblerClickClip, volume, clickPitchRange);
    }

    public void PlayTestComplete()
    {
        if (!CanPlay(testCompleteClip))
            return;

        PlayClip(testCompleteClip, 0.9f, Vector2.one);
    }

    public void PlayReset()
    {
        if (!CanPlay(resetClip))
            return;

        PlayClip(resetClip, 0.9f, Vector2.one);
    }

    public void PlayUnlock()
    {
        if (!CanPlay(unlockClip))
            return;

        PlayClip(unlockClip, 1f, Vector2.one);
    }

    private bool CanPlay(AudioClip clip)
    {
        return audioEnabled && audioSource != null && clip != null;
    }

    private void PlayClip(AudioClip clip, float volume, Vector2 pitchRange)
    {
        float previousPitch = audioSource.pitch;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume * masterVolume);
        audioSource.pitch = previousPitch;
    }
}