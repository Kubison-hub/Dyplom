using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Konieczne do u¿ywania Listy

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance;

    [Header("UI Elementy")]
    public GameObject journalWindow;
    public GameObject[] pages;
    public Button nextBtn;
    public Button prevBtn;
    public Button closeBtn;

    [Header("Notatki do odkrycia")]
    public GameObject[] unlockableNotes;

    [Header("Ustawienia")]
    public KeyCode openKey = KeyCode.N;
    public bool isJournalOpen = false;

    // --- NOWOŒÆ: Lista indeksów odkrytych notatek (Dla Systemu Zapisu) ---
    // SaveLoadManager weŸmie tê listê i zapisze j¹ do pliku.
    public List<int> unlockedNoteIndices = new List<int>();
    // ---------------------------------------------------------------------

    private int currentPageIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (journalWindow != null) journalWindow.SetActive(false);
    }

    private void Start()
    {
        if (nextBtn) nextBtn.onClick.AddListener(NextPage);
        if (prevBtn) prevBtn.onClick.AddListener(PrevPage);
        if (closeBtn) closeBtn.onClick.AddListener(ToggleJournal);

        // Dodatkowo: Upewniamy siê, ¿e na starcie notatki s¹ ukryte 
        // (chyba ¿e zostan¹ wczytane przez SaveLoadManager chwilê póŸniej)
        if (unlockableNotes != null)
        {
            foreach (var note in unlockableNotes)
            {
                if (note != null) note.SetActive(false);
            }
        }

        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        isJournalOpen = !isJournalOpen;

        if (journalWindow != null)
        {
            journalWindow.SetActive(isJournalOpen);
        }

        if (isJournalOpen)
        {
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
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == currentPageIndex);
        }

        if (prevBtn) prevBtn.interactable = (currentPageIndex > 0);
        if (nextBtn) nextBtn.interactable = (currentPageIndex < pages.Length - 1);
    }

    public void UnlockNote(int noteIndex)
    {
        if (unlockableNotes != null && noteIndex >= 0 && noteIndex < unlockableNotes.Length)
        {
            if (unlockableNotes[noteIndex] != null)
            {
                // 1. Wizualne w³¹czenie notatki
                unlockableNotes[noteIndex].SetActive(true);

                // --- NOWOŒÆ: Zapamiêtanie faktu odkrycia ---
                // Dodajemy numer notatki do listy, ¿eby zapisaæ to w grze
                if (!unlockedNoteIndices.Contains(noteIndex))
                {
                    unlockedNoteIndices.Add(noteIndex);
                }
                // -------------------------------------------
            }
        }
    }
}