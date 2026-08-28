using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Automatycznie doda AudioSource do obiektu
public class GameMusicManager : MonoBehaviour
{
    [Header("Playlista")]
    [Tooltip("Przeciągnij tutaj swoje 3 utwory (lub więcej)")]
    public AudioClip[] utwory;

    private AudioSource audioSource;
    private int aktualnyIndeks = 0;
    private bool trackWasPlaying;
    private float lastPlaybackTime;
    private const float TrackEndTolerance = 0.1f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Ważne: Wyłączamy domyślne zapętlanie pojedynczego utworu,
        // bo chcemy zapętlać całą playlistę, a nie jeden plik.
        audioSource.loop = false;

        GrajKolejnyUtwor();
    }

    void Update()
    {
        if (audioSource == null || utwory == null || utwory.Length == 0)
            return;

        // AudioSource reports isPlaying as false while AudioListener is paused.
        // That must not be interpreted as the end of the current playlist track.
        if (AudioListener.pause)
            return;

        if (audioSource.isPlaying)
        {
            trackWasPlaying = true;
            lastPlaybackTime = audioSource.time;
            return;
        }

        if (!trackWasPlaying || audioSource.loop || audioSource.clip == null)
            return;

        bool trackFinished = lastPlaybackTime >= audioSource.clip.length - TrackEndTolerance;
        if (!trackFinished)
            return;

        GrajKolejnyUtwor();
    }

    private void GrajKolejnyUtwor()
    {
        if (utwory.Length == 0) return;

        audioSource.loop = false;

        // Ustawiamy klip w AudioSource na ten z obecnego indeksu
        audioSource.clip = utwory[aktualnyIndeks];
        audioSource.Play();
        trackWasPlaying = false;
        lastPlaybackTime = 0f;

        // Przesuwamy indeks do przodu
        aktualnyIndeks++;

        // Jeśli wyszliśmy poza listę utworów, wracamy na początek (indeks 0)
        if (aktualnyIndeks >= utwory.Length)
        {
            aktualnyIndeks = 0;
        }
    }

    // Odtwarza wybrany utwór. Gdy loop jest wylaczone, playlista wznawia sie po jego zakonczeniu.
    public void ChangeMusic(AudioClip newTrack, bool loop = false)
    {
        if (newTrack == null)
            return;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.Stop();
        audioSource.clip = newTrack;
        audioSource.loop = loop;
        audioSource.Play();
        trackWasPlaying = false;
        lastPlaybackTime = 0f;
    }
    // Tę funkcję wykorzystamy w przyszłości do zmiany muzyki na konkretną (system warunkowy)
    public void ZmienUtworWymuszenie(AudioClip nowyUtwor)
    {
        audioSource.Stop();
        audioSource.clip = nowyUtwor;
        audioSource.Play();
        audioSource.loop = true; // Wtedy pewnie będziemy chcieli go zapętlić
        trackWasPlaying = false;
        lastPlaybackTime = 0f;
    }
}
