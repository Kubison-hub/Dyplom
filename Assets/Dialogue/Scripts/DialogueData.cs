using System;
using System.Collections.Generic;
using UnityEngine;

// Definiuje, która postaæ (gracz) wypowiada dan¹ liniê
public enum CharacterType
{
    NPC,
    Sherlock,
    Watson,
    None // U¿ywane dla opcji wyboru
}

// =============================
// Dane Notatnika / S³ów Kluczowych
// =============================

[System.Serializable]
public class NoteData
{
    [Tooltip("Unikalny identyfikator notatki (np. 'murder_weapon_found')")]
    public string NoteID;

    [Tooltip("Treœæ, która pojawi siê w Notatniku.")]
    [TextArea(3, 5)]
    public string ClueText;

    [Tooltip("Czy ta notatka pochodzi z rozmowy z NPC czy z konwersacji miêdzy graczami.")]
    public bool IsInference;
}

[System.Serializable]
public class KeywordData
{
    [Tooltip("S³owo/Frazza do podœwietlenia w dialogu (np. 'skrzynia' lub 'alibi')")]
    public string Keyword;

    [Tooltip("Tekst odpowiedzi NPC, gdy gracz zapyta o to s³owo kluczowe.")]
    [TextArea(3, 5)]
    public string NPCResponse;

    [Tooltip("Wymagania, aby s³owo kluczowe siê pojawi³o (np. 'Sherlock' lub posiadanie innej notatki).")]
    public List<Condition> Conditions = new List<Condition>();

    [Tooltip("Notatka, która zostanie dodana do Notatnika po zapytaniu o to s³owo kluczowe.")]
    public NoteData NoteOnAsk;
}

// =============================
// Struktury Dialogowe
// =============================

[System.Serializable]
public class DialogueOption
{
    [Tooltip("Tekst widoczny jako wybór gracza.")]
    [TextArea(1, 3)]
    public string OptionText;

    [Tooltip("Wymagania, aby ta opcja by³a widoczna/dostêpna.")]
    public List<Condition> Conditions = new List<Condition>();

    [Tooltip("Klucz do nastêpnego Node'a dialogowego (ustawiane w GraphView).")]
    public string NextNodeID;

    [Tooltip("Akcje do wykonania po wybraniu tej opcji (np. 'UpdateQuest', 'TriggerCutscene').")]
    public List<string> Actions; // Do zaimplementowania w QuestManagerze
}

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Postaæ, która wypowiada tê liniê.")]
    public CharacterType Speaker;

    [Tooltip("Treœæ wypowiedzi. U¿yj formatu [KEYWORD]aby zaznaczyæ s³owa kluczowe.[/KEYWORD]")]
    [TextArea(3, 5)]
    public string LineText;

    [Tooltip("Lista s³ów kluczowych do znalezienia w tej linii. Pamiêtaj o dopasowaniu tekstu!")]
    public List<KeywordData> Keywords = new List<KeywordData>();
}

[System.Serializable]
public class DialogueNode
{
    [Tooltip("Unikalny klucz wêz³a (u¿ywany do po³¹czeñ).")]
    public string NodeID;

    [Tooltip("Sekwencja linii wypowiadanych przez NPC/postacie.")]
    public List<DialogueLine> Lines = new List<DialogueLine>();

    [Tooltip("Opcje wyboru dla gracza.")]
    public List<DialogueOption> Options = new List<DialogueOption>();

    [Tooltip("Czy po zakoñczeniu tego wêz³a powinien zostaæ uruchomiony dialog wewnêtrzny Sherlock/Watson.")]
    public bool ShouldTriggerInterrogation = false;
}

// =============================
// Warunki (Conditions)
// =============================

[System.Serializable]
public class Condition
{
    public ConditionType Type;
    public string TargetID; // Np. nazwa zadania, ID notatki
    public bool RequiredValue; // Np. True jeœli notatka musi istnieæ
}

public enum ConditionType
{
    RequireNote, // Wymaga posiadania notatki (TargetID = NoteID)
    RequireQuestStatus, // Wymaga konkretnego statusu zadania
    IsSherlock, // Wymaga, aby aktualn¹ postaci¹ by³ Sherlock
    IsWatson // Wymaga, aby aktualn¹ postaci¹ by³ Watson
}