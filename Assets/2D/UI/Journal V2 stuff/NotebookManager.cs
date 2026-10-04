using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;

public class NotebookManager : MonoBehaviour
{
    private static readonly System.Text.RegularExpressions.Regex PolishOrphanPattern =
        new System.Text.RegularExpressions.Regex(@"(?<!\S)([AaIiOoUuWwZz]) ", System.Text.RegularExpressions.RegexOptions.Compiled);

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

    [Header("People View")]
    [SerializeField] private GameObject personFactsContent;
    [SerializeField] private TextMeshProUGUI personFactPrefab;

    [Header("Observations View")]
    [SerializeField] private GameObject observationFactsContent;
    [SerializeField] private TextMeshProUGUI observationFactPrefab;

    [Header("Database")]
    public List<NoteData> allNotes;

    private readonly HashSet<NoteData> unreadNotes = new HashSet<NoteData>();

    private PlayerInput notebookLockedInput;
    private bool notebookDisabledInput;
    private bool openedAsQuickRead;
    private bool quickReadClosesOnLeftClick;
    private bool hasQuickReadPanelPosition;
    private Vector2 panelPositionBeforeQuickRead;
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
        if (Input.GetKeyDown(KeyCode.Tab) && !IsTutorialBlockingNotebook())
        {
            ToggleNotebook();
        }

        // Szybki podglad notatki otwarty z flaga closeOnLeftMouseClick
        // zamyka sie pierwszym kliknieciem lewym przyciskiem.
        if (openedAsQuickRead && quickReadClosesOnLeftClick && IsNotebookOpen &&
            Input.GetMouseButtonDown(0))
        {
            CloseNotebook();
        }
    }

    public void ToggleNotebook()
    {
        if (IsTutorialBlockingNotebook())
            return;

        if (!IsNotebookOpen && DialogueEditor.ConversationManager.Instance != null &&
            DialogueEditor.ConversationManager.Instance.IsConversationActive)
        {
            return;
        }

        if (IsNotebookOpen)
        {
            CloseNotebook();
            return;
        }

        PlayerTopText.Instance?.ClearAllTopText();
        HideQuestLogForNotebook();
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
        if (IsTutorialBlockingNotebook())
            return;

        if (notebookPanel != null)
            notebookPanel.SetActive(false);

        openedAsQuickRead = false;
        quickReadClosesOnLeftClick = false;

        // Przywracamy pozycje panelu sprzed szybkiego podgladu.
        if (hasQuickReadPanelPosition)
        {
            RectTransform panelRect = notebookPanel != null
                ? notebookPanel.transform as RectTransform
                : null;

            if (panelRect != null)
                panelRect.anchoredPosition = panelPositionBeforeQuickRead;

            hasQuickReadPanelPosition = false;
        }

        RestoreQuestLogAfterNotebook();
        RestoreActivePlayerInput();
        ResumeGameplayTime();
        PlaySound(closeNotebookClip);
    }

    private static bool IsTutorialBlockingNotebook()
    {
        return TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput ||
               TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput;
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

        if (selectedCategory == NoteCategory.Osoby)
        {
            NotebookPerson firstPerson = NotebookPerson.None;
            HashSet<NotebookPerson> displayedPeople = new HashSet<NotebookPerson>();

            foreach (NoteData note in allNotes)
            {
                // Pusty wpis na liscie wywracal cale okno notatnika (NullReference).
                if (note == null)
                    continue;

                if (!IsPersonNote(note) || !displayedPeople.Add(note.person))
                    continue;

                CreatePersonButton(note.person);

                if (firstPerson == NotebookPerson.None)
                    firstPerson = note.person;
            }

            if (firstPerson != NotebookPerson.None)
                DisplayPerson(firstPerson);

            return;
        }

        if (selectedCategory == NoteCategory.Obserwacje)
        {
            NotebookObservation firstObservation = NotebookObservation.None;
            NoteData firstUngroupedObservation = null;
            HashSet<NotebookObservation> displayedObservations = new HashSet<NotebookObservation>();

            foreach (NoteData note in allNotes)
            {
                if (note == null || note.category != NoteCategory.Obserwacje)
                    continue;

                if (IsGroupedObservationNote(note))
                {
                    if (!displayedObservations.Add(note.observation))
                        continue;

                    CreateObservationButton(note.observation);
                    if (firstObservation == NotebookObservation.None)
                        firstObservation = note.observation;
                    continue;
                }

                CreateNoteButton(note);
                if (firstUngroupedObservation == null)
                    firstUngroupedObservation = note;
            }

            if (firstObservation != NotebookObservation.None)
                DisplayObservation(firstObservation);
            else if (firstUngroupedObservation != null)
                DisplayNote(firstUngroupedObservation);

            return;
        }

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

    private void CreatePersonButton(NotebookPerson person)
    {
        GameObject newBtn = Instantiate(noteButtonPrefab, noteListContent);
        TextMeshProUGUI titleText = FindNoteButtonTitle(newBtn);
        if (titleText != null)
            titleText.text = GetPersonDisplayName(person);

        Button personButton = newBtn.GetComponent<Button>();
        if (personButton != null)
        {
            personButton.onClick.AddListener(() =>
            {
                OpenPerson(person);
                SetNewEntryIndicatorVisible(newBtn, false);
            });
        }

        SetNewEntryIndicatorVisible(newBtn, HasUnreadPersonNote(person));
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

        SetPeopleFactsVisible(false);
        SetObservationFactsVisible(false);
        noteDisplayArea.SetActive(true);

        // Zabezpieczenie, aby TaskDisplayArea był wyłączony podczas czytania notatki
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);

        if (displayTitle != null)
            displayTitle.text = note.noteTitle;

        if (displayContent != null)
        {
            displayContent.gameObject.SetActive(true);
            displayContent.text = ApplyPolishTypography(note.content);
        }

        if (note.customPaperGraphic != null)
        {
            paperImage.sprite = note.customPaperGraphic;
        }
    }

    // Szybki podglad notatki: panel przesuniety w osi X, opcjonalnie
    // zamykany lewym przyciskiem myszy (uzywane przez Interactable).
    public void ShowNoteImmediately(NoteData note, float displayPositionX, bool closeOnLeftMouseClick)
    {
        if (note == null || notebookPanel == null)
            return;

        RectTransform panelRect = notebookPanel.transform as RectTransform;
        if (panelRect != null)
        {
            if (!hasQuickReadPanelPosition)
            {
                panelPositionBeforeQuickRead = panelRect.anchoredPosition;
                hasQuickReadPanelPosition = true;
            }

            panelRect.anchoredPosition = new Vector2(displayPositionX, panelPositionBeforeQuickRead.y);
        }

        quickReadClosesOnLeftClick = closeOnLeftMouseClick;
        ShowNoteImmediately(note);
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
            HideQuestLogForNotebook();
            notebookPanel.SetActive(true);
            LockActivePlayerInput();
            PauseGameplayTime();
            PlaySound(openNotebookClip);
        }

        if (categoryPanel != null)
            categoryPanel.SetActive(false);

        DisplayNote(note);
    }

    private void HideQuestLogForNotebook()
    {
        CluesLog log = cluesLog != null ? cluesLog : CluesLog.Instance;
        log?.SetQuestLogVisible(false);
    }

    private void RestoreQuestLogAfterNotebook()
    {
        CluesLog log = cluesLog != null ? cluesLog : CluesLog.Instance;
        log?.SetQuestLogVisible(true);
    }

    // showUpdateNotification - czy pokazac pasek "dodano notatke"
    // playNoticeAudio    - czy odtworzyc dzwiek powiadomienia
    public void AddNote(NoteData note, bool showUpdateNotification = true, bool playNoticeAudio = true)
    {
        if (note == null)
            return;

        if (allNotes == null)
            allNotes = new List<NoteData>();

        if (allNotes.Contains(note))
            return;

        allNotes.Insert(0, note);
        unreadNotes.Add(note);
        Debug.Log("DODANO NOTATKE: " + note.name, note);

        if (showUpdateNotification)
            CluesLog.Instance?.ShowJournalUpdateNotice(GetNoteAddedNotice(note), playNoticeAudio);

        if (note.category == NoteCategory.Osoby && note.person == NotebookPerson.None)
            Debug.LogWarning("NotebookManager: note in category Osoby has no assigned person.", note);

        if (note.category == NoteCategory.Obserwacje && note.observation == NotebookObservation.None)
            Debug.LogWarning("NotebookManager: observation note has no assigned observation group and will remain an individual note.", note);

        RefreshCategoryButtons();
    }

    // Dodaje notatke i od razu otwiera ja w notatniku.
    public void OpenNoteInNotebook(NoteData note, bool playNoticeAudio = true)
    {
        if (note == null || notebookPanel == null)
            return;

        AddNote(note, true, playNoticeAudio);

        if (!IsNotebookOpen)
            ToggleNotebook();

        if (!IsNotebookOpen)
            return;

        openedAsQuickRead = false;
        quickReadClosesOnLeftClick = false;
        if (hasQuickReadPanelPosition)
        {
            RectTransform panelRect = notebookPanel.transform as RectTransform;
            if (panelRect != null)
                panelRect.anchoredPosition = panelPositionBeforeQuickRead;
            hasQuickReadPanelPosition = false;
        }

        ShowCategory((int)note.category);

        if (note.category == NoteCategory.Osoby && IsPersonNote(note))
        {
            MarkPersonNotesAsRead(note.person);
            DisplayPerson(note.person);
        }
        else if (note.category == NoteCategory.Obserwacje && IsGroupedObservationNote(note))
        {
            MarkObservationNotesAsRead(note.observation);
            DisplayObservation(note.observation);
        }
        else
        {
            MarkNoteAsRead(note);
            DisplayNote(note);
        }
    }

    // Szybki podglad notatki, bez przesuwania panelu.
    public void ShowNoteImmediately(NoteData note, bool playNoticeAudio)
    {
        AddNote(note, true, playNoticeAudio);
        ShowNoteImmediately(note);
    }

    // Szybki podglad notatki z przesunieciem panelu i zamykaniem kliknieciem.
    public void ShowNoteImmediately(NoteData note, float displayPositionX,
                                    bool closeOnLeftMouseClick, bool playNoticeAudio)
    {
        AddNote(note, true, playNoticeAudio);
        ShowNoteImmediately(note, displayPositionX, closeOnLeftMouseClick);
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

    private void CreateObservationButton(NotebookObservation observation)
    {
        GameObject newBtn = Instantiate(noteButtonPrefab, noteListContent);
        TextMeshProUGUI titleText = FindNoteButtonTitle(newBtn);
        if (titleText != null)
            titleText.text = GetObservationDisplayName(observation);

        Button observationButton = newBtn.GetComponent<Button>();
        if (observationButton != null)
        {
            observationButton.onClick.AddListener(() =>
            {
                OpenObservation(observation);
                SetNewEntryIndicatorVisible(newBtn, false);
            });
        }

        SetNewEntryIndicatorVisible(newBtn, HasUnreadObservationNote(observation));
    }

    private void OpenObservation(NotebookObservation observation)
    {
        MarkObservationNotesAsRead(observation);
        PlayButtonClickSound();
        DisplayObservation(observation);
    }

    private void DisplayObservation(NotebookObservation observation)
    {
        if (observation == NotebookObservation.None)
            return;

        noteDisplayArea.SetActive(true);
        SetPeopleFactsVisible(false);
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);

        if (displayTitle != null)
            displayTitle.text = GetObservationDisplayName(observation);

        if (displayContent != null)
            displayContent.gameObject.SetActive(false);

        if (observationFactsContent == null || observationFactPrefab == null)
        {
            Debug.LogWarning("NotebookManager: Observations View references are not assigned.", this);
            return;
        }

        observationFactsContent.SetActive(true);
        ClearObservationFacts();

        for (int i = allNotes.Count - 1; i >= 0; i--)
        {
            NoteData note = allNotes[i];
            if (!IsGroupedObservationNote(note) || note.observation != observation)
                continue;

            TextMeshProUGUI fact = Instantiate(observationFactPrefab, observationFactsContent.transform);
            fact.gameObject.SetActive(true);
            string factText = string.IsNullOrWhiteSpace(note.content) ? note.noteTitle : note.content;
            fact.text = "• " + ApplyPolishTypography(factText);
        }
    }

    private void ClearObservationFacts()
    {
        for (int i = observationFactsContent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = observationFactsContent.transform.GetChild(i);
            if (child != observationFactPrefab.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        observationFactPrefab.gameObject.SetActive(false);
    }

    private void SetObservationFactsVisible(bool visible)
    {
        if (observationFactsContent != null)
            observationFactsContent.SetActive(visible);
    }

    private bool HasUnreadObservationNote(NotebookObservation observation)
    {
        foreach (NoteData note in unreadNotes)
        {
            if (IsGroupedObservationNote(note) && note.observation == observation)
                return true;
        }

        return false;
    }

    private void MarkObservationNotesAsRead(NotebookObservation observation)
    {
        bool changed = false;
        foreach (NoteData note in allNotes)
        {
            if (IsGroupedObservationNote(note) && note.observation == observation)
                changed |= unreadNotes.Remove(note);
        }

        if (changed)
            RefreshCategoryButtons();
    }

    private static bool IsGroupedObservationNote(NoteData note)
    {
        return note != null && note.category == NoteCategory.Obserwacje &&
               note.observation != NotebookObservation.None;
    }

    private static string GetObservationDisplayName(NotebookObservation observation)
    {
        switch (observation)
        {
            case NotebookObservation.DuchJakoSprawca: return "Duch jako sprawca";
            case NotebookObservation.TajemniczeDzwieki: return "Tajemnicze dźwięki";
            case NotebookObservation.PierscienZInicjalem: return "Pierścień z inicjałem";
            case NotebookObservation.PustyPergamin: return "Pusty pergamin";
            case NotebookObservation.MiejsceZbrodni: return "Miejsce zbrodni";
            case NotebookObservation.SekretnePrzejscie: return "Sekretne przejście";
            case NotebookObservation.UkrytePrzejscie: return "Ukryte przejście";
            case NotebookObservation.PulapkaWPiwnicy: return "Pułapka w piwnicy";
            case NotebookObservation.Biblioteka: return "Biblioteka";
            default: return string.Empty;
        }
    }

    private void OpenPerson(NotebookPerson person)
    {
        MarkPersonNotesAsRead(person);
        PlayButtonClickSound();
        DisplayPerson(person);
    }

    private void DisplayPerson(NotebookPerson person)
    {
        if (person == NotebookPerson.None)
            return;

        noteDisplayArea.SetActive(true);
        SetObservationFactsVisible(false);
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);

        if (displayTitle != null)
            displayTitle.text = GetPersonDisplayName(person);

        if (displayContent != null)
            displayContent.gameObject.SetActive(false);

        if (personFactsContent == null || personFactPrefab == null)
        {
            Debug.LogWarning("NotebookManager: People View references are not assigned.", this);
            return;
        }

        personFactsContent.SetActive(true);
        ClearPersonFacts();

        // allNotes stores newest notes first; profiles read naturally from oldest fact to newest.
        for (int i = allNotes.Count - 1; i >= 0; i--)
        {
            NoteData note = allNotes[i];
            if (!IsPersonNote(note) || note.person != person)
                continue;

            TextMeshProUGUI fact = Instantiate(personFactPrefab, personFactsContent.transform);
            fact.gameObject.SetActive(true);
            string factText = string.IsNullOrWhiteSpace(note.content) ? note.noteTitle : note.content;
            fact.text = "• " + ApplyPolishTypography(factText);
        }
    }

    private void ClearPersonFacts()
    {
        for (int i = personFactsContent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = personFactsContent.transform.GetChild(i);
            if (child != personFactPrefab.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        personFactPrefab.gameObject.SetActive(false);
    }

    private void SetPeopleFactsVisible(bool visible)
    {
        if (personFactsContent != null)
            personFactsContent.SetActive(visible);
    }

    private bool HasUnreadPersonNote(NotebookPerson person)
    {
        foreach (NoteData note in unreadNotes)
        {
            if (IsPersonNote(note) && note.person == person)
                return true;
        }

        return false;
    }

    private void MarkPersonNotesAsRead(NotebookPerson person)
    {
        bool changed = false;
        foreach (NoteData note in allNotes)
        {
            if (IsPersonNote(note) && note.person == person)
                changed |= unreadNotes.Remove(note);
        }

        if (changed)
            RefreshCategoryButtons();
    }

    private static bool IsPersonNote(NoteData note)
    {
        return note != null && note.category == NoteCategory.Osoby && note.person != NotebookPerson.None;
    }

    private static string GetPersonDisplayName(NotebookPerson person)
    {
        switch (person)
        {
            case NotebookPerson.LadyEdithOgilvy: return "Lady Edith Ogilvy";
            case NotebookPerson.MadameSelma: return "Madame Selma";
            case NotebookPerson.LadyVioletOgilvy: return "Lady Violet Ogilvy";
            case NotebookPerson.SirHenry: return "Sir Henry";
            case NotebookPerson.Arthur: return "Arthur";
            case NotebookPerson.ReverendGeorge: return "Wielebny George";
            case NotebookPerson.LittleEthel: return "Mała Ethel";
            default: return string.Empty;
        }
    }

    private static string GetNoteAddedNotice(NoteData note)
    {
        if (note == null)
            return "Dodano wpis w notatniku.";

        switch (note.category)
        {
            case NoteCategory.Osoby:
                return note.person != NotebookPerson.None
                    ? "Dodano wpis w notatniku: " + GetPersonDisplayName(note.person) + "."
                    : "Dodano wpis w notatniku.";
            case NoteCategory.Obserwacje:
                return note.observation != NotebookObservation.None
                    ? "Dodano wpis w notatniku: " + GetObservationDisplayName(note.observation) + "."
                    : "Dodano wpis w notatniku: obserwacja. " + note.noteTitle;
            case NoteCategory.Obiekty:
                return "Dodano wpis w notatniku: " + note.noteTitle + ".";
            case NoteCategory.Zadania:
                return "Dodano wpis w notatniku: zadanie. " + note.noteTitle;
            default:
                return "Dodano wpis w notatniku.";
        }
    }

    private static string ApplyPolishTypography(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return PolishOrphanPattern.Replace(text, "$1\u00A0");
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

    // ---------------------------------------------------------------
    // SYSTEM ZAPISU - zebrane notatki
    // ---------------------------------------------------------------

    // Zwraca nazwy assetow zebranych notatek, od najnowszej do najstarszej.
    public List<string> GetSaveNoteNames()
    {
        List<string> nazwy = new List<string>();

        if (allNotes == null)
            return nazwy;

        foreach (NoteData note in allNotes)
        {
            if (note != null)
                nazwy.Add(note.name);
        }

        return nazwy;
    }

    // Nazwy notatek, ktorych gracz jeszcze nie otworzyl (kropka "nowe").
    public List<string> GetSaveUnreadNoteNames()
    {
        List<string> nazwy = new List<string>();

        foreach (NoteData note in unreadNotes)
        {
            if (note != null)
                nazwy.Add(note.name);
        }

        return nazwy;
    }

    public void RestoreNotes(List<string> nazwyNotatek, List<string> nazwyNieprzeczytanych)
    {
        if (nazwyNotatek == null || nazwyNotatek.Count == 0)
        {
            Debug.Log("NotebookManager: brak zapisanych notatek.");
            return;
        }

        Dictionary<string, NoteData> baza;

        try
        {
            baza = ZbierzWszystkieNotatkiZeSceny();
        }
        catch (System.Exception e)
        {
            Debug.LogError("NotebookManager: blad przy zbieraniu assetow notatek: " + e.Message +
                           " Notatki nie zostana przywrocone, ale reszta wczytywania bedzie kontynuowana.");
            return;
        }

        if (allNotes == null)
            allNotes = new List<NoteData>();

        HashSet<string> nieprzeczytane = nazwyNieprzeczytanych != null
            ? new HashSet<string>(nazwyNieprzeczytanych)
            : new HashSet<string>();

        int przywrocone = 0;
        List<string> nieznalezione = new List<string>();

        // Idziemy od konca, bo notatki wstawiamy na poczatek listy -
        // dzieki temu kolejnosc wyjdzie taka jak przy zapisie.
        for (int i = nazwyNotatek.Count - 1; i >= 0; i--)
        {
            string nazwa = nazwyNotatek[i];

            NoteData note;
            if (!baza.TryGetValue(nazwa, out note))
            {
                nieznalezione.Add(nazwa);
                continue;
            }

            if (allNotes.Contains(note))
                continue;

            allNotes.Insert(0, note);
            przywrocone++;

            if (nieprzeczytane.Contains(nazwa))
                unreadNotes.Add(note);
        }

        // Sprzatamy puste wpisy - jeden null na liscie wywraca ShowCategory.
        allNotes.RemoveAll(note => note == null);

        RefreshCategoryButtons();

        Debug.Log("NotebookManager: przywrocono " + przywrocone + " z " + nazwyNotatek.Count +
                  " notatek (nieprzeczytanych: " + unreadNotes.Count + ").");

        if (nieznalezione.Count > 0)
        {
            Debug.LogWarning("NotebookManager: nie znaleziono assetow notatek: " +
                             string.Join(" | ", nieznalezione) +
                             ". Sprawdz, czy sa przypisane w polu 'Database Notes' jakiegos Interactable.");
        }
    }

    // NotebookManager nie ma listy wszystkich mozliwych notatek - assety NoteData
    // sa przypisane w polach roznych komponentow (Interactable, skrypty dialogowe NPC).
    // Zbieramy je z trzech zrodel, zeby zadna notatka nie umknela.
    private Dictionary<string, NoteData> ZbierzWszystkieNotatkiZeSceny()
    {
        Dictionary<string, NoteData> baza = new Dictionary<string, NoteData>();

        // 1. Notatki juz zebrane / ustawione w Inspectorze.
        if (allNotes != null)
        {
            foreach (NoteData note in allNotes)
            {
                if (note != null)
                    baza[note.name] = note;
            }
        }

        // 2. Wszystkie assety NoteData wczytane do pamieci.
        foreach (NoteData note in Resources.FindObjectsOfTypeAll<NoteData>())
        {
            if (note != null)
                baza[note.name] = note;
        }

        // 3. Refleksja po komponentach sceny - lapie notatki przypisane
        //    w skryptach dialogowych NPC, ktorych nie widzi 'databaseNotes'.
        ZbierzNotatkiZKomponentow(baza);

        Debug.Log("NotebookManager: znaleziono " + baza.Count + " dostepnych assetow notatek.");
        return baza;
    }

    // Przepuszczamy tylko NoteData[] oraz List<NoteData> / kolekcje NoteData.
    private static bool CzyPoleMozeZawieracNotatki(System.Type typ)
    {
        if (typ.IsArray)
            return typeof(NoteData).IsAssignableFrom(typ.GetElementType());

        if (!typ.IsGenericType)
            return false;

        foreach (System.Type argument in typ.GetGenericArguments())
        {
            if (typeof(NoteData).IsAssignableFrom(argument))
                return true;
        }

        return false;
    }

    private void ZbierzNotatkiZKomponentow(Dictionary<string, NoteData> baza)
    {
        const BindingFlags flagi = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        MonoBehaviour[] komponenty = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (MonoBehaviour mb in komponenty)
        {
            if (mb == null)
                continue;

            foreach (FieldInfo pole in mb.GetType().GetFields(flagi))
            {
                object wartosc;

                try
                {
                    wartosc = pole.GetValue(mb);
                }
                catch (System.Exception)
                {
                    continue;
                }

                if (wartosc == null)
                    continue;

                // Pojedyncza notatka
                NoteData pojedyncza = wartosc as NoteData;
                if (pojedyncza != null)
                {
                    baza[pojedyncza.name] = pojedyncza;
                    continue;
                }

                // Tablica lub lista notatek.
                // Sprawdzamy typ pola, a NIE wartosci - inaczej wpadamy np. na
                // Transform (ktory tez jest IEnumerable) i przy nieprzypisanym
                // polu Unity rzuca UnassignedReferenceException.
                if (!CzyPoleMozeZawieracNotatki(pole.FieldType))
                    continue;

                IEnumerable kolekcja = wartosc as IEnumerable;
                if (kolekcja == null)
                    continue;

                foreach (object element in kolekcja)
                {
                    NoteData note = element as NoteData;
                    if (note != null)
                        baza[note.name] = note;
                }
            }
        }
    }
}
