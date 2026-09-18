using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Elementy UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;


    [Header("Audio")]
    [Tooltip("Optional source used for every standard tutorial panel sound.")]
    [SerializeField] private AudioSource tutorialAudioSource;
    [SerializeField] private AudioClip tutorialOpenClip;
    [SerializeField] private AudioClip tutorialCloseClip;

    [Header("Ustawienia")]
    public bool isTutorialActive = false;

    // Zbiór ID tutoriali, które już były (HashSet jest szybki, ale nie zapisuje się w JSON)
    private HashSet<string> pokazaneTutoriale = new HashSet<string>();

    public bool BlocksWorldInput => isTutorialActive || blockWorldInputUntilTutorialKeyRelease;
    private bool blockWorldInputUntilTutorialKeyRelease;
    private Coroutine releaseTutorialInputCoroutine;
    private bool activeTutorialPlaysAudio = true;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        // Tutorial startowy (LPM)
        StartCoroutine(PokazStartowyTutorial());
    }

    private void OnDestroy()
    {
        if (releaseTutorialInputCoroutine != null)
            StopCoroutine(releaseTutorialInputCoroutine);

        GameplayTimePause.Resume(this);
    }

    private void Update()
    {
        // Tutorial panels are dismissed only with Tab, so a world click cannot close them.
        if (isTutorialActive)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ZamknijTutorial();
            }
        }
    }

    public void PokazTutorial(string tresc, string unikalneID, bool playAudio = true)
    {
        // Jeśli ID jest w zbiorze, to znaczy, że już to widzieliśmy -> wychodzimy
        if (pokazaneTutoriale.Contains(unikalneID)) return;

        // Dodajemy do zbioru "widzianych"
        pokazaneTutoriale.Add(unikalneID);

        CancelActivePlayerPendingInteraction();
        PlayerTopText.Instance?.ClearAllTopText();
        activeTutorialPlaysAudio = playAudio;

        if (tutorialText != null) tutorialText.text = tresc;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (activeTutorialPlaysAudio)
            PlayTutorialSound(tutorialOpenClip);

        isTutorialActive = true;
        GameplayTimePause.Pause(this);

    }

    private static void CancelActivePlayerPendingInteraction()
    {
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null && switchCharacter.players != null)
        {
            int activeIndex = switchCharacter.activePlayerIndex;
            if (activeIndex >= 0 && activeIndex < switchCharacter.players.Length &&
                switchCharacter.players[activeIndex] != null)
            {
                PlayerController activePlayer = switchCharacter.players[activeIndex]
                    .GetComponent<PlayerController>();
                activePlayer?.CancelPendingInteraction();
                return;
            }
        }

        foreach (PlayerController player in FindObjectsByType<PlayerController>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
            player?.CancelPendingInteraction();
    }

    public void ZamknijTutorial()
    {
        if (activeTutorialPlaysAudio)
            PlayTutorialSound(tutorialCloseClip);

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        isTutorialActive = false;
        activeTutorialPlaysAudio = true;
        GameplayTimePause.Resume(this);

        blockWorldInputUntilTutorialKeyRelease = true;

        if (releaseTutorialInputCoroutine != null)
            StopCoroutine(releaseTutorialInputCoroutine);

        releaseTutorialInputCoroutine = StartCoroutine(ReleaseTutorialInputAfterTabRelease());
    }

    private IEnumerator ReleaseTutorialInputAfterTabRelease()
    {
        // Keep the closing Tab consumed for the rest of this frame.
        yield return null;

        while (Input.GetKey(KeyCode.Tab))
            yield return null;

        blockWorldInputUntilTutorialKeyRelease = false;
        releaseTutorialInputCoroutine = null;
    }
    private void PlayTutorialSound(AudioClip clip)
    {
        if (tutorialAudioSource != null && clip != null)
            tutorialAudioSource.PlayOneShot(clip);
    }

    private IEnumerator PokazStartowyTutorial()
    {
        
        yield return new WaitForSeconds(0.1f);
        //TutorialTimeline.Instance.ShowGameplayTutorialPopup(0);
        PokazTutorial("Witaj w demie gry \"Ghosts of the Past\"\n\nWciśnij Lewy Przycisk Myszy, aby się poruszyć. \n Wciśnij Prawy Przycisk Myszy, aby poruszyć kamerą. \n Użyj kółka myszy aby oddalić/przybliżyć kamerę. \nWciśnij Lewy Shift, aby wejść w tryb skupienia.", "MoveTutorial");
    }



    // --- FUNKCJE DLA SYSTEMU ZAPISU (NOWOŚĆ) ---

    // 1. Daj mi listę tego co widziałem (dla SaveLoadManagera)
    public List<string> GetShownTutorials()
    {
        return new List<string>(pokazaneTutoriale);
    }

    // 2. Przywróć listę tego co widziałem (od SaveLoadManagera)
    public void RestoreShownTutorials(List<string> loadedList)
    {
        if (loadedList != null)
        {
            pokazaneTutoriale = new HashSet<string>(loadedList);
        }
    }
}

/// <summary>
/// Coordinates UI-driven gameplay pauses so closing one panel cannot resume
/// the world while another pause-owning panel remains open.
/// </summary>
public static class GameplayTimePause
{
    private static readonly HashSet<int> pauseOwners = new HashSet<int>();
    private static float timeScaleBeforePause = 1f;

    public static void Pause(Object owner)
    {
        if (owner == null)
            return;

        int ownerId = owner.GetInstanceID();
        if (pauseOwners.Count == 0)
            timeScaleBeforePause = Time.timeScale;

        pauseOwners.Add(ownerId);
        Time.timeScale = 0f;
    }

    public static void Resume(Object owner)
    {
        if (owner == null || !pauseOwners.Remove(owner.GetInstanceID()))
            return;

        if (pauseOwners.Count == 0)
            Time.timeScale = timeScaleBeforePause;
    }
}
