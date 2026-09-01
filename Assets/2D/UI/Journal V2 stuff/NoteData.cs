using UnityEngine;

public enum NoteCategory
{
    Obserwacje, // Indeks 0
    Osoby,      // Indeks 1
    Obiekty,    // Indeks 2
    Zadania,   // Indeks 3
}

[CreateAssetMenu(fileName = "Nowa Notatka", menuName = "Notatnik/Notatka")]
public class NoteData : ScriptableObject
{
    public string noteTitle;
    public NoteCategory category;
    [TextArea(5, 15)]
    public string content;
    public Sprite customPaperGraphic;
}