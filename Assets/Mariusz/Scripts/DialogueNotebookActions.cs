using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows Dialogue Editor node events to add a selected notebook note.
/// Attach this component to the same object as the NPCConversation.
/// </summary>
public class DialogueNotebookActions : MonoBehaviour
{
    [SerializeField] private List<NoteData> notes = new List<NoteData>();

    public void AddNote(int noteIndex)
    {
        if (noteIndex < 0 || noteIndex >= notes.Count)
        {
            Debug.LogWarning($"DialogueNotebookActions: invalid note index {noteIndex} on '{name}'.", this);
            return;
        }

        NoteData note = notes[noteIndex];
        if (note == null)
        {
            Debug.LogWarning($"DialogueNotebookActions: note at index {noteIndex} is not assigned on '{name}'.", this);
            return;
        }

        if (NotebookManager.Instance == null)
        {
            Debug.LogWarning("DialogueNotebookActions: NotebookManager.Instance was not found.", this);
            return;
        }

        NotebookManager.Instance.AddNote(note, playNoticeAudio: false);
    }
}
