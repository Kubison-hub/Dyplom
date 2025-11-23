using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DetectiveManager : MonoBehaviour
{
    public static DetectiveManager Instance;

    [Header("Stan Gry")]
    public CharacterType currentCharacter = CharacterType.Sherlock; // Zmieniaj to w trakcie gry prze³¹czaj¹c postacie

    // Listy tymczasowe (w prawdziwej grze ³adujemy je ze startem)
    private HashSet<string> unlockedClues = new HashSet<string>();
    private List<ClueData> allCluesDatabase; // Przypisz wszystkie mo¿liwe ClueData w inspektorze lub ³aduj z Resources

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadState();
    }

    // --- ZARZ¥DZANIE STANEM (PlayerPrefs) ---

    // Oznaczamy, ¿e postaæ gada³a z NPC
    public void MarkDialogueComplete(string npcID, CharacterType character)
    {
        string key = $"Talked_{npcID}_{character}";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"Zapisano: {character} rozmawia³ z {npcID}");
    }

    // Sprawdzamy czy obaj rozmawiali z NPC (do odblokowania dedukcji)
    public bool CheckCrossInteraction(string npcID)
    {
        bool sherlockTalked = PlayerPrefs.GetInt($"Talked_{npcID}_Sherlock", 0) == 1;
        bool watsonTalked = PlayerPrefs.GetInt($"Talked_{npcID}_Watson", 0) == 1;
        return sherlockTalked && watsonTalked;
    }

    // --- SYSTEM WSKAZÓWEK (CLUES) ---

    // Wywo³ywane, gdy klikniemy s³owo kluczowe w tekœcie
    public void UnlockClue(string clueID)
    {
        if (unlockedClues.Contains(clueID)) return; // Ju¿ mamy

        // Szukamy danych wskazówki (w uproszczeniu: musisz mieæ listê wszystkich wskazówek w menad¿erze)
        ClueData foundClue = FindClueByID(clueID);

        if (foundClue != null)
        {
            unlockedClues.Add(clueID);
            PlayerPrefs.SetInt($"Clue_{clueID}", 1);
            PlayerPrefs.Save();

            Debug.Log($"ODBLOKOWANO NOTATKÊ: {foundClue.title}");
            // Tu mo¿na wywo³aæ zdarzenie UI (np. powiadomienie na ekranie)
            DialogueUIController.Instance.ShowNotification($"Nowy wpis: {foundClue.title}");
        }
    }

    public bool HasClue(ClueData clue)
    {
        if (clue == null) return true;
        return unlockedClues.Contains(clue.clueID);
    }

    public List<ClueData> GetUnlockedCluesList()
    {
        List<ClueData> list = new List<ClueData>();
        foreach (var id in unlockedClues)
        {
            var c = FindClueByID(id);
            if (c != null) list.Add(c);
        }
        return list;
    }

    private void LoadState()
    {
        // Tutaj wczytalibyœmy wszystkie klucze zaczynaj¹ce siê od Clue_
        // Dla uproszczenia wczytujemy dynamicznie przy sprawdzaniu, 
        // ale do wyœwietlenia notatnika musimy znaæ listê.

        // Prostym trikiem jest trzymanie wszystkich ClueData w jednej liœcie w Inspectorze tego managera
        // i iterowanie po niej sprawdzaj¹c PlayerPrefs.
    }

    // Helper: Musisz przypisaæ wszystkie stworzone ScriptableObjects Clue do tej listy w Inspectorze
    [SerializeField] private List<ClueData> allGameClues;

    private ClueData FindClueByID(string id)
    {
        return allGameClues.FirstOrDefault(c => c.clueID == id);
    }

    // Helper do ³adowania przy starcie
    void Start()
    {
        foreach (var c in allGameClues)
        {
            if (PlayerPrefs.GetInt($"Clue_{c.clueID}", 0) == 1)
            {
                unlockedClues.Add(c.clueID);
            }
        }
    }
}