using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;

public class NotebookManager : MonoBehaviour
{
    public static NotebookManager Instance;
    [Header("UI Panels")]
    public GameObject notebookPanel;
    public GameObject categoryPanel;
    public GameObject noteListPanel;
    public GameObject noteDisplayArea;
    public GameObject taskDisplayArea; // NOWE: Zmienna dla TaskDisplayArea

    [Header("Tasks")]
    [SerializeField] private CluesLog cluesLog;
    [SerializeField] private TextMeshProUGUI taskTitleText;
    [SerializeField] private TextMeshProUGUI taskDescriptionText;
    [FormerlySerializedAs("taskDisplayText")]
    [SerializeField] private TextMeshProUGUI taskObjectivesText;

    [Header("List Settings")]
    public Transform noteListContent;

    [Header("Category Buttons")]
    [SerializeField] private Button observationsButton;
    [SerializeField] private Button peopleButton;
    [SerializeField] private Button objectsButton;

    [Header("Audio")]
    [SerializeField] private AudioSource notebookAudioSource;
    [SerializeField] private AudioClip openNotebookClip;
    [SerializeField] private AudioClip closeNotebookClip;
    [SerializeField] private AudioClip buttonClickClip;

    [Header("Prefabs & UI Elements")]
    public GameObject noteButtonPrefab;
    public TextMeshProUGUI displayTitle;
    public TextMeshProUGUI displayContent;
    public Image paperImage;

    [Header("Database")]
    public List<NoteData> allNotes;

    private readonly HashSet<NoteData> unreadNotes = new HashSet<NoteData>();

    private PlayerInput notebookLockedInput;
    private bool notebookDisabledInput;
    private bool openedAsQuickRead;
    private bool notebookPausedTime;

    public bool IsNotebookOpen => notebookPanel != null && notebookPanel.activeSelf;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (notebookAudioSource == null)
            notebookAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        notebookPanel.SetActive(false);
        ConnectTaskLog();
        RefreshTaskDisplay();
        RefreshCategoryButtons();
    }

    private void OnDestroy()
    {
        if (cluesLog != null)
            cluesLog.OnLogUpdated -= UpdateTaskDisplay;

        ResumeGameplayTime();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleNotebook();
        }
    }

    public void ToggleNotebook()
    {
        if (IsNotebookOpen)
        {
            CloseNotebook();
            return;
        }

        PlayerTopText.Instance?.ClearAllTopText();
        notebookPanel.SetActive(true);
        LockActivePlayerInput();
        PauseGameplayTime();
        RefreshCategoryButtons();
        ShowDefaultNotebookView();
        PlaySound(openNotebookClip);
    }

    // Funkcja do fizycznego przycisku zamykania Notatnika
    public void CloseNotebook()
    {
        if (notebookPanel != null)
            notebookPanel.SetActive(false);

        openedAsQuickRead = false;
        RestoreActivePlayerInput();
        ResumeGameplayTime();
        PlaySound(closeNotebookClip);
    }

    private void LockActivePlayerInput()
    {
        if (notebookDisabledInput || SwitchCharacter.Instance == null)
            return;

        PlayerInput[] players = SwitchCharacter.Instance.players;
        int activeIndex = SwitchCharacter.Instance.activePlayerIndex;
        if (players == null || activeIndex < 0 || activeIndex >= players.Length)
            return;

        PlayerInput activePlayerInput = players[activeIndex];
        if (activePlayerInput == null || !activePlayerInput.inputIsActive)
            return;

        notebookLockedInput = activePlayerInput;
        notebookLockedInput.DeactivateInput();
        notebookDisabledInput = true;
    }

    private void RestoreActivePlayerInput()
    {
        if (!notebookDisabledInput)
            return;

        if (notebookLockedInput != null)
            notebookLockedInput.ActivateInput();

        notebookLockedInput = null;
        notebookDisabledInput = false;
    }

    // Wywoływane przez przyciski kategorii (0, 1, 2)
    public void ShowCategory(int categoryIndex)
    {
        PlayButtonClickSound();

        NoteCategory selectedCategory = (NoteCategory)categoryIndex;

        if (selectedCategory == NoteCategory.Zadania)
        {
            ShowTasks();
            return;
        }

        categoryPanel.SetActive(false);
        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);

        // NOWE: Ukrywamy TaskDisplayArea po wejściu w kategorię
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);

        // Wyczyść starą listę
        foreach (Transform child in noteListContent)
        {
            Destroy(child.gameObject);
        }

        NoteData firstNote = null;

        // Generuj nową listę dla wybranej kategorii.
        foreach (NoteData note in allNotes)
        {
            if (note.category == selectedCategory)
            {
                CreateNoteButton(note);

                if (firstNote == null)
                    firstNote = note;
            }
        }

        // A category opens directly on its first available note.
        if (firstNote != null)
            DisplayNote(firstNote);
    }

    public void BackToCategories()
    {
        if (openedAsQuickRead)
        {
            CloseNotebook();
            return;
        }

        PlayButtonClickSound();
        ShowDefaultNotebookView();
    }

    private void ShowDefaultNotebookView()
    {
        categoryPanel.SetActive(true);
        noteListPanel.SetActive(false);
        noteDisplayArea.SetActive(false);

        // NOWE: Wyświetlamy TaskDisplayArea jako domyślny widok wraz z kategoriami
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(true);

        RefreshTaskDisplay();
    }

    public void ShowTasks()
    {
        categoryPanel.SetActive(true);
        noteListPanel.SetActive(false);
        noteDisplayArea.SetActive(false);

        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(true);

        RefreshTaskDisplay();
    }

    public void BackToNoteList()
    {
        if (openedAsQuickRead)
        {
            CloseNotebook();
            return;
        }

        PlayButtonClickSound();
        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);

        // NOWE: Upewniamy się, że wracając do listy, TaskDisplayArea jest ukryty
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);
    }

    private void CreateNoteButton(NoteData note)
    {
        GameObject newBtn = Instantiate(noteButtonPrefab, noteListContent);
        TextMeshProUGUI titleText = FindNoteButtonTitle(newBtn);
        if (titleText != null)
            titleText.text = note.noteTitle;

        Button noteButton = newBtn.GetComponent<Button>();
        if (noteButton != null)
        {
            noteButton.onClick.AddListener(() =>
            {
                OpenNote(note);
                SetNewEntryIndicatorVisible(newBtn, false);
            });
        }

        SetNewEntryIndicatorVisible(newBtn, unreadNotes.Contains(note));
    }

    private void OpenNote(NoteData note)
    {
        MarkNoteAsRead(note);
        PlayButtonClickSound();
        DisplayNote(note);
    }

    private void DisplayNote(NoteData note)
    {
        if (note == null)
            return;

        // ZMIANA: Usunięto ukrywanie noteListPanel, aby lista notatek do wyboru pozostała widoczna na ekranie.
        // noteListPanel.SetActive(false); <-- To powodowało znikanie listy

        noteDisplayArea.SetActive(true);

        // Zabezpieczenie, aby TaskDisplayArea był wyłączony podczas czytania notatki
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);

        displayTitle.text = note.noteTitle;
        displayContent.text = note.content;

        if (note.customPaperGraphic != null)
        {
            paperImage.sprite = note.customPaperGraphic;
        }
    }

    public void ShowNoteImmediately(NoteData note)
    {
        if (note == null || notebookPanel == null)
            return;

        AddNote(note);
        openedAsQuickRead = true;

        if (!IsNotebookOpen)
        {
            PlayerTopText.Instance?.ClearAllTopText();
            notebookPanel.SetActive(true);
            LockActivePlayerInput();
            PauseGameplayTime();
            PlaySound(openNotebookClip);
        }

        if (categoryPanel != null)
            categoryPanel.SetActive(false);

        DisplayNote(note);
    }

    public void AddNote(NoteData note)
    {
        if (note == null)
            return;

        if (allNotes == null)
            allNotes = new List<NoteData>();

        if (allNotes.Contains(note))
            return;

        allNotes.Insert(0, note);
        unreadNotes.Add(note);
        Debug.Log("DODANO NOTATKE: " + note.noteTitle, this);
        RefreshCategoryButtons();
    }

    private void RefreshCategoryButtons()
    {
        SetCategoryButtonState(observationsButton, NoteCategory.Obserwacje);
        SetCategoryButtonState(peopleButton, NoteCategory.Osoby);
        SetCategoryButtonState(objectsButton, NoteCategory.Obiekty);
    }

    private void SetCategoryButtonState(Button button, NoteCategory category)
    {
        if (button == null)
            return;

        button.interactable = HasNoteInCategory(category);
        SetNewEntryIndicatorVisible(button.gameObject, HasUnreadNoteInCategory(category));
    }

    private bool HasNoteInCategory(NoteCategory category)
    {
        if (allNotes == null)
            return false;

        foreach (NoteData note in allNotes)
        {
            if (note != null && note.category == category)
                return true;
        }

        return false;
    }

    private bool HasUnreadNoteInCategory(NoteCategory category)
    {
        foreach (NoteData note in unreadNotes)
        {
            if (note != null && note.category == category)
                return true;
        }

        return false;
    }

    private void MarkNoteAsRead(NoteData note)
    {
        if (note != null && unreadNotes.Remove(note))
            RefreshCategoryButtons();
    }

    private static TextMeshProUGUI FindNoteButtonTitle(GameObject noteButton)
    {
        if (noteButton == null)
            return null;

        foreach (TextMeshProUGUI text in noteButton.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text != null && text.name.IndexOf("asterix", System.StringComparison.OrdinalIgnoreCase) < 0)
                return text;
        }

        return null;
    }

    private static void SetNewEntryIndicatorVisible(GameObject root, bool visible)
    {
        if (root == null)
            return;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.IndexOf("asterix", System.StringComparison.OrdinalIgnoreCase) >= 0)
                child.gameObject.SetActive(visible);
        }
    }

    public void PlayButtonClickSound()
    {
        PlaySound(buttonClickClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (notebookAudioSource != null && clip != null)
            notebookAudioSource.PlayOneShot(clip);
    }

    private void PauseGameplayTime()
    {
        if (notebookPausedTime)
            return;

        GameplayTimePause.Pause(this);
        notebookPausedTime = true;
    }

    private void ResumeGameplayTime()
    {
        if (!notebookPausedTime)
            return;

        GameplayTimePause.Resume(this);
        notebookPausedTime = false;
    }

    private void ConnectTaskLog()
    {
        if (cluesLog == null)
            cluesLog = FindFirstObjectByType<CluesLog>();

        if (cluesLog != null)
            cluesLog.OnLogUpdated += UpdateTaskDisplay;
    }

    private void RefreshTaskDisplay()
    {
        if (cluesLog == null)
            ConnectTaskLog();

        if (cluesLog != null)
            UpdateTaskDisplay(cluesLog.GetFormattedLog());
    }

    private void UpdateTaskDisplay(string _)
    {
        if (cluesLog == null)
            return;

        if (taskTitleText != null)
            taskTitleText.text = cluesLog.Title;

        if (taskDescriptionText != null)
        {
            bool hasDescription = !string.IsNullOrWhiteSpace(cluesLog.Description);
            taskDescriptionText.gameObject.SetActive(hasDescription);

            if (hasDescription)
                taskDescriptionText.text = cluesLog.Description;
        }

        if (taskObjectivesText != null)
            taskObjectivesText.text = cluesLog.GetObjectivesText();
    }
}
