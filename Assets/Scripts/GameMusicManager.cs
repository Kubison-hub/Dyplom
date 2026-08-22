using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Automatycznie doda AudioSource do obiektu
public class GameMusicManager : MonoBehaviour
{
    [Header("Playlista")]
    [Tooltip("Przeci¹gnij tutaj swoje 3 utwory (lub wiêcej)")]
    public AudioClip[] utwory;

    private AudioSource audioSource;
    private int aktualnyIndeks = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Wa¿ne: Wy³¹czamy domyœlne zapêtlanie pojedynczego utworu,
        // bo chcemy zapêtlaæ ca³¹ playlistê, a nie jeden plik.
        audioSource.loop = false;

        GrajKolejnyUtwor();
    }

    void Update()
    {
        // Sprawdzamy, czy muzyka przesta³a graæ
        if (!audioSource.isPlaying && utwory.Length > 0)
        {
            GrajKolejnyUtwor();
        }
    }

    private void GrajKolejnyUtwor()
    {
        if (utwory.Length == 0) return;

        audioSource.loop = false;

        // Ustawiamy klip w AudioSource na ten z obecnego indeksu
        audioSource.clip = utwory[aktualnyIndeks];
        audioSource.Play();

        // Przesuwamy indeks do przodu
        aktualnyIndeks++;

        // Jeœli wyszliœmy poza listê utworów, wracamy na pocz¹tek (indeks 0)
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
    }
    // Tê funkcjê wykorzystamy w przysz³oœci do zmiany muzyki na konkretn¹ (system warunkowy)
    public void ZmienUtworWymuszenie(AudioClip nowyUtwor)
    {
        audioSource.Stop();
        audioSource.clip = nowyUtwor;
        audioSource.Play();
        audioSource.loop = true; // Wtedy pewnie bêdziemy chcieli go zapêtliæ
    }
}