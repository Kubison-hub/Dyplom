using UnityEngine;
using UnityEngine.UI; // Potrzebne do obs³ugi Buttonów

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance; // Singleton

    [Header("UI Elementy")]
    public GameObject journalWindow; // Ca³y panel notatnika (JournalPanel)
    public GameObject[] pages;       // Lista stron (Page_1, Page_2...)
    public Button nextBtn;           // Guzik Dalej
    public Button prevBtn;           // Guzik Wstecz
    public Button closeBtn;          // Guzik Zamknij

    [Header("Stan")]
    public bool isJournalOpen = false;
    private int currentPageIndex = 0;

    private void Awake()
    {
        // Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Upewnij siê, ¿e okno jest zamkniête na starcie
        if (journalWindow != null)
            journalWindow.SetActive(false);
    }

    private void Start()
    {
        // Przypisz funkcje do guzików
        if (nextBtn) nextBtn.onClick.AddListener(NextPage);
        if (prevBtn) prevBtn.onClick.AddListener(PrevPage);
        if (closeBtn) closeBtn.onClick.AddListener(ToggleJournal);

        UpdateUI(); // Odœwie¿ widok na start
    }

    private void Update()
    {
        // Otwieranie/zamykanie klawiszem 'N'
        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        isJournalOpen = !isJournalOpen;
        journalWindow.SetActive(isJournalOpen);

        if (isJournalOpen)
        {
            // Przy otwarciu zawsze wracamy na 1 stronê (opcjonalne)
            currentPageIndex = 0;
            UpdateUI();
        }
    }

    public void NextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            UpdateUI();
        }
    }

    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // 1. Poka¿ tylko aktualn¹ stronê, resztê ukryj
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }

        // 2. Zarz¹dzaj aktywnoœci¹ przycisków (np. nie mo¿na klikn¹æ "Wstecz" na 1 stronie)
        if (prevBtn) prevBtn.interactable = (currentPageIndex > 0);
        if (nextBtn) nextBtn.interactable = (currentPageIndex < pages.Length - 1);
    }
}