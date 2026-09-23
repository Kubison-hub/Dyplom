using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;

using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;
using Component = UnityEngine.Component;
using Random = UnityEngine.Random;

public class SaveLoadManager : MonoBehaviour
{
    // Wersja formatu zapisu. Podnies o 1 po kazdej zmianie struktury GameData.
    // Pliki z inna wersja sa ignorowane - lepiej zaczac od nowa niz wczytac
    // polowe danych i dostac czarny ekran.
    public const int WERSJA_ZAPISU = 5;

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
            StartCoroutine(WczytajPoStartachInnychSkryptow());

            PlayerPrefs.SetInt("LoadGameOnStart", 0);
            PlayerPrefs.Save();
        }
    }

    // Unity nie gwarantuje kolejnosci Start() miedzy skryptami. Gdy wczytywalismy
    // od razu w Start(), skrypty uruchomione po nas (np. system odkrywania
    // pomieszczen) nadpisywaly przywrocony stan - stad czarne plyty na terenie,
    // ktory byl juz odkryty, i dzwiek "odkrycia" przy wczytaniu.
    // Czekamy dwie klatki: po nich wszystkie Start() i pierwsze Update() sa za nami.
    private System.Collections.IEnumerator WczytajPoStartachInnychSkryptow()
    {
        yield return null;
        yield return null;

        LoadGame();
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
        data.saveVersion = WERSJA_ZAPISU;

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

        // --- Stan zagadek: flagi, pozycje, animatory ---
        ZbierzStanZagadek(data);

        // --- Niesione lampy (piwnica) ---
        if (Lvl3LampVisualManager.Instance != null)
        {
            GameObject lampaSherlocka = Lvl3LampVisualManager.Instance.GetCarriedLampForSave(false);
            GameObject lampaWatsona = Lvl3LampVisualManager.Instance.GetCarriedLampForSave(true);

            data.sherlockCarriedLampId = lampaSherlocka != null ? BuildObjectID(lampaSherlocka) : "";
            data.watsonCarriedLampId = lampaWatsona != null ? BuildObjectID(lampaWatsona) : "";

            Debug.Log("SaveGame: niesione lampy - Sherlock: '" + data.sherlockCarriedLampId +
                      "', Watson: '" + data.watsonCarriedLampId + "'.");
        }

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

            // Kopia zapasowa poprzedniego zapisu - gdyby nowy wyszedl wadliwy,
            // jest do czego wrocic bez zaczynania gry od poczatku.
            if (File.Exists(path))
            {
                try
                {
                    File.Copy(path, path + ".bak", true);
                }
                catch (System.Exception kopia)
                {
                    Debug.LogWarning("SaveGame: nie udalo sie zrobic kopii zapasowej: " + kopia.Message);
                }
            }

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
            Debug.LogError("LoadGame: plik zapisu jest uszkodzony lub pusty. Pomijam wczytywanie.");
            return;
        }

        if (data.saveVersion != WERSJA_ZAPISU)
        {
            Debug.LogWarning("LoadGame: plik zapisu pochodzi z wersji " + data.saveVersion +
                             ", a gra uzywa wersji " + WERSJA_ZAPISU + ". " +
                             "Pomijam wczytywanie, zeby nie uszkodzic sceny - gra startuje od poczatku. " +
                             "Zrob nowy zapis, zeby nadpisac stary plik.");
            return;
        }

        // --- Aktywne grupy poziomow: MUSZA byc pierwsze ---
        // Reszta (pozycje, NPC, przedmioty) odnosi sie do obiektow wewnatrz
        // tych grup. Na wylaczonej grupie nic nie da sie odnalezc.
        Krok("poziomy", () => PrzywrocGrupyPoziomow(data.levelGroups));

        // --- Stan zagadek: flagi, pozycje, animatory ---
        // Po wlaczeniu grup, zeby objac takze obiekty dopiero co odsloniete.
        Krok("stan zagadek", () => PrzywrocStanZagadek(data));

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
        Krok("ekwipunek", () => PrzywrocEkwipunek(data.itemsInInventory));

        // --- Usuwamy ze sceny przedmioty juz podniesione ---
        // Po SpawnHiddenItems, zeby objac takze przedmioty dopiero co odsloniete.
        Krok("podniesione przedmioty", () => UsunPodniesionePrzedmioty(data.pickedUpItemIDs));

        // --- Ukonczone interakcje ---
        Krok("ukonczone interakcje", () => PrzywrocUkonczoneInterakcje(data.completedInteractionIDs));

        // --- Pozycje NPC ---
        Krok("NPC", () => PrzywrocNpc(data.npcs));
        Krok("niesione lampy", () => PrzywrocNiesioneLampy(data));

        // --- Odkryte punkty sledztwa ---
        // MUSI byc przed CluesLog: panel przelicza licznik "Zbadaj pomieszczenie"
        // po stanie punktow, wiec bez tego pierwsze nowe odkrycie zerowaloby licznik.
        Krok("punkty sledztwa", () => PrzywrocOdkrytePunkty(data.discoveredIdeaPointIDs));

        // --- Badanie ciala Lady Edith ---
        // Przed CluesLog: skrypt sam wypycha licznik do panelu.
        Int_EdithExamBody edithExam = FindFirstObjectByType<Int_EdithExamBody>(FindObjectsInactive.Include);
        if (edithExam != null)
            Krok("badanie ciala", () => edithExam.RestoreSaveState(data.edithExam));
        else
            Debug.LogWarning("LoadGame: nie znaleziono Int_EdithExamBody - badanie ciala nie zostanie przywrocone.");

        // --- Panel zadan (lista celow) ---
        if (CluesLog.Instance != null)
            Krok("panel zadan", () => CluesLog.Instance.RestoreSaveState(data.questLog));
        else
            Debug.LogWarning("LoadGame: brak CluesLog.Instance - postep celow nie zostanie przywrocony.");

        // --- Etap samouczka: na koncu, zeby nic go pozniej nie nadpisalo ---
        if (TutorialTimeline.Instance != null)
            Krok("samouczek", () => TutorialTimeline.Instance.RestoreSaveStage(data.tutorialStage));
        else
            Debug.LogWarning("LoadGame: brak TutorialTimeline.Instance - sekwencja startowa poleci od nowa.");

        // --- Notatnik ---
        if (NotebookManager.Instance != null)
            Krok("notatnik", () => NotebookManager.Instance.RestoreNotes(data.notebookNotes, data.notebookUnreadNotes));
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
            Krok("cele kamer", () => PrzywrocCeleKamer(data.cameraLookAtIDs));
            Krok("aktywna postac", () => SwitchCharacter.Instance.RestoreActivePlayer(data.activePlayerIndex));
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

    // Zwraca korzen hierarchii poziomow - obiekt z "LEVELS" w nazwie.
    private static Transform ZnajdzKorzenPoziomow()
    {
        Transform[] wszystkie = FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in wszystkie)
        {
            if (t != null && t.name.IndexOf("LEVELS", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }

        return null;
    }

    // Wszystkie obiekty w drzewie poziomow (korzen + potomkowie).
    private static void ZbierzDrzewo(Transform korzen, List<Transform> wynik)
    {
        if (korzen == null)
            return;

        wynik.Add(korzen);

        for (int i = 0; i < korzen.childCount; i++)
            ZbierzDrzewo(korzen.GetChild(i), wynik);
    }

    // Zapisujemy sciezki obiektow WYLACZONYCH. Przejscie na kolejny poziom
    // wlacza nie tylko grupe LEVEL_x, ale i obiekty w jej wnetrzu (pokoje,
    // swiatla), wiec sam stan grupy nie wystarcza.
    private List<LevelGroupSaveData> ZbierzGrupyPoziomow()
    {
        List<LevelGroupSaveData> lista = new List<LevelGroupSaveData>();

        Transform korzen = ZnajdzKorzenPoziomow();
        if (korzen == null)
        {
            Debug.LogWarning("SaveGame: nie znaleziono obiektu z 'LEVELS' w nazwie. " +
                             "Stan poziomow NIE zostanie zapisany.");
            return lista;
        }

        List<Transform> drzewo = new List<Transform>();
        ZbierzDrzewo(korzen, drzewo);

        foreach (Transform t in drzewo)
        {
            if (t == null || t.gameObject.activeSelf)
                continue;

            LevelGroupSaveData wpis = new LevelGroupSaveData();
            wpis.objectId = BuildObjectID(t.gameObject);
            wpis.active = false;
            lista.Add(wpis);
        }

        Debug.Log("SaveGame: drzewo poziomow '" + korzen.name + "' - obiektow: " + drzewo.Count +
                  ", wylaczonych: " + lista.Count + ".");

        return lista;
    }

    private void PrzywrocGrupyPoziomow(List<LevelGroupSaveData> zapisane)
    {
        if (zapisane == null)
        {
            Debug.Log("LoadGame: brak stanu poziomow w zapisie.");
            return;
        }

        Transform korzen = ZnajdzKorzenPoziomow();
        if (korzen == null)
        {
            Debug.LogWarning("LoadGame: nie znaleziono obiektu z 'LEVELS' w nazwie. " +
                             "Stan poziomow nie zostanie przywrocony.");
            return;
        }

        HashSet<string> wylaczone = new HashSet<string>();
        foreach (LevelGroupSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.objectId))
                wylaczone.Add(wpis.objectId);
        }

        List<Transform> drzewo = new List<Transform>();
        ZbierzDrzewo(korzen, drzewo);

        int wlaczone = 0;
        int zgaszone = 0;

        foreach (Transform t in drzewo)
        {
            if (t == null)
                continue;

            bool maBycAktywny = !wylaczone.Contains(BuildObjectID(t.gameObject));

            if (t.gameObject.activeSelf == maBycAktywny)
                continue;

            t.gameObject.SetActive(maBycAktywny);

            if (maBycAktywny)
                wlaczone++;
            else
                zgaszone++;
        }

        Debug.Log("LoadGame: stan poziomow przywrocony (wlaczono: " + wlaczone +
                  ", wylaczono: " + zgaszone + ", obiektow w drzewie: " + drzewo.Count + ").");
    }

    // ---------------------------------------------------------------
    // STAN ZAGADEK W DRZEWIE POZIOMOW
    // ---------------------------------------------------------------

    private const BindingFlags FlagiPol =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static string KluczFlagi(GameObject go, System.Type typ, string pole)
    {
        return BuildObjectID(go) + "|" + typ.Name + "|" + pole;
    }

    private static string KluczKomponentu(GameObject go, Component komponent, int indeks)
    {
        return BuildObjectID(go) + "|#" + indeks + "|" + komponent.GetType().Name + "|enabled";
    }

    // Renderer i Collider nie dziedzicza po Behaviour, wiec obsluga jest osobna.
    private static bool SprobujOdczytacWlaczenie(Component komponent, out bool wlaczony)
    {
        wlaczony = true;

        if (komponent == null)
            return false;

        Renderer renderer = komponent as Renderer;
        if (renderer != null)
        {
            wlaczony = renderer.enabled;
            return true;
        }

        Collider collider = komponent as Collider;
        if (collider != null)
        {
            wlaczony = collider.enabled;
            return true;
        }

        // Behaviour to m.in. Light, MonoBehaviour, Animator.
        Behaviour behaviour = komponent as Behaviour;
        if (behaviour != null)
        {
            wlaczony = behaviour.enabled;
            return true;
        }

        return false;
    }

    private static bool SprobujUstawicWlaczenie(Component komponent, bool wlaczony)
    {
        if (komponent == null)
            return false;

        Renderer renderer = komponent as Renderer;
        if (renderer != null)
        {
            renderer.enabled = wlaczony;
            return true;
        }

        Collider collider = komponent as Collider;
        if (collider != null)
        {
            collider.enabled = wlaczony;
            return true;
        }

        Behaviour behaviour = komponent as Behaviour;
        if (behaviour != null)
        {
            behaviour.enabled = wlaczony;
            return true;
        }

        return false;
    }

    private void ZbierzStanZagadek(GameData data)
    {
        data.levelFlags = new List<ScriptFlagSaveData>();
        data.levelTransforms = new List<TransformSaveData>();
        data.levelAnimators = new List<AnimatorSaveData>();

        Transform korzen = ZnajdzKorzenPoziomow();
        if (korzen == null)
            return;

        List<Transform> drzewo = new List<Transform>();
        ZbierzDrzewo(korzen, drzewo);

        foreach (Transform t in drzewo)
        {
            if (t == null)
                continue;

            // 1. Pozycja i obrot - lapie przesuniete sciany, przyciski, dzwignie.
            TransformSaveData trans = new TransformSaveData();
            trans.objectId = BuildObjectID(t.gameObject);
            trans.parentId = t.parent != null ? BuildObjectID(t.parent.gameObject) : "";
            trans.localPosition = t.localPosition;
            trans.localEuler = t.localEulerAngles;
            data.levelTransforms.Add(trans);

            // 2. Animator - stan otwartych drzwi.
            Animator animator = t.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                data.levelAnimators.Add(ZbierzAnimator(t.gameObject, animator));

            // 3. Wlaczenie komponentow - MeshRenderer, Light, Collider.
            //    Czesc rzeczy (np. czarne plyty zakrywajace pokoje, kolidery
            //    zagadek) jest gaszona przez wylaczenie komponentu, a nie
            //    calego obiektu - samo activeSelf tego nie zlapie.
            Component[] komponenty = t.GetComponents<Component>();

            for (int i = 0; i < komponenty.Length; i++)
            {
                bool wlaczony;

                if (!SprobujOdczytacWlaczenie(komponenty[i], out wlaczony))
                    continue;

                ScriptFlagSaveData wpis = new ScriptFlagSaveData();
                wpis.key = KluczKomponentu(t.gameObject, komponenty[i], i);
                wpis.value = wlaczony;
                data.levelFlags.Add(wpis);
            }

            // 4. Flagi bool w skryptach - 'performed', 'isOpen', 'canOpen'.
            foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
            {
                if (mb == null)
                    continue;

                foreach (FieldInfo pole in mb.GetType().GetFields(FlagiPol))
                {
                    if (pole.FieldType != typeof(bool))
                        continue;

                    ScriptFlagSaveData flaga = new ScriptFlagSaveData();
                    flaga.key = KluczFlagi(t.gameObject, mb.GetType(), pole.Name);

                    try
                    {
                        flaga.value = (bool)pole.GetValue(mb);
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }

                    data.levelFlags.Add(flaga);
                }
            }
        }

        data.solvedSequencePuzzles = new List<string>();

        foreach (DetectiveSequencePuzzle zagadka in FindObjectsByType<DetectiveSequencePuzzle>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zagadka != null && zagadka.IsSolved)
                data.solvedSequencePuzzles.Add(BuildObjectID(zagadka.gameObject));
        }

        Debug.Log("SaveGame: rozwiazanych zagadek sekwencyjnych: " + data.solvedSequencePuzzles.Count);

        Debug.Log("SaveGame: stan zagadek - flag: " + data.levelFlags.Count +
                  ", pozycji: " + data.levelTransforms.Count +
                  ", animatorow: " + data.levelAnimators.Count + ".");
    }

    private static AnimatorSaveData ZbierzAnimator(GameObject go, Animator animator)
    {
        AnimatorSaveData dane = new AnimatorSaveData();
        dane.objectId = BuildObjectID(go);
        dane.boolNames = new List<string>();
        dane.boolValues = new List<bool>();
        dane.floatNames = new List<string>();
        dane.floatValues = new List<float>();
        dane.intNames = new List<string>();
        dane.intValues = new List<int>();

        if (animator.layerCount > 0)
        {
            AnimatorStateInfo stan = animator.GetCurrentAnimatorStateInfo(0);
            dane.stateHash = stan.fullPathHash;
            dane.normalizedTime = stan.normalizedTime;
        }

        foreach (AnimatorControllerParameter parametr in animator.parameters)
        {
            switch (parametr.type)
            {
                case AnimatorControllerParameterType.Bool:
                    dane.boolNames.Add(parametr.name);
                    dane.boolValues.Add(animator.GetBool(parametr.name));
                    break;
                case AnimatorControllerParameterType.Float:
                    dane.floatNames.Add(parametr.name);
                    dane.floatValues.Add(animator.GetFloat(parametr.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    dane.intNames.Add(parametr.name);
                    dane.intValues.Add(animator.GetInteger(parametr.name));
                    break;
            }
        }

        return dane;
    }

    private void PrzywrocStanZagadek(GameData data)
    {
        Transform korzen = ZnajdzKorzenPoziomow();
        if (korzen == null)
            return;

        List<Transform> drzewo = new List<Transform>();
        ZbierzDrzewo(korzen, drzewo);

        // Mapa sciezka -> Transform, zeby nie skanowac drzewa w petli.
        Dictionary<string, Transform> poSciezce = new Dictionary<string, Transform>();
        foreach (Transform t in drzewo)
        {
            if (t != null)
                poSciezce[BuildObjectID(t.gameObject)] = t;
        }

        // Zagadki potrafia przepinac obiekty pod innego rodzica - odtwarzamy to
        // PRZED pozycjami, inaczej localPosition odnosilaby sie do zlego rodzica.
        PrzywrocRodzicow(data.levelTransforms, poSciezce);

        int pozycje = PrzywrocPozycjeObiektow(data.levelTransforms, poSciezce);
        int flagi = PrzywrocFlagiSkryptow(data.levelFlags, poSciezce);
        int komponenty = PrzywrocWlaczenieKomponentow(data.levelFlags, poSciezce);
        int animatory = PrzywrocAnimatory(data.levelAnimators, poSciezce);

        PrzywrocRozwiazaneZagadki(data.solvedSequencePuzzles);

        Debug.Log("LoadGame: stan zagadek przywrocony (pozycji: " + pozycje +
                  ", flag: " + flagi + ", komponentow: " + komponenty +
                  ", animatorow: " + animatory + ").");
    }

    private void PrzywrocNiesioneLampy(GameData data)
    {
        if (Lvl3LampVisualManager.Instance == null)
        {
            Debug.Log("LoadGame: brak Lvl3LampVisualManager - pomijam niesione lampy.");
            return;
        }

        Lvl3LampVisualManager.Instance.RestoreCarriedLamp(false, ZnajdzObiektPoSciezce(data.sherlockCarriedLampId));
        Lvl3LampVisualManager.Instance.RestoreCarriedLamp(true, ZnajdzObiektPoSciezce(data.watsonCarriedLampId));
    }

    // Odnajduje obiekt w scenie po sciezce w hierarchii (takze nieaktywny).
    private static GameObject ZnajdzObiektPoSciezce(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
            return null;

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && BuildObjectID(t.gameObject) == objectId)
                return t.gameObject;
        }

        Debug.LogWarning("LoadGame: nie znaleziono obiektu '" + objectId + "'.");
        return null;
    }

    private static void PrzywrocRodzicow(
        List<TransformSaveData> zapisane, Dictionary<string, Transform> poSciezce)
    {
        if (zapisane == null)
            return;

        int przepiete = 0;

        foreach (TransformSaveData dane in zapisane)
        {
            if (dane == null || string.IsNullOrEmpty(dane.parentId))
                continue;

            Transform obiekt;
            if (!poSciezce.TryGetValue(dane.objectId, out obiekt) || obiekt == null)
                continue;

            Transform rodzic;
            if (!poSciezce.TryGetValue(dane.parentId, out rodzic) || rodzic == null)
                continue;

            if (obiekt.parent == rodzic)
                continue;

            obiekt.SetParent(rodzic, false);
            przepiete++;
        }

        if (przepiete > 0)
            Debug.Log("LoadGame: przepieto " + przepiete + " obiektow pod zapisanych rodzicow.");
    }

    private void PrzywrocRozwiazaneZagadki(List<string> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        HashSet<string> rozwiazane = new HashSet<string>(zapisane);
        int licznik = 0;

        foreach (DetectiveSequencePuzzle zagadka in FindObjectsByType<DetectiveSequencePuzzle>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zagadka == null || !rozwiazane.Contains(BuildObjectID(zagadka.gameObject)))
                continue;

            zagadka.RestoreSolvedState();
            licznik++;
        }

        Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count +
                  " rozwiazanych zagadek sekwencyjnych.");
    }

    private static int PrzywrocPozycjeObiektow(
        List<TransformSaveData> zapisane, Dictionary<string, Transform> poSciezce)
    {
        if (zapisane == null)
            return 0;

        int licznik = 0;

        foreach (TransformSaveData dane in zapisane)
        {
            Transform t;
            if (dane == null || !poSciezce.TryGetValue(dane.objectId, out t) || t == null)
                continue;

            t.localPosition = dane.localPosition;
            t.localEulerAngles = dane.localEuler;
            licznik++;
        }

        return licznik;
    }

    private static int PrzywrocFlagiSkryptow(
        List<ScriptFlagSaveData> zapisane, Dictionary<string, Transform> poSciezce)
    {
        if (zapisane == null)
            return 0;

        Dictionary<string, bool> mapa = new Dictionary<string, bool>();
        foreach (ScriptFlagSaveData flaga in zapisane)
        {
            if (flaga != null && !string.IsNullOrEmpty(flaga.key))
                mapa[flaga.key] = flaga.value;
        }

        int licznik = 0;

        foreach (Transform t in poSciezce.Values)
        {
            if (t == null)
                continue;

            foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
            {
                if (mb == null)
                    continue;

                System.Type typ = mb.GetType();

                foreach (FieldInfo pole in typ.GetFields(FlagiPol))
                {
                    if (pole.FieldType != typeof(bool))
                        continue;

                    bool wartosc;
                    if (!mapa.TryGetValue(KluczFlagi(t.gameObject, typ, pole.Name), out wartosc))
                        continue;

                    try
                    {
                        pole.SetValue(mb, wartosc);
                        licznik++;
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
        }

        return licznik;
    }

    private static int PrzywrocWlaczenieKomponentow(
        List<ScriptFlagSaveData> zapisane, Dictionary<string, Transform> poSciezce)
    {
        if (zapisane == null)
            return 0;

        Dictionary<string, bool> mapa = new Dictionary<string, bool>();
        foreach (ScriptFlagSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.key) && wpis.key.EndsWith("|enabled"))
                mapa[wpis.key] = wpis.value;
        }

        if (mapa.Count == 0)
            return 0;

        int licznik = 0;

        foreach (Transform t in poSciezce.Values)
        {
            if (t == null)
                continue;

            Component[] komponenty = t.GetComponents<Component>();

            for (int i = 0; i < komponenty.Length; i++)
            {
                if (komponenty[i] == null)
                    continue;

                bool wlaczony;
                if (!mapa.TryGetValue(KluczKomponentu(t.gameObject, komponenty[i], i), out wlaczony))
                    continue;

                if (SprobujUstawicWlaczenie(komponenty[i], wlaczony))
                    licznik++;
            }
        }

        return licznik;
    }

    private static int PrzywrocAnimatory(
        List<AnimatorSaveData> zapisane, Dictionary<string, Transform> poSciezce)
    {
        if (zapisane == null)
            return 0;

        int licznik = 0;

        foreach (AnimatorSaveData dane in zapisane)
        {
            Transform t;
            if (dane == null || !poSciezce.TryGetValue(dane.objectId, out t) || t == null)
                continue;

            Animator animator = t.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;

            for (int i = 0; i < dane.boolNames.Count; i++)
                animator.SetBool(dane.boolNames[i], dane.boolValues[i]);

            for (int i = 0; i < dane.floatNames.Count; i++)
                animator.SetFloat(dane.floatNames[i], dane.floatValues[i]);

            for (int i = 0; i < dane.intNames.Count; i++)
                animator.SetInteger(dane.intNames[i], dane.intValues[i]);

            if (dane.stateHash != 0)
            {
                // Odtwarzamy stan od razu w miejscu, w ktorym byl przy zapisie -
                // dzieki temu otwarte drzwi zostaja otwarte, bez animacji.
                animator.Play(dane.stateHash, 0, dane.normalizedTime);
                animator.Update(0f);
            }

            licznik++;
        }

        return licznik;
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

    // Uruchamia jeden etap wczytywania tak, zeby jego blad nie przerwal reszty.
    // Bez tego pojedynczy wyjatek (np. w cudzym skrypcie) zostawia scene
    // w polowie wczytana - czyli zwykle z czarnym ekranem.
    private static void Krok(string nazwa, System.Action akcja)
    {
        try
        {
            akcja();
        }
        catch (System.Exception e)
        {
            Debug.LogError("LoadGame: etap '" + nazwa + "' nie powiodl sie: " + e.Message +
                           " Wczytywanie jest kontynuowane.");
        }
    }

    [ContextMenu("Pokaz sciezke zapisu")]
    public void PokazSciezke()
    {
        Debug.Log("Sciezka zapisu: " + Path.Combine(Application.persistentDataPath, "savegame.json"));
    }
}