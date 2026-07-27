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

    [Header("Ustawienia")]
    public bool isTutorialActive = false;

    // Zbiór ID tutoriali, które ju¿ by³y (HashSet jest szybki, ale nie zapisuje siê w JSON)
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
        // Logika zamykania tutoriala klikniêciem
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
        // Jeœli ID jest w zbiorze, to znaczy, ¿e ju¿ to widzieliœmy -> wychodzimy
        if (pokazaneTutoriale.Contains(unikalneID)) return;

        // Dodajemy do zbioru "widzianych"
        pokazaneTutoriale.Add(unikalneID);

        if (tutorialText != null) tutorialText.text = tresc;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        isTutorialActive = true;
        Time.timeScale = 0f; // Pauza gry

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ZamknijTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        isTutorialActive = false;
        Time.timeScale = 1f; // Wznowienie gry

        blockWorldInputUntilMouseRelease = true;
        StartCoroutine(ReleaseWorldInputAfterMouseRelease());


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
        PokazTutorial("Wciœnij Lewy Przycisk Myszy, aby siê poruszyæ, Wciœnij Lewy Shift, aby wejœæ w tryb skupienia.", "MoveTutorial");
    }



    // --- FUNKCJE DLA SYSTEMU ZAPISU (NOWOŒÆ) ---

    // 1. Daj mi listê tego co widzia³em (dla SaveLoadManagera)
    public List<string> GetShownTutorials()
    {
        return new List<string>(pokazaneTutoriale);
    }

    // 2. Przywróæ listê tego co widzia³em (od SaveLoadManagera)
    public void RestoreShownTutorials(List<string> loadedList)
    {
        if (loadedList != null)
        {
            pokazaneTutoriale = new HashSet<string>(loadedList);
        }
    }
}