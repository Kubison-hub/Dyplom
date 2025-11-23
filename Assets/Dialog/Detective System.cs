using UnityEngine;
using System.Collections.Generic;

// Enum okreœlaj¹cy, kto aktualnie mówi lub dla kogo jest opcja dialogowa
public enum CharacterType { Sherlock, Watson, NPC, Both }

// --- 1. WSKAZÓWKA DO NOTATNIKA ---
[CreateAssetMenu(fileName = "New Clue", menuName = "Detective System/Clue")]
public class ClueData : ScriptableObject
{
    public string clueID; // Unikalne ID, np. "blood_stain"
    public string title;  // Tytu³ w notatniku
    [TextArea] public string description; // Treœæ notatki
}

// --- 2. POJEDYNCZY WÊZE£ DIALOGOWY ---
[CreateAssetMenu(fileName = "New Node", menuName = "Detective System/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Ustawienia Wizualne")]
    public CharacterType speaker;
    public string speakerName;
    public Sprite portrait; // Obrazek postaci

    [Header("Treœæ")]
    // Tutaj u¿ywamy tagów TMP, np: "Widzia³em <link="clue_id"><color=yellow>Czerwony Klucz</color></link>."
    [TextArea(3, 10)] public string dialogueText;

    [Header("Wybory / Rozga³êzienia")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("Logika Specjalna")]
    public bool isEndNode = false; // Czy koñczy rozmowê?

    // Opcjonalnie: Jeœli to jest wêze³ "Wspólnej dedukcji", wymaga, aby obaj wczeœniej gadali z tym NPC
    public bool requiresInteractionComplete = false;
}

// --- 3. STRUKTURA WYBORU ---
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueNode nextNode;

    [Header("Wymagania")]
    public CharacterType requiredCharacter = CharacterType.Both; // Kto widzi tê opcjê?
    public ClueData requiredClue; // Czy trzeba mieæ dowód, by to powiedzieæ?
}

// --- 4. NPC (Kontener startowy) ---
[CreateAssetMenu(fileName = "New NPC", menuName = "Detective System/NPC Definition")]
public class NPCDefinition : ScriptableObject
{
    public string npcID; // Unikalne ID NPC, np. "Baker"
    public DialogueNode startNodeSherlock;
    public DialogueNode startNodeWatson;
    public DialogueNode interactionNode; // Dialog, gdy obaj ju¿ pogadali (dedukcja)
}