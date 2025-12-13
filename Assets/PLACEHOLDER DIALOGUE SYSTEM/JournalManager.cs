using UnityEngine;
using UnityEngine.UI;

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
                unlockableNotes[noteIndex].SetActive(true);
            }
        }
    }
}