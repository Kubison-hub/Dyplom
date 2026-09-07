using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Unity.Cinemachine;

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
                Debug.LogWarning("SaveGame: sherlockController jest pusty - pozycja Sherlocka NIE zostanie zapisana. " +
                                 "Przypisz pole 'Sherlock Controller' w komponencie QuestManager.");

            if (QuestManager.Instance.watsonController != null)
                data.watsonPosition = QuestManager.Instance.watsonController.transform.position;
            else
                Debug.LogWarning("SaveGame: watsonController jest pusty - pozycja Watsona NIE zostanie zapisana. " +
                                 "Przypisz pole 'Watson Controller' w komponencie QuestManager.");

            data.sherlock_NPC1 = QuestManager.Instance.sherlock_Gadal_Z_NPC1;
            data.watson_NPC1 = QuestManager.Instance.watson_Gadal_Z_NPC1;
            data.sherlock_NPC2 = QuestManager.Instance.sherlock_Gadal_Z_NPC2;
            data.watson_NPC2 = QuestManager.Instance.watson_Gadal_Z_NPC2;
            data.rozmowaMiedzyGraczami = QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta;

            data.czyPrzedmiotySiePojawily = CzyUkrytePrzedmiotyOdsloniete();
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

        // --- Ukonczone interakcje (zagadki, drzwi, dialogi) ---
        if (GameProgressManager.Instance != null)
        {
            data.completedInteractionIDs = GameProgressManager.Instance.GetCompletedInteractionIds();
            Debug.Log("SaveGame: zapisuje ukonczone interakcje: " + data.completedInteractionIDs.Count);
        }
        else
        {
            data.completedInteractionIDs = new List<string>();
            Debug.LogWarning("SaveGame: brak GameProgressManager.Instance - postep interakcji NIE zostanie zapisany. " +
                             "Dodaj komponent GameProgressManager do sceny.");
        }

        // --- Panel zadan (lista celow) ---
        if (CluesLog.Instance != null)
        {
            data.questLog = CluesLog.Instance.GetSaveState();
            Debug.Log("SaveGame: zapisuje postep panelu zadan.");
        }
        else
        {
            Debug.LogWarning("SaveGame: brak CluesLog.Instance - postep celow NIE zostanie zapisany.");
        }

        // --- Etap samouczka / sekwencji startowej ---
        if (TutorialTimeline.Instance != null)
        {
            data.tutorialStage = TutorialTimeline.Instance.GetSaveStage();
            Debug.Log("SaveGame: zapisuje etap samouczka: " + data.tutorialStage);
        }
        else
        {
            Debug.LogWarning("SaveGame: brak TutorialTimeline.Instance - etap samouczka NIE zostanie zapisany.");
        }

        // --- Odkryte punkty sledztwa ---
        data.discoveredIdeaPointIDs = ZbierzOdkrytePunkty();

        // --- Aktywne grupy poziomow ---
        data.levelGroups = ZbierzGrupyPoziomow();

        // --- Pozycje NPC ---
        data.npcs = ZbierzNpc();

        // --- Aktywna postac ---
        if (SwitchCharacter.Instance != null)
        {
            data.activePlayerIndex = SwitchCharacter.Instance.activePlayerIndex;
            data.cameraLookAtIDs = ZbierzCeleKamer();
            Debug.Log("SaveGame: zapisuje aktywna postac, indeks: " + data.activePlayerIndex);
        }
        else
        {
            Debug.LogWarning("SaveGame: brak SwitchCharacter.Instance - aktywna postac NIE zostanie zapisana.");
        }

        // --- Badanie ciala Lady Edith ---
        Int_EdithExamBody edithExam = FindFirstObjectByType<Int_EdithExamBody>(FindObjectsInactive.Include);
        if (edithExam != null)
        {
            data.edithExam = edithExam.GetSaveState();
            Debug.Log("SaveGame: zapisuje badanie ciala Lady Edith (" +
                      data.edithExam.collectedExamClueCount + " punktow).");
        }
        else
        {
            Debug.LogWarning("SaveGame: nie znaleziono Int_EdithExamBody - badanie ciala NIE zostanie zapisane.");
        }

        // --- Notatnik ---
        if (NotebookManager.Instance != null)
        {
            data.notebookNotes = NotebookManager.Instance.GetSaveNoteNames();
            data.notebookUnreadNotes = NotebookManager.Instance.GetSaveUnreadNoteNames();
            Debug.Log("SaveGame: zapisuje notatki w notatniku: " + data.notebookNotes.Count);
        }
        else
        {
            Debug.LogWarning("SaveGame: brak NotebookManager.Instance - notatki NIE zostana zapisane.");
        }

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
            Debug.Log("Gra zapisana w: " + path +
                      " (eq: " + (data.itemsInInventory != null ? data.itemsInInventory.Count : 0) +
                      ", podniesionych: " + data.pickedUpItemIDs.Count +
                      ", interakcji: " + data.completedInteractionIDs.Count + ")");
        }
        catch (System.Exception e)
        {
            Debug.LogError("SaveGame: blad zapisu pliku! " + e.Message);
        }
    }

    // Sprawdza, czy ukryte przedmioty zostaly odsloniete.
    // Nie patrzy tylko na hiddenItems[0] - ten obiekt moze byc juz podniesiony
    // i zniszczony, co blednie dawalo false.
    private bool CzyUkrytePrzedmiotyOdsloniete()
    {
        GameObject[] ukryte = QuestManager.Instance.hiddenItems;

        if (ukryte == null || ukryte.Length == 0)
            return false;

        bool brakujeChocJednego = false;

        foreach (GameObject item in ukryte)
        {
            // Obiekt zniszczony = zostal podniesiony, czyli wczesniej byl widoczny.
            if (item == null)
            {
                brakujeChocJednego = true;
                continue;
            }

            if (item.activeSelf)
                return true;
        }

        return brakujeChocJednego;
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

        // --- Aktywne grupy poziomow: MUSZA byc pierwsze ---
        // Reszta (pozycje, NPC, przedmioty) odnosi sie do obiektow wewnatrz
        // tych grup. Na wylaczonej grupie nic nie da sie odnalezc.
        PrzywrocGrupyPoziomow(data.levelGroups);

        // --- Tutoriale ---
        // SpawnHiddenItems() ponizej pokazuje tutorial o przedmiotach do znalezienia.
        // Bez wczesniejszego przywrocenia listy widzianych tutoriali popup wyskoczylby
        // po kazdym wczytaniu zapisu.
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.RestoreShownTutorials(data.shownTutorialIDs);
            Debug.Log("LoadGame: przywrocono widziane tutoriale: " +
                      (data.shownTutorialIDs != null ? data.shownTutorialIDs.Count : 0));
        }
        else
        {
            Debug.LogWarning("LoadGame: brak TutorialManager.Instance - tutoriale moga wyskoczyc ponownie.");
        }

        if (QuestManager.Instance != null)
        {
            PrzywrocPozycje(QuestManager.Instance.sherlockController, data.sherlockPosition, "Sherlock");
            PrzywrocPozycje(QuestManager.Instance.watsonController, data.watsonPosition, "Watson");

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

        // --- Ukonczone interakcje ---
        PrzywrocUkonczoneInterakcje(data.completedInteractionIDs);

        // --- Pozycje NPC ---
        PrzywrocNpc(data.npcs);

        // --- Odkryte punkty sledztwa ---
        // MUSI byc przed CluesLog: panel przelicza licznik "Zbadaj pomieszczenie"
        // po stanie punktow, wiec bez tego pierwsze nowe odkrycie zerowaloby licznik.
        PrzywrocOdkrytePunkty(data.discoveredIdeaPointIDs);

        // --- Badanie ciala Lady Edith ---
        // Przed CluesLog: skrypt sam wypycha licznik do panelu.
        Int_EdithExamBody edithExam = FindFirstObjectByType<Int_EdithExamBody>(FindObjectsInactive.Include);
        if (edithExam != null)
            edithExam.RestoreSaveState(data.edithExam);
        else
            Debug.LogWarning("LoadGame: nie znaleziono Int_EdithExamBody - badanie ciala nie zostanie przywrocone.");

        // --- Panel zadan (lista celow) ---
        if (CluesLog.Instance != null)
            CluesLog.Instance.RestoreSaveState(data.questLog);
        else
            Debug.LogWarning("LoadGame: brak CluesLog.Instance - postep celow nie zostanie przywrocony.");

        // --- Etap samouczka: na koncu, zeby nic go pozniej nie nadpisalo ---
        if (TutorialTimeline.Instance != null)
            TutorialTimeline.Instance.RestoreSaveStage(data.tutorialStage);
        else
            Debug.LogWarning("LoadGame: brak TutorialTimeline.Instance - sekwencja startowa poleci od nowa.");

        // --- Notatnik ---
        if (NotebookManager.Instance != null)
            NotebookManager.Instance.RestoreNotes(data.notebookNotes, data.notebookUnreadNotes);
        else
            Debug.LogWarning("LoadGame: brak NotebookManager.Instance - notatki nie zostana przywrocone.");

        if (JournalManager.Instance != null && data.unlockedNoteIDs != null)
        {
            foreach (int id in data.unlockedNoteIDs)
                JournalManager.Instance.UnlockNote(id);
        }

        // --- Aktywna postac: na samym koncu ---
        // SetActivePlayer przestawia priorytety kamer, wiec musi byc po wszystkim,
        // co moglo ruszac kamera (dialogi, punkty sledztwa, panel zadan).
        if (SwitchCharacter.Instance != null)
        {
            PrzywrocCeleKamer(data.cameraLookAtIDs);
            SwitchCharacter.Instance.RestoreActivePlayer(data.activePlayerIndex);
        }
        else
        {
            Debug.LogWarning("LoadGame: brak SwitchCharacter.Instance - aktywna postac nie zostanie przywrocona.");
        }

        Debug.Log("Gra wczytana!");
    }

    // Ustawia pozycje postaci. CharacterController trzeba wylaczyc,
    // inaczej nadpisze recznie ustawiona pozycje.
    private void PrzywrocPozycje(PlayerController controller, Vector3 pozycja, string nazwa)
    {
        if (controller == null)
        {
            Debug.LogWarning("LoadGame: " + nazwa + " - kontroler jest pusty, pomijam pozycje. " +
                             "Przypisz pole w komponencie QuestManager.");
            return;
        }

        if (pozycja == Vector3.zero)
        {
            Debug.LogWarning("LoadGame: " + nazwa + " ma w zapisie pozycje (0,0,0). " +
                             "Plik pochodzi z zapisu zrobionego przy nieprzypisanym kontrolerze - " +
                             "pomijam, zeby nie wrzucic postaci pod mape. Zrob nowy zapis.");
            return;
        }

        // NavMeshAgent trzyma wlasna pozycje i w nastepnej klatce przeciagnalby
        // postac z powrotem. Do teleportacji sluzy Warp().
        UnityEngine.AI.NavMeshAgent agent = controller.GetComponent<UnityEngine.AI.NavMeshAgent>();
        CharacterController cc = controller.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        bool przeniesiony = false;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            przeniesiony = agent.Warp(pozycja);

            if (!przeniesiony)
            {
                Debug.LogWarning("LoadGame: " + nazwa + " - Warp na " + pozycja +
                                 " nie znalazl NavMesha. Ustawiam pozycje bezposrednio.");
            }
        }

        if (!przeniesiony)
            controller.transform.position = pozycja;

        if (cc != null) cc.enabled = true;

        Debug.Log("LoadGame: " + nazwa + " ustawiony na " + pozycja +
                  (agent != null ? " (przez NavMeshAgent.Warp)" : "") + ".");
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
        Transform[] wszystkieObiekty = FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

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

    // Odtwarza stan ukonczonych interakcji przez GameProgressManager.
    // ID sa stabilne (GUID w polu saveId komponentu Interactable),
    // wiec nie psuja sie przy zmianie nazw obiektow w hierarchii.
    private void PrzywrocUkonczoneInterakcje(List<string> zapisaneID)
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("LoadGame: brak GameProgressManager.Instance - " +
                             "postep interakcji nie zostanie przywrocony.");
            return;
        }

        if (zapisaneID == null || zapisaneID.Count == 0)
        {
            Debug.Log("LoadGame: brak ukonczonych interakcji w zapisie.");
            return;
        }

        int przywrocone = 0;
        List<string> nieznalezione = new List<string>();

        foreach (string id in zapisaneID)
        {
            if (GameProgressManager.Instance.RestoreInteractionCompletedState(id, true))
                przywrocone++;
            else
                nieznalezione.Add(id);
        }

        Debug.Log("LoadGame: przywrocono " + przywrocone + " z " + zapisaneID.Count + " ukonczonych interakcji.");

        if (nieznalezione.Count > 0)
        {
            Debug.LogWarning("LoadGame: nie znaleziono interakcji o ID: " + string.Join(" | ", nieznalezione) +
                             ". Kliknij 'Refresh Interaction List' na komponencie GameProgressManager.");
        }
    }

    // Zwraca grupy poziomow w scenie. Nie zgadujemy nazw: bierzemy WSZYSTKIE
    // bezposrednie dzieci obiektu-rodzica poziomow (tego z "LEVELS" w nazwie).
    // Dzieki temu dziala niezaleznie od tego, czy grupa nazywa sie LEVEL_2,
    // LEVEL2, "Level 2 - Pietro" czy jakkolwiek inaczej.
    private static Transform[] ZnajdzGrupyPoziomow()
    {
        List<Transform> grupy = new List<Transform>();

        Transform[] wszystkie = FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 1. Szukamy rodzica poziomow - obiektu z "LEVELS" w nazwie.
        foreach (Transform t in wszystkie)
        {
            if (t == null || t.name.IndexOf("LEVELS", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            for (int i = 0; i < t.childCount; i++)
                grupy.Add(t.GetChild(i));

            break;
        }

        // 2. Zapasowo: obiekty z "LEVEL" w nazwie, jesli rodzica nie ma.
        if (grupy.Count == 0)
        {
            foreach (Transform t in wszystkie)
            {
                if (t != null && t.name.IndexOf("LEVEL", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    grupy.Add(t);
            }
        }

        return grupy.ToArray();
    }

    private List<LevelGroupSaveData> ZbierzGrupyPoziomow()
    {
        List<LevelGroupSaveData> lista = new List<LevelGroupSaveData>();

        foreach (Transform t in ZnajdzGrupyPoziomow())
        {
            LevelGroupSaveData grupa = new LevelGroupSaveData();
            grupa.objectId = BuildObjectID(t.gameObject);
            grupa.active = t.gameObject.activeSelf;
            lista.Add(grupa);

            Debug.Log("SaveGame: grupa poziomu '" + t.name + "' aktywna: " + grupa.active);
        }

        if (lista.Count == 0)
        {
            Debug.LogWarning("SaveGame: nie znaleziono grup poziomow. Stan poziomow NIE zostanie zapisany.");
        }

        return lista;
    }

    private void PrzywrocGrupyPoziomow(List<LevelGroupSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
        {
            Debug.Log("LoadGame: brak grup poziomow w zapisie.");
            return;
        }

        // Zabezpieczenie: gdyby zapis mial wszystkie grupy wylaczone, wczytanie
        // dalo by czarny ekran. Wtedy lepiej nie ruszac nic.
        bool ktorakolwiekAktywna = false;
        foreach (LevelGroupSaveData grupa in zapisane)
        {
            if (grupa != null && grupa.active)
            {
                ktorakolwiekAktywna = true;
                break;
            }
        }

        if (!ktorakolwiekAktywna)
        {
            Debug.LogWarning("LoadGame: w zapisie zadna grupa poziomu nie jest aktywna. " +
                             "Pomijam przywracanie poziomow, zeby nie zgasic calej sceny. " +
                             "Prawdopodobnie przy zapisie grupa aktywnego poziomu nie zostala rozpoznana.");
            return;
        }

        Dictionary<string, bool> poId = new Dictionary<string, bool>();
        foreach (LevelGroupSaveData grupa in zapisane)
        {
            if (grupa != null && !string.IsNullOrEmpty(grupa.objectId))
                poId[grupa.objectId] = grupa.active;
        }

        int przywrocone = 0;

        foreach (Transform t in ZnajdzGrupyPoziomow())
        {
            bool active;
            if (!poId.TryGetValue(BuildObjectID(t.gameObject), out active))
                continue;

            t.gameObject.SetActive(active);
            przywrocone++;

            Debug.Log("LoadGame: grupa poziomu '" + t.name + "' aktywna: " + active);
        }

        Debug.Log("LoadGame: przywrocono stan " + przywrocone + " z " + zapisane.Count + " grup poziomow.");
    }

    // Zapisuje cel patrzenia kazdej kamery postaci (kolejnosc jak w playersCamera).
    private List<string> ZbierzCeleKamer()
    {
        List<string> cele = new List<string>();

        CinemachineCamera[] kamery = SwitchCharacter.Instance.playersCamera;
        if (kamery == null)
            return cele;

        foreach (CinemachineCamera kamera in kamery)
        {
            CameraController controller = kamera != null ? kamera.GetComponent<CameraController>() : null;
            Transform cel = controller != null ? controller.CurrentLookAtTarget : null;

            cele.Add(cel != null ? BuildObjectID(cel.gameObject) : "");
        }

        Debug.Log("SaveGame: zapisuje cele kamer: " + string.Join(" | ", cele));
        return cele;
    }

    private void PrzywrocCeleKamer(List<string> zapisaneID)
    {
        if (zapisaneID == null || zapisaneID.Count == 0)
        {
            Debug.Log("LoadGame: brak celow kamer w zapisie.");
            return;
        }

        CinemachineCamera[] kamery = SwitchCharacter.Instance.playersCamera;
        if (kamery == null)
            return;

        // Budujemy mape sciezka -> Transform raz, zeby nie skanowac sceny w petli.
        Dictionary<string, Transform> poSciezce = new Dictionary<string, Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null)
                poSciezce[BuildObjectID(t.gameObject)] = t;
        }

        int przywrocone = 0;

        for (int i = 0; i < kamery.Length && i < zapisaneID.Count; i++)
        {
            if (string.IsNullOrEmpty(zapisaneID[i]))
                continue;

            CameraController controller = kamery[i] != null ? kamery[i].GetComponent<CameraController>() : null;
            if (controller == null)
                continue;

            Transform cel;
            if (!poSciezce.TryGetValue(zapisaneID[i], out cel))
            {
                Debug.LogWarning("LoadGame: nie znaleziono celu kamery '" + zapisaneID[i] + "'.");
                continue;
            }

            controller.ForceLookAtTarget(cel);
            przywrocone++;
        }

        Debug.Log("LoadGame: przywrocono cele " + przywrocone + " kamer.");
    }

    // Zwraca wszystkie NPC w scenie. Konwencja: nazwa obiektu konczy sie na "_NPC".
    private static Transform[] ZnajdzNpc()
    {
        List<Transform> npc = new List<Transform>();

        Transform[] wszystkie = FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in wszystkie)
        {
            if (t != null && t.name.EndsWith("_NPC"))
                npc.Add(t);
        }

        return npc.ToArray();
    }

    private List<NpcSaveData> ZbierzNpc()
    {
        List<NpcSaveData> lista = new List<NpcSaveData>();

        foreach (Transform t in ZnajdzNpc())
        {
            NpcSaveData npc = new NpcSaveData();
            npc.objectId = BuildObjectID(t.gameObject);
            npc.position = t.position;
            npc.eulerAngles = t.eulerAngles;
            npc.active = t.gameObject.activeSelf;
            lista.Add(npc);
        }

        Debug.Log("SaveGame: zapisuje pozycje NPC: " + lista.Count);
        return lista;
    }

    private void PrzywrocNpc(List<NpcSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
        {
            Debug.Log("LoadGame: brak pozycji NPC w zapisie.");
            return;
        }

        Dictionary<string, NpcSaveData> poId = new Dictionary<string, NpcSaveData>();
        foreach (NpcSaveData npc in zapisane)
        {
            if (npc != null && !string.IsNullOrEmpty(npc.objectId))
                poId[npc.objectId] = npc;
        }

        int przywrocone = 0;

        foreach (Transform t in ZnajdzNpc())
        {
            NpcSaveData npc;
            if (!poId.TryGetValue(BuildObjectID(t.gameObject), out npc))
                continue;

            t.gameObject.SetActive(npc.active);

            // NavMeshAgent trzeba przenosic przez Warp, inaczej wroci na stare miejsce.
            UnityEngine.AI.NavMeshAgent agent = t.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool przeniesiony = false;

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.ResetPath();
                przeniesiony = agent.Warp(npc.position);
            }

            if (!przeniesiony)
                t.position = npc.position;

            t.eulerAngles = npc.eulerAngles;
            przywrocone++;
        }

        Debug.Log("LoadGame: przywrocono pozycje " + przywrocone + " z " + zapisane.Count + " NPC.");

        if (przywrocone < zapisane.Count)
        {
            Debug.LogWarning("LoadGame: nie znaleziono czesci NPC. Sprawdz, czy nie zmienily sie " +
                             "nazwy obiektow lub ich miejsce w hierarchii.");
        }
    }

    // Zbiera ideaId wszystkich odkrytych punktow sledztwa w scenie.
    private List<string> ZbierzOdkrytePunkty()
    {
        List<string> odkryte = new List<string>();

        DetectiveIdeaPoint[] punkty = FindObjectsByType<DetectiveIdeaPoint>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (DetectiveIdeaPoint punkt in punkty)
        {
            if (punkt == null || !punkt.IsDiscovered)
                continue;

            if (string.IsNullOrWhiteSpace(punkt.ideaId))
            {
                Debug.LogWarning("SaveGame: punkt '" + punkt.name + "' jest odkryty, ale ma puste pole 'Idea Id'. " +
                                 "Nie zostanie zapisany.", punkt);
                continue;
            }

            odkryte.Add(punkt.ideaId);
        }

        Debug.Log("SaveGame: zapisuje odkryte punkty sledztwa: " + odkryte.Count + " z " + punkty.Length + ".");
        return odkryte;
    }

    // Oznacza punkty jako odkryte. DiscoverPoint wywoluje zdarzenie OnDiscovered,
    // dzieki czemu CluesLog sam przelicza licznik "Zbadaj pomieszczenie".
    private void PrzywrocOdkrytePunkty(List<string> zapisaneID)
    {
        if (zapisaneID == null || zapisaneID.Count == 0)
        {
            Debug.Log("LoadGame: brak odkrytych punktow sledztwa w zapisie.");
            return;
        }

        if (DetectiveIdeaManager.Instance == null)
        {
            Debug.LogWarning("LoadGame: brak DetectiveIdeaManager.Instance - punkty sledztwa nie zostana przywrocone.");
            return;
        }

        HashSet<string> szukane = new HashSet<string>(zapisaneID);

        DetectiveIdeaPoint[] punkty = FindObjectsByType<DetectiveIdeaPoint>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int przywrocone = 0;

        foreach (DetectiveIdeaPoint punkt in punkty)
        {
            if (punkt == null || string.IsNullOrWhiteSpace(punkt.ideaId))
                continue;

            if (!szukane.Contains(punkt.ideaId))
                continue;

            DetectiveIdeaManager.Instance.DiscoverPoint(punkt);
            przywrocone++;
        }

        Debug.Log("LoadGame: przywrocono " + przywrocone + " z " + zapisaneID.Count + " odkrytych punktow sledztwa.");

        if (przywrocone < zapisaneID.Count)
        {
            Debug.LogWarning("LoadGame: nie znaleziono czesci punktow. Zapisane ID: " +
                             string.Join(" | ", zapisaneID));
        }
    }

    [ContextMenu("Pokaz sciezke zapisu")]
    public void PokazSciezke()
    {
        Debug.Log("Sciezka zapisu: " + Path.Combine(Application.persistentDataPath, "savegame.json"));
    }
}