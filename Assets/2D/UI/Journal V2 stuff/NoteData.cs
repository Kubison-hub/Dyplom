using UnityEngine;

public enum NoteCategory
{
    Obserwacje, // Indeks 0
    Osoby,      // Indeks 1
    Obiekty,    // Indeks 2
    Zadania,   // Indeks 3
}

public enum NotebookPerson
{
    None,
    LadyEdithOgilvy,
    MadameSelma,
    LadyVioletOgilvy,
    SirHenry,
    Arthur,
    ReverendGeorge,
    LittleEthel,
}

public enum NotebookObservation
{
    None,
    DuchJakoSprawca,
    TajemniczeDzwieki,
    PierscienZInicjalem,
    PustyPergamin,
    MiejsceZbrodni,
    SekretnePrzejscie,
    UkrytePrzejscie,
}

[CreateAssetMenu(fileName = "Nowa Notatka", menuName = "Notatnik/Notatka")]
public class NoteData : ScriptableObject
{
    public string noteTitle;
    public NoteCategory category;
    [Tooltip("Required only for notes in the Osoby category.")]
    public NotebookPerson person;
    [Tooltip("Required only for grouped notes in the Obserwacje category.")]
    public NotebookObservation observation;
    [TextArea(5, 15)]
    public string content;
    public Sprite customPaperGraphic;
}
