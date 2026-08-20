using UnityEngine;

public enum NoteCategory
{
    Obserwacje,
    Osoby,
    Obiekty
}

[CreateAssetMenu(fileName = "Nowa Notatka", menuName = "Notatnik/Notatka")]
public class NoteData : ScriptableObject
{
    public string noteTitle; // Tytu³ widoczny na przycisku
    public NoteCategory category; // Kategoria notatki
    [TextArea(5, 15)]
    public string content; // Treœæ notatki
    public Sprite customPaperGraphic; // Opcjonalna inna grafika kartki
}