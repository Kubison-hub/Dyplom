using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// System zarz¹dzaj¹cy notatnikiem i clue (wskazówkami)
public class NotebookManager : MonoBehaviour
{
    // U¿ywamy statycznego singletona, aby by³ ³atwo dostêpny
    public static NotebookManager Instance { get; private set; }

    [Header("Wizualne komponenty Notatnika")]
    // Referencje do Canvasu i elementów UI Notatnika (np. BookUI)
    public GameObject NotebookCanvas;

    // Lista notatek, które gracz zebra³
    private List<NoteData> collectedNotes = new List<NoteData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Zapisywanie notatek do Notatnika
    public bool AddNote(NoteData note)
    {
        // SprawdŸ, czy notatka o takim ID ju¿ istnieje
        if (collectedNotes.Any(n => n.NoteID == note.NoteID))
        {
            Debug.LogWarning($"Notatka o ID: {note.NoteID} ju¿ istnieje i nie zosta³a dodana ponownie.");
            return false;
        }

        collectedNotes.Add(note);
        Debug.Log($"Dodano now¹ notatkê do Notatnika: {note.NoteID}");

        // TODO: Wywo³anie zdarzenia do aktualizacji UI notatnika
        // OnNoteAdded?.Invoke(note); 
        return true;
    }

    // Sprawdzanie warunku (wymagane np. dla opcji dialogowych)
    public bool HasNote(string noteId)
    {
        return collectedNotes.Any(n => n.NoteID == noteId);
    }

    // Pobieranie wszystkich notatek do wyœwietlenia w UI
    public List<NoteData> GetAllNotes()
    {
        return collectedNotes;
    }

    // Prze³¹czanie widocznoœci Notatnika
    public void ToggleNotebook(bool isPaused)
    {
        bool newState = !NotebookCanvas.activeSelf;
        NotebookCanvas.SetActive(newState);

        // Opcjonalnie: ustawienie Time.timeScale na 0, jeœli gra ma byæ zatrzymana
        Time.timeScale = newState ? 0f : 1f;

        // TODO: Aktualizacja wizualna notatnika po otwarciu
        if (newState)
        {
            // UpdateNotebookUI(collectedNotes);
        }
    }
}