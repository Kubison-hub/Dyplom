using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject notebookPanel;     // T³o ca³ego notatnika
    public GameObject categoryPanel;     // Panel z trzema kategoriami
    public GameObject noteListPanel;     // Panel z list¹ wygenerowanych notatek i przyciskiem "Wróæ"
    public GameObject noteDisplayArea;   // Panel z otwart¹ kartk¹ papieru

    [Header("List Settings")]
    public Transform noteListContent;    // Miejsce (rodzic), gdzie generuj¹ siê przyciski

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
            // Otwieramy notatnik - zawsze wymuszamy ekran startowy z kategoriami
            BackToCategories();
        }
    }

    // Wywo³ywane przez przyciski kategorii (0, 1, 2)
    public void ShowCategory(int categoryIndex)
    {
        NoteCategory selectedCategory = (NoteCategory)categoryIndex;

        // UKRYWAMY panel kategorii, POKAZUJEMY panel listy notatek
        categoryPanel.SetActive(false);
        noteListPanel.SetActive(true);
        noteDisplayArea.SetActive(false);

        // Wyczyœæ star¹ listê
        foreach (Transform child in noteListContent)
        {
            Destroy(child.gameObject);
        }

        // Generuj now¹ listê
        foreach (NoteData note in allNotes)
        {
            if (note.category == selectedCategory)
            {
                CreateNoteButton(note);
            }
        }
    }

    // NOWA FUNKCJA - podepnij j¹ pod przycisk "Wróæ"
    public void BackToCategories()
    {
        categoryPanel.SetActive(true);
        noteListPanel.SetActive(false);
        noteDisplayArea.SetActive(false);
    }

    // NOWA FUNKCJA - podepnij pod przycisk "Wróæ do listy" (ten na kartce)
    public void BackToNoteList()
    {
        noteListPanel.SetActive(true);   // Pokazujemy z powrotem listê notatek
        noteDisplayArea.SetActive(false); // Ukrywamy kartkê z tekstem
    }

    private void CreateNoteButton(NoteData note)
    {
        GameObject newBtn = Instantiate(noteButtonPrefab, noteListContent);
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text = note.noteTitle;
        newBtn.GetComponent<Button>().onClick.AddListener(() => OpenNote(note));
    }

    private void OpenNote(NoteData note)
    {
        noteListPanel.SetActive(false); // Dodaliœmy to: ukrywa listê, ¿eby nie blokowa³a klikniêæ w tle!
        noteDisplayArea.SetActive(true);

        displayTitle.text = note.noteTitle;
        displayContent.text = note.content;

        if (note.customPaperGraphic != null)
        {
            paperImage.sprite = note.customPaperGraphic;
        }
    }
}