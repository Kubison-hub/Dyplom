using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject notebookPanel;
    public GameObject categoryPanel;
    public GameObject noteListPanel;
    public GameObject noteDisplayArea;

    [Header("List Settings")]
    public Transform noteListContent;

    [Header("Prefabs & UI Elements")]
    public GameObject noteButtonPrefab;
    public TextMeshProUGUI displayTitle;
    public TextMeshProUGUI displayContent;
    public Image paperImage;

    [Header("Database")]
    public List<NoteData> allNotes;

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
        bool isActive = notebookPanel.activeSelf;
        notebookPanel.SetActive(!isActive);

        if (!isActive)
        {
            BackToCategories(); // Pokazujemy kategorie na start
        }
    }

    // Funkcja do fizycznego przycisku zamykania Notatnika
    public void CloseNotebook()
    {
        notebookPanel.SetActive(false);
    }

    // Wywo³ywane przez przyciski kategorii (0, 1, 2)
    public void ShowCategory(int categoryIndex)
    {
        NoteCategory selectedCategory = (NoteCategory)categoryIndex;

        categoryPanel.SetActive(false);
        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);

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
        categoryPanel.SetActive(true);
        noteListPanel.SetActive(false);
        noteDisplayArea.SetActive(false);
    }

    public void BackToNoteList()
    {
        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);
    }

    private void CreateNoteButton(NoteData note)
    {
        GameObject newBtn = Instantiate(noteButtonPrefab, noteListContent);
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text = note.noteTitle;
        newBtn.GetComponent<Button>().onClick.AddListener(() => OpenNote(note));
    }

    private void OpenNote(NoteData note)
    {
        noteListPanel.SetActive(false);
        noteDisplayArea.SetActive(true);

        displayTitle.text = note.noteTitle;
        displayContent.text = note.content;

        if (note.customPaperGraphic != null)
        {
            paperImage.sprite = note.customPaperGraphic;
        }
    }
}