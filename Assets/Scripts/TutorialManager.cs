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

    public bool BlocksWorldInput => isTutorialActive || blockWorldInputUntilMouseRelease;
    private bool blockWorldInputUntilMouseRelease;



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

    private void Update()
    {
        // Logika zamykania tutoriala kliknięciem
        if (isTutorialActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ZamknijTutorial();
            }
        }
    }

    public void PokazTutorial(string tresc, string unikalneID)
    {
        // Jeśli ID jest w zbiorze, to znaczy, że już to widzieliśmy -> wychodzimy
        if (pokazaneTutoriale.Contains(unikalneID)) return;

        // Dodajemy do zbioru "widzianych"
        pokazaneTutoriale.Add(unikalneID);

        PlayerTopText.Instance?.ClearAllTopText();

        if (tutorialText != null) tutorialText.text = tresc;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        PlayTutorialSound(tutorialOpenClip);

        isTutorialActive = true;
        Time.timeScale = 0f; // Pauza gry

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ZamknijTutorial()
    {
        PlayTutorialSound(tutorialCloseClip);

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        isTutorialActive = false;
        Time.timeScale = 1f; // Wznowienie gry

        blockWorldInputUntilMouseRelease = true;
        StartCoroutine(ReleaseWorldInputAfterMouseRelease());


    }
    private void PlayTutorialSound(AudioClip clip)
    {
        if (tutorialAudioSource != null && clip != null)
            tutorialAudioSource.PlayOneShot(clip);
    }

    private IEnumerator ReleaseWorldInputAfterMouseRelease()
    {
        yield return null;

        while (Input.GetMouseButton(0))
            yield return null;

        blockWorldInputUntilMouseRelease = false;
    }
    private IEnumerator PokazStartowyTutorial()
    {
        
        yield return new WaitForSeconds(0.1f);
        //TutorialTimeline.Instance.ShowGameplayTutorialPopup(0);
        PokazTutorial("Witaj w demie gry \"Sherlock Holmes: Duchy Przeszłości\".\n\nWciśnij Lewy Przycisk Myszy, aby się poruszyć.\nWciśnij Lewy Shift, aby wejść w tryb skupienia.", "MoveTutorial");
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
