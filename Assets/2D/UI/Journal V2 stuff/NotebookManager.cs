using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    [Header("List Settings")]
    public Transform noteListContent;

    [Header("Prefabs & UI Elements")]
    public GameObject noteButtonPrefab;
    public TextMeshProUGUI displayTitle;
    public TextMeshProUGUI displayContent;
    public Image paperImage;

    [Header("Database")]
    public List<NoteData> allNotes;

    private PlayerInput notebookLockedInput;
    private bool notebookDisabledInput;
    private bool openedAsQuickRead;

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
    }

    private void Start()
    {
        notebookPanel.SetActive(false);
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

        notebookPanel.SetActive(true);
        LockActivePlayerInput();
        BackToCategories(); // Ta funkcja ustawi domyœlny widok po wciœniêciu TAB
    }

    // Funkcja do fizycznego przycisku zamykania Notatnika
    public void CloseNotebook()
    {
        if (notebookPanel != null)
            notebookPanel.SetActive(false);

        openedAsQuickRead = false;
        RestoreActivePlayerInput();
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

    // Wywo³ywane przez przyciski kategorii (0, 1, 2)
    public void ShowCategory(int categoryIndex)
    {
        NoteCategory selectedCategory = (NoteCategory)categoryIndex;

        categoryPanel.SetActive(false);
        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);

        // NOWE: Ukrywamy TaskDisplayArea po wejœciu w kategoriê
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);

        // Wyczyœæ star¹ listê
        foreach (Transform child in noteListContent)
        {
            Destroy(child.gameObject);
        }

        // Generuj now¹ listê dla wybranej kategorii
        foreach (NoteData note in allNotes)
        {
            if (note.category == selectedCategory)
            {
                CreateNoteButton(note);
            }
        }
    }

    public void BackToCategories()
    {
        if (openedAsQuickRead)
        {
            CloseNotebook();
            return;
        }

        categoryPanel.SetActive(true);
        noteListPanel.SetActive(false);
        noteDisplayArea.SetActive(false);

        // NOWE: Wyœwietlamy TaskDisplayArea jako domyœlny widok wraz z kategoriami
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(true);
    }

    public void BackToNoteList()
    {
        if (openedAsQuickRead)
        {
            CloseNotebook();
            return;
        }

        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);

        // NOWE: Upewniamy siê, ¿e wracaj¹c do listy, TaskDisplayArea jest ukryty
        if (taskDisplayArea != null)
            taskDisplayArea.SetActive(false);
    }

    private void CreateNoteButton(NoteData note)
    {
        GameObject newBtn = Instantiate(noteButtonPrefab, noteListContent);
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text = note.noteTitle;
        newBtn.GetComponent<Button>().onClick.AddListener(() => OpenNote(note));
    }

    private void OpenNote(NoteData note)
    {
        // ZMIANA: Usuniêto ukrywanie noteListPanel, aby lista notatek do wyboru pozosta³a widoczna na ekranie.
        // noteListPanel.SetActive(false); <-- To powodowa³o znikanie listy

        noteDisplayArea.SetActive(true);

        // Zabezpieczenie, aby TaskDisplayArea by³ wy³¹czony podczas czytania notatki
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
            notebookPanel.SetActive(true);
            LockActivePlayerInput();
        }

        if (categoryPanel != null)
            categoryPanel.SetActive(false);

        OpenNote(note);
    }

    public void AddNote(NoteData note)
    {
        if (note == null)
            return;

        if (allNotes == null)
            allNotes = new List<NoteData>();

        if (!allNotes.Contains(note))
            allNotes.Add(note);
    }
}