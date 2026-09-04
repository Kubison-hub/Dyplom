using UnityEngine;
using System.IO;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    // ID przedmiotow podniesionych w tej sesji/scenie.
    // Nie jest statyczne celowo: nowa scena = nowy manager = czysta lista (nowa gra).
    private readonly HashSet<string> collectedObjectIDs = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SaveLoadManager: wykryto duplikat na obiekcie '" + gameObject.name + "'. Usuwam nadmiarowy komponent.");
            Destroy(this);
            return;
        }

        Instance = this;
        Debug.Log("SaveLoadManager: gotowy na obiekcie '" + gameObject.name + "'.");
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("LoadGameOnStart", 0) == 1)
        {
            Debug.Log("SaveLoadManager: flaga LoadGameOnStart = 1, wczytuje zapis.");
            LoadGame();

            PlayerPrefs.SetInt("LoadGameOnStart", 0);
            PlayerPrefs.Save();
        }
    }

    // ---------------------------------------------------------------
    // IDENTYFIKACJA OBIEKTOW
    // ---------------------------------------------------------------

    // Buduje unikalna sciezke obiektu w hierarchii, np. "Level/Items/Zielona kostka".
    // Dziala dla dowolnego obiektu, niezaleznie od tego jaki ma skrypt.
    public static string BuildObjectID(GameObject go)
    {
        if (go == null)
            return "";

        Transform t = go.transform;
        string path = t.name;
        Transform parent = t.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Wywolaj to w momencie podniesienia/usuniecia przedmiotu ze sceny.
    // Przyklad: SaveLoadManager.Instance?.MarkCollected(gameObject);
    public void MarkCollected(GameObject go)
    {
        string id = BuildObjectID(go);
        if (string.IsNullOrEmpty(id))
            return;

        collectedObjectIDs.Add(id);
        Debug.Log("SaveLoadManager: zarejestrowano podniesienie '" + id + "'.");
    }

    // Wersja przyjmujaca gotowe ID (uzywa jej stary PickupItem).
    public void MarkPickupCollected(string objectID)
    {
        if (string.IsNullOrEmpty(objectID))
            return;

        collectedObjectIDs.Add(objectID);
        Debug.Log("SaveLoadManager: zarejestrowano podniesienie '" + objectID + "'.");
    }

    // ---------------------------------------------------------------
    // ZAPIS
    // ---------------------------------------------------------------

    public void SaveGame()
    {
        Debug.Log(">>> SaveGame() ZOSTALO WYWOLANE <<<");

        GameData data = new GameData();

        if (QuestManager.Instance != null)
        {
            if (QuestManager.Instance.sherlockController != null)
                data.sherlockPosition = QuestManager.Instance.sherlockController.transform.position;
            else
                Debug.LogWarning("SaveGame: sherlockController jest pusty.");

            if (QuestManager.Instance.watsonController != null)
                data.watsonPosition = QuestManager.Instance.watsonController.transform.position;
            else
                Debug.LogWarning("SaveGame: watsonController jest pusty.");

            data.sherlock_NPC1 = QuestManager.Instance.sherlock_Gadal_Z_NPC1;
            data.watson_NPC1 = QuestManager.Instance.watson_Gadal_Z_NPC1;
            data.sherlock_NPC2 = QuestManager.Instance.sherlock_Gadal_Z_NPC2;
            data.watson_NPC2 = QuestManager.Instance.watson_Gadal_Z_NPC2;
            data.rozmowaMiedzyGraczami = QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta;

            if (QuestManager.Instance.hiddenItems != null &&
                QuestManager.Instance.hiddenItems.Length > 0 &&
                QuestManager.Instance.hiddenItems[0] != null)
            {
                data.czyPrzedmiotySiePojawily = QuestManager.Instance.hiddenItems[0].activeSelf;
            }
        }
        else
        {
            Debug.LogWarning("SaveGame: brak QuestManager.Instance w tej scenie.");
        }

        // --- Ekwipunek ---
        if (InventoryManager.Instance != null)
        {
            data.itemsInInventory = new List<ItemType>(InventoryManager.Instance.items);
            Debug.Log("SaveGame: zapisuje ekwipunek, przedmiotow: " + data.itemsInInventory.Count);
        }
        else
        {
            Debug.LogError("SaveGame: InventoryManager.Instance jest NULL! Ekwipunek NIE zostanie zapisany.");
        }

        data.pickedUpItemIDs = new List<string>(collectedObjectIDs);

        if (JournalManager.Instance != null)
            data.unlockedNoteIDs = JournalManager.Instance.unlockedNoteIndices;
        else
            Debug.LogWarning("SaveGame: brak JournalManager.Instance.");

        if (TutorialManager.Instance != null)
            data.shownTutorialIDs = TutorialManager.Instance.GetShownTutorials();
        else
            Debug.LogWarning("SaveGame: brak TutorialManager.Instance.");

        string path = Path.Combine(Application.persistentDataPath, "savegame.json");

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log("Gra zapisana w: " + path + " (przedmiotow w eq: " +
                      (data.itemsInInventory != null ? data.itemsInInventory.Count : 0) +
                      ", podniesionych: " + data.pickedUpItemIDs.Count + ")");
        }
        catch (System.Exception e)
        {
            Debug.LogError("SaveGame: blad zapisu pliku! " + e.Message);
        }
    }

    // ---------------------------------------------------------------
    // WCZYTANIE
    // ---------------------------------------------------------------

    public void LoadGame()
    {
        Debug.Log(">>> LoadGame() ZOSTALO WYWOLANE <<<");

        string path = Path.Combine(Application.persistentDataPath, "savegame.json");

        if (!File.Exists(path))
        {
            Debug.LogWarning("Brak pliku zapisu! Szukalem tutaj: " + path);
            return;
        }

        GameData data;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<GameData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("LoadGame: blad odczytu pliku! " + e.Message);
            return;
        }

        if (data == null)
        {
            Debug.LogError("LoadGame: plik zapisu jest uszkodzony lub pusty.");
            return;
        }

        if (QuestManager.Instance != null)
        {
            if (QuestManager.Instance.sherlockController != null)
            {
                var sherlockCC = QuestManager.Instance.sherlockController.GetComponent<CharacterController>();
                if (sherlockCC) sherlockCC.enabled = false;
                QuestManager.Instance.sherlockController.transform.position = data.sherlockPosition;
                if (sherlockCC) sherlockCC.enabled = true;
            }
            else
            {
                Debug.LogWarning("LoadGame: sherlockController jest pusty, pomijam pozycje.");
            }

            if (QuestManager.Instance.watsonController != null)
            {
                var watsonCC = QuestManager.Instance.watsonController.GetComponent<CharacterController>();
                if (watsonCC) watsonCC.enabled = false;
                QuestManager.Instance.watsonController.transform.position = data.watsonPosition;
                if (watsonCC) watsonCC.enabled = true;
            }
            else
            {
                Debug.LogWarning("LoadGame: watsonController jest pusty, pomijam pozycje.");
            }

            QuestManager.Instance.sherlock_Gadal_Z_NPC1 = data.sherlock_NPC1;
            QuestManager.Instance.watson_Gadal_Z_NPC1 = data.watson_NPC1;
            QuestManager.Instance.sherlock_Gadal_Z_NPC2 = data.sherlock_NPC2;
            QuestManager.Instance.watson_Gadal_Z_NPC2 = data.watson_NPC2;
            QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta = data.rozmowaMiedzyGraczami;

            if (data.czyPrzedmiotySiePojawily)
                QuestManager.Instance.SpawnHiddenItems();
        }
        else
        {
            Debug.LogWarning("LoadGame: brak QuestManager.Instance w tej scenie.");
        }

        // --- Ekwipunek (z pelna diagnostyka) ---
        PrzywrocEkwipunek(data.itemsInInventory);

        // --- Usuwamy ze sceny przedmioty juz podniesione ---
        // Po SpawnHiddenItems, zeby objac takze przedmioty dopiero co odsloniete.
        UsunPodniesionePrzedmioty(data.pickedUpItemIDs);

        if (JournalManager.Instance != null && data.unlockedNoteIDs != null)
        {
            foreach (int id in data.unlockedNoteIDs)
                JournalManager.Instance.UnlockNote(id);
        }

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.RestoreShownTutorials(data.shownTutorialIDs);

        Debug.Log("Gra wczytana!");
    }

    private void PrzywrocEkwipunek(List<ItemType> zapisanePrzedmioty)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("LoadGame: InventoryManager.Instance jest NULL! Ekwipunek nie zostanie przywrocony. " +
                           "Sprawdz, czy obiekt z InventoryManager jest AKTYWNY na starcie sceny " +
                           "(nieaktywny obiekt nie wykona Awake i nie ustawi Instance).");
            return;
        }

        if (zapisanePrzedmioty == null)
        {
            Debug.LogWarning("LoadGame: pole 'itemsInInventory' w pliku zapisu jest null. " +
                             "Prawdopodobnie plik pochodzi ze starszej wersji - zrob nowy zapis.");
            return;
        }

        Debug.Log("LoadGame: przywracam ekwipunek, przedmiotow w pliku: " + zapisanePrzedmioty.Count);

        foreach (ItemType it in zapisanePrzedmioty)
            Debug.Log("LoadGame:   - w zapisie: " + it);

        InventoryManager.Instance.RestoreItems(zapisanePrzedmioty);
    }

    private void UsunPodniesionePrzedmioty(List<string> zapisaneID)
    {
        collectedObjectIDs.Clear();

        if (zapisaneID == null || zapisaneID.Count == 0)
        {
            Debug.Log("LoadGame: brak podniesionych przedmiotow w zapisie.");
            return;
        }

        foreach (string id in zapisaneID)
            collectedObjectIDs.Add(id);

        // Przechodzimy po WSZYSTKICH obiektach sceny (takze nieaktywnych)
        // i porownujemy ich sciezke z lista podniesionych.
        int usuniete = 0;
        Transform[] wszystkieObiekty = FindObjectsOfType<Transform>(true);

        foreach (Transform t in wszystkieObiekty)
        {
            if (t == null)
                continue;

            if (collectedObjectIDs.Contains(BuildObjectID(t.gameObject)))
            {
                Destroy(t.gameObject);
                usuniete++;
            }
        }

        Debug.Log("LoadGame: usunieto ze sceny " + usuniete + " z " + zapisaneID.Count + " podniesionych przedmiotow.");

        if (usuniete < zapisaneID.Count)
        {
            Debug.LogWarning("LoadGame: nie znaleziono czesci obiektow. Zapisane ID: " +
                             string.Join(" | ", zapisaneID));
        }
    }

    [ContextMenu("Pokaz sciezke zapisu")]
    public void PokazSciezke()
    {
        Debug.Log("Sciezka zapisu: " + Path.Combine(Application.persistentDataPath, "savegame.json"));
    }
}