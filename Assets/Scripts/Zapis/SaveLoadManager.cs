using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using DialogueEditor;

using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;
using Component = UnityEngine.Component;
using Random = UnityEngine.Random;

public class SaveLoadManager : MonoBehaviour
{
    // Bump for incompatible format changes; optional fields retain legacy compatibility.
    // Pliki z inna wersja sa ignorowane - lepiej zaczac od nowa niz wczytac
    // polowe danych i dostac czarny ekran.
    public const int WERSJA_ZAPISU = 31;

    [Header("Diagnostyka")]
    [Tooltip("Przywracanie wszystkich pol bool w skryptach pod LEVELS. " +
             "Odznacz, jesli po wczytaniu jakas interakcja przestaje reagowac - " +
             "to najszersza czesc zapisu i najczestsze zrodlo takich problemow.")]
    public bool przywracajFlagiSkryptow = true;

    [Tooltip("Nazwa obiektu do sledzenia w logu podczas wczytywania. " +
             "Zostaw puste, zeby wylaczyc. Przyklad: lv3_R2bb")]
    public string sledzonyObiekt = "";

    [Tooltip("Przywracanie licznikow int/float ze skryptow. Domyslnie WYLACZONE - " +
             "nadpisywalo tez pola interfejsu (rozmiary czcionek, odstepy), " +
             "przez co po wczytaniu psul sie wyglad UI.")]
    public bool przywracajLicznikiSkryptow = false;

    [Tooltip("Ile razy po wczytaniu ponowic ustawienie czarnych plyt. " +
             "Kilkanascie skryptow zapala je we wlasnym Start(), ktory " +
             "wykonuje sie PO wczytaniu - dlatego stan trzeba nalozyc ponownie.")]
    [Min(0)] public int powtorzeniaPlyt = 4;

    [Tooltip("Odstep miedzy powtorzeniami, w sekundach.")]
    [Min(0.05f)] public float odstepPowtorzenPlyt = 0.5f;

    public static SaveLoadManager Instance;

    // ID przedmiotow podniesionych w tej sesji/scenie.
    // Nie jest statyczne celowo: nowa scena = nowy manager = czysta lista (nowa gra).
    private readonly HashSet<string> collectedObjectIDs = new HashSet<string>();

    // Wycisza logi przy ponawianiu stanu plyt, zeby nie zalewac konsoli.
    private bool cichePowtorzenie;

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
        string path = SegmentSciezki(t);
        Transform parent = t.parent;

        while (parent != null)
        {
            path = SegmentSciezki(parent) + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Jeden segment sciezki. Gdy rodzenstwo ma powtarzajace sie nazwy
    // (a w scenie zdarza sie to czesto, np. dwa "BrickPillar_01 (65)"
    // w jednym rodzicu), dopisujemy numer porzadkowy. Bez tego oba obiekty
    // maja identyczne ID, jeden nadpisuje drugiego w zapisie i po wczytaniu
    // dostaja wspolny stan - stad znikajace kukly, lampy i postacie.
    private static string SegmentSciezki(Transform t)
    {
        Transform parent = t.parent;

        if (parent == null)
            return t.name;

        int powtorzenia = 0;

        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == t.name)
                powtorzenia++;
        }

        if (powtorzenia <= 1)
            return t.name;

        return t.name + "#" + t.GetSiblingIndex();
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

        // --- Stan obiektow pod postaciami (swiatla lamp) ---
        ZbierzDrzewoPostaci(data);

        // --- Lampy: pozycja, rodzic i stan ---
        data.lamps = ZbierzLampy();
        data.wallLamps = ZbierzUchwytyLamp();
        data.npcRenderers = ZbierzRendereryNpc();
        // WYLACZONE. Czytanie ParameterList i wywolywanie DeserializeForEditor()
        // w czasie gry rozregulowywalo dialogi, a ustawianie IntValue zmienialo
        // sam asset rozmowy. Warianty dialogow musza byc zapisywane przez
        // wlasne flagi skryptow, nie przez wnetrznosci Dialogue Editora.
        data.conversationFlags = new List<ScriptFlagSaveData>();
        data.conversationNumbers = new List<ConversationNumberSaveData>();
        data.sceneAnimators = ZbierzAnimatorySceny();
        data.movableObjects = ZbierzRuchomeObiekty();
        ZbierzSledztwo(data);
        data.scriptNumbers = ZbierzLicznikiSkryptow();

        data.triggeredMannequinTraps = new List<string>();
        data.unlockedBasementLadderIDs = new List<string>();
        foreach (var ladder in FindObjectsByType<Int_Lvl_3_Leadder>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ladder.CanGo)
                data.unlockedBasementLadderIDs.Add(ladder.ExitPermissionSaveId);
        }

        foreach (Int_lv3_Manequine pulapka in FindObjectsByType<Int_lv3_Manequine>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pulapka != null && pulapka.IsTrapTriggered)
                data.triggeredMannequinTraps.Add(BuildObjectID(pulapka.gameObject));
        }

        Debug.Log("SaveGame: uruchomionych pulapek manekina: " + data.triggeredMannequinTraps.Count + ".");

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
        Sledz("po poziomach");
        Krok("stan zagadek", () => PrzywrocStanZagadek(data));
        Sledz("po stanie zagadek");

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
        Krok("drzewo postaci", () => PrzywrocDrzewoPostaci(data));
        // UWAGA: ogolnego PrzywrocLampy juz NIE wywolujemy. Dopasowywalo lampy
        // po nazwie, a w scenie jest 11 uchwytow i nazwy typu "lvl3_HeldLamp 1"
        // powtarzaja sie - przez to aktywowalo przypadkowa lampe na jej
        // pierwotnym miejscu i powstawal duplikat. Zawieszone lampy odtwarza
        // teraz sam uchwyt, ktory zna swoje referencje z Inspectora.
        Krok("uchwyty lamp", () => PrzywrocUchwytyLamp(data.wallLamps));
        Krok("renderery NPC", () => PrzywrocRendereryNpc(data.npcRenderers));
        // WYLACZONE - patrz uwaga przy zapisie.
        Krok("animatory sceny", () => PrzywrocAnimatorySceny(data.sceneAnimators));
        Krok("ruchome obiekty", () => PrzywrocRuchomeObiekty(data.movableObjects));
        Krok("sledztwo", () => PrzywrocSledztwo(data));

        if (przywracajLicznikiSkryptow)
            Krok("liczniki skryptow", () => PrzywrocLicznikiSkryptow(data.scriptNumbers));
        Krok("pulapki manekina", () => PrzywrocPulapkiManekina(data.triggeredMannequinTraps));
        Krok("wyjscie Ethel z piwnicy", () => PrzywrocWyjsciaZPiwnicy(data.unlockedBasementLadderIDs));
        Krok("niesione lampy", () => PrzywrocNiesioneLampy(data));
        Krok("zebrane lampy piwnicy", PrzywrocZebraneLampyPiwnicy);

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

        // Wizja orla: jej flagi wracaja razem z flagami skryptow, ale to,
        // co wizja podswietla, trzeba przeliczyc osobno.
        Krok("wizja orla", () =>
        {
            if (EagleVisionSystem.Instance != null)
            {
                EagleVisionSystem.Instance.RefreshScan();
                Debug.Log("LoadGame: odswiezono wizje orla (aktywna: " +
                          EagleVisionSystem.Instance.isActive + ").");
            }
        });

        Sledz("przed plytami");

        // Czarne plyty przywracamy NA KONCU - wczesniejsze etapy potrafia
        // wlaczac obiekty wewnatrz odkrytych pomieszczen.
        Krok("czarne plyty", () => PrzywrocCzarnePlyty(data.blackboards));
        Krok("usuniete plyty", () => PrzywrocUsunietePlyty(data.removedBlackboardTriggers));
        Krok("ukonczone stoly piwnicy", PrzywrocUkonczoneStolyPiwnicy);
        Krok("drzwi Ethel z odtwarzaniem zapisu", PrzywrocDrzwiEthel);

        // Skrypty w rodzaju Int_lv3_RemoveBlackboard zapalaja czarne plyty
        // we wlasnym Start(), a ten wykonuje sie dopiero PO wczytaniu - obiekty
        // z wylaczonych grup poziomu budza sie w chwili ich wlaczenia.
        // Dlatego nakladamy stan plyt jeszcze kilka razy przez najblizsze sekundy.
        if (powtorzeniaPlyt > 0)
            StartCoroutine(PonawiajPlyty(data.blackboards, data.removedBlackboardTriggers,
                                         data.npcs, data.wallLamps, data.npcRenderers));

        Sledz("po plytach");

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

        // Rendererow NIE zapisujemy. System DOCS (Dynamic Occlusion Cutout)
        // wylacza renderery scian zaslaniajacych postac i robi to na biezaco.
        // Utrwalenie tego stanu w zapisie powodowalo, ze po wczytaniu setki
        // obiektow zostawaly niewidoczne, a scena byla czarna.
        if (komponent is Renderer)
            return false;

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

        // Renderery pomijamy - patrz komentarz w SprobujOdczytacWlaczenie.
        if (komponent is Renderer)
            return false;

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
            trans.stableId = BuildStableTransformID(t);
            trans.parentStableId = BuildStableTransformID(t.parent);
            trans.objectName = t.name;
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

        // Flagi skryptow spoza drzewa LEVELS (interakcje na NPC, managery zagadek).
        // Bez nich takie skrypty po wczytaniu uruchamiaja sie od nowa.
        DolaczFlagiSpozaPoziomow(data.levelFlags, drzewo);

        data.blackboards = ZbierzCzarnePlyty(drzewo);

        // Wyzwalacze usuwania czarnych plyt.
        data.removedBlackboardTriggers = new List<string>();

        foreach (Int_lv3_RemoveBlackboard wyzwalacz in FindObjectsByType<Int_lv3_RemoveBlackboard>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (wyzwalacz != null && wyzwalacz.HasTriggered)
                data.removedBlackboardTriggers.Add(BuildObjectID(wyzwalacz.gameObject));
        }

        Debug.Log("SaveGame: usunietych czarnych plyt: " + data.removedBlackboardTriggers.Count + ".");

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
        int flagi = 0;

        if (przywracajFlagiSkryptow)
        {
            flagi = PrzywrocFlagiSkryptow(data.levelFlags, poSciezce);
            flagi += PrzywrocFlagiSpozaPoziomow(data.levelFlags, poSciezce);
        }
        else
        {
            Debug.LogWarning("LoadGame: przywracanie flag skryptow jest WYLACZONE w Inspectorze. " +
                             "Czesc zagadek moze nie pamietac swojego stanu.");
        }
        int komponenty = PrzywrocWlaczenieKomponentow(data.levelFlags, poSciezce);
        int animatory = PrzywrocAnimatory(data.levelAnimators, poSciezce);

        PrzywrocRozwiazaneZagadki(data.solvedSequencePuzzles);

        Debug.Log("LoadGame: stan zagadek przywrocony (pozycji: " + pozycje +
                  ", flag: " + flagi + ", komponentow: " + komponenty +
                  ", animatorow: " + animatory + ").");
    }

    // Korzenie obiektow obu postaci. Pod nimi wisza m.in. swiatla trzymanych lamp.
    private static List<Transform> ZnajdzKorzeniePostaci()
    {
        List<Transform> korzenie = new List<Transform>();

        if (SwitchCharacter.Instance != null && SwitchCharacter.Instance.players != null)
        {
            // Typ elementu zalezy od wersji SwitchCharacter, wiec go nie wymuszamy.
            foreach (var gracz in SwitchCharacter.Instance.players)
            {
                if (gracz != null)
                    korzenie.Add(gracz.transform.root);
            }
        }

        return korzenie;
    }

    private void ZbierzDrzewoPostaci(GameData data)
    {
        data.playerSubtree = new List<LevelGroupSaveData>();
        data.playerComponents = new List<ScriptFlagSaveData>();

        foreach (Transform korzen in ZnajdzKorzeniePostaci())
        {
            List<Transform> drzewo = new List<Transform>();
            ZbierzDrzewo(korzen, drzewo);

            foreach (Transform t in drzewo)
            {
                if (t == null)
                    continue;

                if (!t.gameObject.activeSelf)
                {
                    LevelGroupSaveData wpis = new LevelGroupSaveData();
                    wpis.objectId = BuildObjectID(t.gameObject);
                    wpis.active = false;
                    data.playerSubtree.Add(wpis);
                }

                Component[] komponenty = t.GetComponents<Component>();

                for (int i = 0; i < komponenty.Length; i++)
                {
                    bool wlaczony;
                    if (!SprobujOdczytacWlaczenie(komponenty[i], out wlaczony))
                        continue;

                    ScriptFlagSaveData wpis = new ScriptFlagSaveData();
                    wpis.key = KluczKomponentu(t.gameObject, komponenty[i], i);
                    wpis.value = wlaczony;
                    data.playerComponents.Add(wpis);
                }
            }
        }

        Debug.Log("SaveGame: drzewo postaci - wylaczonych obiektow: " + data.playerSubtree.Count +
                  ", komponentow: " + data.playerComponents.Count + ".");
    }

    private void PrzywrocDrzewoPostaci(GameData data)
    {
        if (data.playerSubtree == null && data.playerComponents == null)
            return;

        HashSet<string> wylaczone = new HashSet<string>();
        if (data.playerSubtree != null)
        {
            foreach (LevelGroupSaveData wpis in data.playerSubtree)
            {
                if (wpis != null && !string.IsNullOrEmpty(wpis.objectId))
                    wylaczone.Add(wpis.objectId);
            }
        }

        Dictionary<string, Transform> poSciezce = new Dictionary<string, Transform>();

        foreach (Transform korzen in ZnajdzKorzeniePostaci())
        {
            List<Transform> drzewo = new List<Transform>();
            ZbierzDrzewo(korzen, drzewo);

            foreach (Transform t in drzewo)
            {
                if (t == null)
                    continue;

                string id = BuildObjectID(t.gameObject);
                poSciezce[id] = t;

                bool maBycAktywny = !wylaczone.Contains(id);
                if (t.gameObject.activeSelf != maBycAktywny)
                    t.gameObject.SetActive(maBycAktywny);
            }
        }

        int komponenty = PrzywrocWlaczenieKomponentow(data.playerComponents, poSciezce);

        Debug.Log("LoadGame: drzewo postaci przywrocone (obiektow: " + poSciezce.Count +
                  ", komponentow: " + komponenty + ").");
    }

    // Wszystkie lampy w scenie. Konwencja: nazwa zawiera "Lamp".
    private static List<Transform> ZnajdzLampy()
    {
        List<Transform> lampy = new List<Transform>();

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name.IndexOf("Lamp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                lampy.Add(t);
        }

        return lampy;
    }

    private List<LampSaveData> ZbierzLampy()
    {
        List<LampSaveData> lista = new List<LampSaveData>();

        foreach (Transform t in ZnajdzLampy())
        {
            LampSaveData wpis = new LampSaveData();
            wpis.objectId = BuildObjectID(t.gameObject);
            wpis.objectName = t.name;
            wpis.parentId = t.parent != null ? BuildObjectID(t.parent.gameObject) : "";
            wpis.parentName = t.parent != null ? t.parent.name : "";
            wpis.localPosition = t.localPosition;
            wpis.localEuler = t.localEulerAngles;
            wpis.active = t.gameObject.activeSelf;
            lista.Add(wpis);
        }

        Debug.Log("SaveGame: lamp: " + lista.Count + ".");
        return lista;
    }

    // Wszystkie renderery w drzewie kazdego NPC.
    private static List<Renderer> ZnajdzRendereryNpc()
    {
        List<Renderer> renderery = new List<Renderer>();

        foreach (Transform npc in ZnajdzNpc())
        {
            if (npc == null)
                continue;

            renderery.AddRange(npc.GetComponentsInChildren<Renderer>(true));
        }

        return renderery;
    }

    private static string KluczRendereraNpc(Renderer renderer)
    {
        return BuildObjectID(renderer.gameObject) + "|" + renderer.GetType().Name + "|renderer";
    }

    // Wszystkie rozmowy przypisane do NPC w scenie.
    private static List<NPCConversation> ZnajdzRozmowy()
    {
        List<NPCConversation> rozmowy = new List<NPCConversation>();

        foreach (SmartNPC npc in FindObjectsByType<SmartNPC>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (npc == null)
                continue;

            if (npc.rozmowaDlaPostaciA != null && !rozmowy.Contains(npc.rozmowaDlaPostaciA))
                rozmowy.Add(npc.rozmowaDlaPostaciA);

            if (npc.rozmowaDlaPostaciB != null && !rozmowy.Contains(npc.rozmowaDlaPostaciB))
                rozmowy.Add(npc.rozmowaDlaPostaciB);
        }

        return rozmowy;
    }

    // Parametry bool rozmow decyduja, ktora galaz dialogu zobaczy gracz.
    // Sa stanem runtime, wiec bez zapisu po wczytaniu wracaja stare rozmowy.
    // Czy ten typ komponentu pomijamy przy zapisie flag.
    private static bool PomijamyKomponent(MonoBehaviour mb)
    {
        if (mb == null)
            return true;

        // Wlasnych managerow nie zapisujemy - ich flagi sa robocze
        // i nadpisanie ich psuje samo wczytywanie.
        if (mb is SaveLoadManager || mb is GameProgressManager || mb is InventoryManager)
            return true;

        // Komponenty interfejsu pomijamy calkowicie. Maja mnostwo pol
        // liczbowych (rozmiary, odstepy, przezroczystosci), a ich nadpisanie
        // psuje wyglad UI po wczytaniu.
        string przestrzen = mb.GetType().Namespace;

        if (!string.IsNullOrEmpty(przestrzen) &&
            (przestrzen.StartsWith("UnityEngine.UI") ||
             przestrzen.StartsWith("TMPro") ||
             przestrzen.StartsWith("UnityEngine.EventSystems")))
        {
            return true;
        }

        return mb.GetComponent<Canvas>() != null ||
               mb.GetComponent<UnityEngine.UI.Graphic>() != null;
    }

    // Dokleja flagi bool ze skryptow spoza drzewa LEVELS.
    private void DolaczFlagiSpozaPoziomow(List<ScriptFlagSaveData> flagi, List<Transform> drzewoPoziomow)
    {
        HashSet<Transform> wDrzewie = new HashSet<Transform>(drzewoPoziomow);
        int dodane = 0;

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PomijamyKomponent(mb) || wDrzewie.Contains(mb.transform))
                continue;

            System.Type typ = mb.GetType();

            foreach (FieldInfo pole in typ.GetFields(FlagiPol))
            {
                if (pole.FieldType != typeof(bool))
                    continue;

                ScriptFlagSaveData flaga = new ScriptFlagSaveData();
                flaga.key = KluczFlagi(mb.gameObject, typ, pole.Name);

                try
                {
                    flaga.value = (bool)pole.GetValue(mb);
                }
                catch (System.Exception)
                {
                    continue;
                }

                flagi.Add(flaga);
                dodane++;
            }
        }

        Debug.Log("SaveGame: flag ze skryptow spoza LEVELS: " + dodane + ".");
    }

    // Przywraca flagi skryptow spoza drzewa LEVELS.
    private int PrzywrocFlagiSpozaPoziomow(
        List<ScriptFlagSaveData> zapisane, Dictionary<string, Transform> wDrzewie)
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

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PomijamyKomponent(mb))
                continue;

            if (wDrzewie.ContainsKey(BuildObjectID(mb.gameObject)))
                continue;

            System.Type typ = mb.GetType();

            foreach (FieldInfo pole in typ.GetFields(FlagiPol))
            {
                if (pole.FieldType != typeof(bool))
                    continue;

                bool wartosc;
                if (!mapa.TryGetValue(KluczFlagi(mb.gameObject, typ, pole.Name), out wartosc))
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

        return licznik;
    }

    private static void PrzywrocZebraneLampyPiwnicy()
    {
        var usedLamps = new HashSet<GameObject>();
        foreach (var holder in FindObjectsByType<Int_lv3_WallLampHolder>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (holder.MountedLamp != null)
                usedLamps.Add(holder.MountedLamp);
        }
        if (Lvl3LampVisualManager.Instance != null)
        {
            usedLamps.Add(Lvl3LampVisualManager.Instance.GetCarriedLampForSave(false));
            usedLamps.Add(Lvl3LampVisualManager.Instance.GetCarriedLampForSave(true));
        }
        foreach (var pickup in FindObjectsByType<lvl3_int_Lamp>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var interaction = pickup.GetComponent<Interactable>();
            if ((pickup.HeldLamp != null && usedLamps.Contains(pickup.HeldLamp)) ||
                (interaction != null && interaction.IsCompleted))
                pickup.RestoreCollectedPickup();
        }
    }

    private static void PrzywrocWyjsciaZPiwnicy(List<string> unlockedIDs)
    {
        var unlocked = unlockedIDs != null ? new HashSet<string>(unlockedIDs) : null;
        foreach (var ladder in FindObjectsByType<Int_Lvl_3_Leadder>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            ladder.RestoreExitPermission(unlocked != null
                ? unlocked.Contains(ladder.ExitPermissionSaveId)
                : ladder.CanGo);

        // Legacy saves have no dedicated permission list. Completed Ethel exits
        // are enough to recover permission, without replaying her conversation.
        if (unlocked != null)
            return;
        foreach (var ethel in FindObjectsByType<Int_lv3_Ethel_2>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ethel.BasementExit != null && IsBlackboardInteractionCompleted(ethel))
                ethel.BasementExit.RestoreExitPermission(true);
        }
    }

    private static void PrzywrocDrzwiEthel()
    {
        foreach (var door in FindObjectsByType<lvl2_Int_DoorEthel>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            door.RestoreOpenedStateFromSave();
    }

    private static void PrzywrocUkonczoneStolyPiwnicy()
    {
        foreach (var table in FindObjectsByType<int_lv3_easyTable>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (table.IsCompleted)
                table.RestoreCompletedState();
        }
    }

    private void PrzywrocPulapkiManekina(List<string> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        HashSet<string> uruchomione = new HashSet<string>(zapisane);
        int licznik = 0;

        foreach (Int_lv3_Manequine pulapka in FindObjectsByType<Int_lv3_Manequine>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pulapka == null || !uruchomione.Contains(BuildObjectID(pulapka.gameObject)))
                continue;

            pulapka.RestoreTrapTriggeredState();
            licznik++;
        }

        Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count + " pulapek manekina.");
    }

    // Parametry int i float rozmow. Czesc galezi dialogow zalezy od licznikow.
    private List<ConversationNumberSaveData> ZbierzLiczboweParametryRozmow()
    {
        List<ConversationNumberSaveData> lista = new List<ConversationNumberSaveData>();

        foreach (NPCConversation rozmowa in ZnajdzRozmowy())
        {
            if (rozmowa.ParameterList == null)
                rozmowa.DeserializeForEditor();

            if (rozmowa.ParameterList == null)
                continue;

            foreach (EditableParameter parametr in rozmowa.ParameterList)
            {
                EditableIntParameter parametrInt = parametr as EditableIntParameter;

                if (parametrInt == null || string.IsNullOrWhiteSpace(parametrInt.ParameterName))
                    continue;

                ConversationNumberSaveData wpis = new ConversationNumberSaveData();
                wpis.key = rozmowa.name + "|" + parametrInt.ParameterName;
                wpis.isInt = true;
                wpis.intValue = parametrInt.IntValue;
                lista.Add(wpis);
            }
        }

        Debug.Log("SaveGame: liczbowych parametrow rozmow: " + lista.Count + ".");
        return lista;
    }

    private void PrzywrocLiczboweParametryRozmow(List<ConversationNumberSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, ConversationNumberSaveData> mapa =
            new Dictionary<string, ConversationNumberSaveData>();

        foreach (ConversationNumberSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.key))
                mapa[wpis.key] = wpis;
        }

        int licznik = 0;

        foreach (NPCConversation rozmowa in ZnajdzRozmowy())
        {
            if (rozmowa.ParameterList == null)
                rozmowa.DeserializeForEditor();

            if (rozmowa.ParameterList == null)
                continue;

            foreach (EditableParameter parametr in rozmowa.ParameterList)
            {
                EditableIntParameter parametrInt = parametr as EditableIntParameter;

                if (parametrInt == null || string.IsNullOrWhiteSpace(parametrInt.ParameterName))
                    continue;

                ConversationNumberSaveData wpis;
                if (!mapa.TryGetValue(rozmowa.name + "|" + parametrInt.ParameterName, out wpis))
                    continue;

                // UWAGA: Dialogue Editor w tej wersji nie udostepnia metody
                // ustawiajacej parametr int w czasie gry. Ustawiamy wartosc
                // bezposrednio na parametrze - dziala tak samo dla galezi,
                // ktore go sprawdzaja.
                parametrInt.IntValue = wpis.intValue;
                licznik++;
            }
        }

        Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count +
                  " liczbowych parametrow rozmow.");
    }

    // Animatory spoza drzewa LEVELS - drzwi i mechanizmy stojace poza grupami poziomow.
    // Czy obiekt wyglada na cos, co gracz moze przesunac.
    // ---------------------------------------------------------------
    // SLEDZTWO - poszlaki, wnioski, questy (ClueManager)
    // ---------------------------------------------------------------

    private static List<string> NazwyAssetow<T>(List<T> lista) where T : UnityEngine.Object
    {
        List<string> nazwy = new List<string>();

        if (lista == null)
            return nazwy;

        foreach (T asset in lista)
        {
            if (asset != null)
                nazwy.Add(asset.name);
        }

        return nazwy;
    }

    // ---------------------------------------------------------------
    // LICZNIKI W SKRYPTACH (int / float)
    // ---------------------------------------------------------------

    // Pola, ktorych NIE zapisujemy - sa robocze i zmieniaja sie co klatke.
    private static bool PomijamyPoleLicznika(string nazwa)
    {
        return nazwa.IndexOf("time", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("timer", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("velocity", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("elapsed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("alpha", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("speed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("duration", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private List<ScriptNumberSaveData> ZbierzLicznikiSkryptow()
    {
        List<ScriptNumberSaveData> lista = new List<ScriptNumberSaveData>();

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PomijamyKomponent(mb))
                continue;

            System.Type typ = mb.GetType();

            foreach (FieldInfo pole in typ.GetFields(FlagiPol))
            {
                bool jestInt = pole.FieldType == typeof(int);
                bool jestFloat = pole.FieldType == typeof(float);

                if (!jestInt && !jestFloat)
                    continue;

                if (PomijamyPoleLicznika(pole.Name))
                    continue;

                ScriptNumberSaveData wpis = new ScriptNumberSaveData();
                wpis.key = KluczFlagi(mb.gameObject, typ, pole.Name);
                wpis.isInt = jestInt;

                try
                {
                    if (jestInt)
                        wpis.intValue = (int)pole.GetValue(mb);
                    else
                        wpis.floatValue = (float)pole.GetValue(mb);
                }
                catch (System.Exception)
                {
                    continue;
                }

                lista.Add(wpis);
            }
        }

        Debug.Log("SaveGame: licznikow w skryptach: " + lista.Count + ".");
        return lista;
    }

    private void PrzywrocLicznikiSkryptow(List<ScriptNumberSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, ScriptNumberSaveData> mapa = new Dictionary<string, ScriptNumberSaveData>();
        foreach (ScriptNumberSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.key))
                mapa[wpis.key] = wpis;
        }

        int licznik = 0;

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PomijamyKomponent(mb))
                continue;

            System.Type typ = mb.GetType();

            foreach (FieldInfo pole in typ.GetFields(FlagiPol))
            {
                bool jestInt = pole.FieldType == typeof(int);
                bool jestFloat = pole.FieldType == typeof(float);

                if (!jestInt && !jestFloat)
                    continue;

                ScriptNumberSaveData wpis;
                if (!mapa.TryGetValue(KluczFlagi(mb.gameObject, typ, pole.Name), out wpis))
                    continue;

                try
                {
                    if (jestInt && wpis.isInt)
                        pole.SetValue(mb, wpis.intValue);
                    else if (jestFloat && !wpis.isInt)
                        pole.SetValue(mb, wpis.floatValue);
                    else
                        continue;

                    licznik++;
                }
                catch (System.Exception)
                {
                }
            }
        }

        Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count + " licznikow w skryptach.");
    }

    private void ZbierzSledztwo(GameData data)
    {
        if (ClueManager.Instance == null)
        {
            Debug.LogWarning("SaveGame: brak ClueManager.Instance - sledztwo NIE zostanie zapisane.");
            return;
        }

        ClueManager cm = ClueManager.Instance;

        data.collectedClues = NazwyAssetow(cm.collectedClues);
        data.collectedConclusions = NazwyAssetow(cm.collectedConclusions);
        data.collectedQuestConclusions = NazwyAssetow(cm.collectedQuestConclusions);
        data.completedQuests = NazwyAssetow(cm.completedQuests);
        data.activeQuests = NazwyAssetow(cm.activeQuests);

        Debug.Log("SaveGame: sledztwo - poszlak: " + data.collectedClues.Count +
                  ", wnioskow: " + data.collectedConclusions.Count +
                  ", wnioskow questowych: " + data.collectedQuestConclusions.Count +
                  ", questow aktywnych: " + data.activeQuests.Count +
                  ", ukonczonych: " + data.completedQuests.Count + ".");
    }

    // Poszlaki nie maja wspolnej listy w ClueManager - zbieramy je z pol
    // 'clues' wszystkich interakcji w scenie.
    private static Dictionary<string, Clues_SO> ZbierzDostepnePoszlaki()
    {
        Dictionary<string, Clues_SO> baza = new Dictionary<string, Clues_SO>();

        foreach (Interactable interakcja in FindObjectsByType<Interactable>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (interakcja == null || interakcja.clues == null)
                continue;

            foreach (Clues_SO poszlaka in interakcja.clues)
            {
                if (poszlaka != null)
                    baza[poszlaka.name] = poszlaka;
            }
        }

        return baza;
    }

    private static void UzupelnijListe<T>(List<T> cel, List<string> nazwy,
                                          Dictionary<string, T> baza, ref int licznik) where T : UnityEngine.Object
    {
        if (cel == null || nazwy == null)
            return;

        foreach (string nazwa in nazwy)
        {
            T asset;
            if (!baza.TryGetValue(nazwa, out asset) || asset == null)
                continue;

            if (cel.Contains(asset))
                continue;

            cel.Add(asset);
            licznik++;
        }
    }

    private static Dictionary<string, T> BazaAssetow<T>(List<T> lista) where T : UnityEngine.Object
    {
        Dictionary<string, T> baza = new Dictionary<string, T>();

        if (lista == null)
            return baza;

        foreach (T asset in lista)
        {
            if (asset != null)
                baza[asset.name] = asset;
        }

        return baza;
    }

    private void PrzywrocSledztwo(GameData data)
    {
        if (ClueManager.Instance == null)
        {
            Debug.LogWarning("LoadGame: brak ClueManager.Instance - sledztwo nie zostanie przywrocone.");
            return;
        }

        ClueManager cm = ClueManager.Instance;
        int licznik = 0;

        UzupelnijListe(cm.collectedClues, data.collectedClues, ZbierzDostepnePoszlaki(), ref licznik);
        UzupelnijListe(cm.collectedConclusions, data.collectedConclusions,
                       BazaAssetow(cm.allConclusions), ref licznik);
        UzupelnijListe(cm.collectedQuestConclusions, data.collectedQuestConclusions,
                       BazaAssetow(cm.allQuestConclusions), ref licznik);

        Dictionary<string, Quest_SO> bazaQuestow = BazaAssetow(cm.allQuests);
        UzupelnijListe(cm.completedQuests, data.completedQuests, bazaQuestow, ref licznik);
        UzupelnijListe(cm.activeQuests, data.activeQuests, bazaQuestow, ref licznik);

        CluesLog.Instance?.UpdateLog();

        Debug.Log("LoadGame: przywrocono " + licznik + " wpisow sledztwa (poszlaki, wnioski, questy).");
    }

    private static bool CzyRuchomyObiekt(Transform t)
    {
        if (t == null)
            return false;

        if (t.GetComponent<Rigidbody>() != null)
            return true;

        string nazwa = t.name;

        return nazwa.IndexOf("Movable", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("Box", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               nazwa.IndexOf("Skrzyn", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private List<TransformSaveData> ZbierzRuchomeObiekty()
    {
        List<TransformSaveData> lista = new List<TransformSaveData>();

        Transform korzen = ZnajdzKorzenPoziomow();
        HashSet<Transform> wDrzewie = new HashSet<Transform>();

        if (korzen != null)
        {
            List<Transform> drzewo = new List<Transform>();
            ZbierzDrzewo(korzen, drzewo);

            foreach (Transform t in drzewo)
                wDrzewie.Add(t);
        }

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || wDrzewie.Contains(t) || !CzyRuchomyObiekt(t))
                continue;

            TransformSaveData wpis = new TransformSaveData();
            wpis.objectId = BuildObjectID(t.gameObject);
            wpis.stableId = BuildStableTransformID(t);
            wpis.parentStableId = BuildStableTransformID(t.parent);
            wpis.objectName = t.name;
            wpis.parentId = t.parent != null ? BuildObjectID(t.parent.gameObject) : "";
            wpis.localPosition = t.localPosition;
            wpis.localEuler = t.localEulerAngles;
            lista.Add(wpis);
        }

        Debug.Log("SaveGame: ruchomych obiektow spoza LEVELS: " + lista.Count + ".");
        return lista;
    }

    private void PrzywrocRuchomeObiekty(List<TransformSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, Transform> poSciezce = new Dictionary<string, Transform>();
        List<Transform> wszystkie = new List<Transform>();

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
                continue;

            poSciezce[BuildObjectID(t.gameObject)] = t;
            wszystkie.Add(t);
        }

        Dictionary<string, Transform> poNazwie = ZbudujMapePoNazwie(wszystkie);
        var stableTransforms = BuildStableTransformMap(wszystkie);
        int licznik = 0;

        foreach (TransformSaveData dane in zapisane)
        {
            if (dane == null)
                continue;

            Transform t = FindSavedTransform(dane, poSciezce, poNazwie, stableTransforms);
            if (t == null)
                continue;

            // Rigidbody trzeba uspic, inaczej fizyka przeciagnie obiekt z powrotem.
            Rigidbody cialo = t.GetComponent<Rigidbody>();
            if (cialo != null && !cialo.isKinematic)
            {
                cialo.linearVelocity = Vector3.zero;
                cialo.angularVelocity = Vector3.zero;
            }

            Transform parent;
            if ((!string.IsNullOrEmpty(dane.parentStableId) &&
                 stableTransforms.TryGetValue(dane.parentStableId, out parent)) ||
                (!string.IsNullOrEmpty(dane.parentId) && poSciezce.TryGetValue(dane.parentId, out parent)))
                t.SetParent(parent, false);
            t.localPosition = dane.localPosition;
            t.localEulerAngles = dane.localEuler;
            licznik++;
        }

        Debug.Log("LoadGame: przywrocono pozycje " + licznik + " z " + zapisane.Count +
                  " ruchomych obiektow.");
    }

    private List<AnimatorSaveData> ZbierzAnimatorySceny()
    {
        List<AnimatorSaveData> lista = new List<AnimatorSaveData>();

        Transform korzen = ZnajdzKorzenPoziomow();
        HashSet<Transform> wDrzewie = new HashSet<Transform>();

        if (korzen != null)
        {
            List<Transform> drzewo = new List<Transform>();
            ZbierzDrzewo(korzen, drzewo);

            foreach (Transform t in drzewo)
                wDrzewie.Add(t);
        }

        foreach (Animator animator in FindObjectsByType<Animator>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;

            if (wDrzewie.Contains(animator.transform))
                continue;

            lista.Add(ZbierzAnimator(animator.gameObject, animator));
        }

        Debug.Log("SaveGame: animatorow spoza LEVELS: " + lista.Count + ".");
        return lista;
    }

    private void PrzywrocAnimatorySceny(List<AnimatorSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, Transform> poSciezce = new Dictionary<string, Transform>();

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null)
                poSciezce[BuildObjectID(t.gameObject)] = t;
        }

        int licznik = PrzywrocAnimatory(zapisane, poSciezce);

        Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count +
                  " animatorow spoza LEVELS.");
    }

    private List<ScriptFlagSaveData> ZbierzParametryRozmow()
    {
        List<ScriptFlagSaveData> lista = new List<ScriptFlagSaveData>();

        foreach (NPCConversation rozmowa in ZnajdzRozmowy())
        {
            if (rozmowa.ParameterList == null)
                rozmowa.DeserializeForEditor();

            if (rozmowa.ParameterList == null)
                continue;

            foreach (EditableParameter parametr in rozmowa.ParameterList)
            {
                EditableBoolParameter parametrBool = parametr as EditableBoolParameter;
                if (parametrBool == null || string.IsNullOrWhiteSpace(parametrBool.ParameterName))
                    continue;

                ScriptFlagSaveData wpis = new ScriptFlagSaveData();
                wpis.key = rozmowa.name + "|" + parametrBool.ParameterName;
                wpis.value = parametrBool.BoolValue;
                lista.Add(wpis);
            }
        }

        Debug.Log("SaveGame: parametrow rozmow: " + lista.Count + ".");
        return lista;
    }

    private void PrzywrocParametryRozmow(List<ScriptFlagSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, bool> mapa = new Dictionary<string, bool>();
        foreach (ScriptFlagSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.key))
                mapa[wpis.key] = wpis.value;
        }

        int licznik = 0;

        foreach (NPCConversation rozmowa in ZnajdzRozmowy())
        {
            if (rozmowa.ParameterList == null)
                rozmowa.DeserializeForEditor();

            if (rozmowa.ParameterList == null)
                continue;

            foreach (EditableParameter parametr in rozmowa.ParameterList)
            {
                EditableBoolParameter parametrBool = parametr as EditableBoolParameter;
                if (parametrBool == null || string.IsNullOrWhiteSpace(parametrBool.ParameterName))
                    continue;

                bool wartosc;
                if (!mapa.TryGetValue(rozmowa.name + "|" + parametrBool.ParameterName, out wartosc))
                    continue;

                rozmowa.SetRuntimeBoolParameter(parametrBool.ParameterName, wartosc);
                licznik++;
            }
        }

        Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count + " parametrow rozmow.");
    }

    private List<ScriptFlagSaveData> ZbierzRendereryNpc()
    {
        List<ScriptFlagSaveData> lista = new List<ScriptFlagSaveData>();

        foreach (Renderer renderer in ZnajdzRendereryNpc())
        {
            if (renderer == null)
                continue;

            ScriptFlagSaveData wpis = new ScriptFlagSaveData();
            wpis.key = KluczRendereraNpc(renderer);
            wpis.value = renderer.enabled;
            lista.Add(wpis);
        }

        int wylaczone = 0;
        foreach (ScriptFlagSaveData wpis in lista)
        {
            if (!wpis.value)
                wylaczone++;
        }

        Debug.Log("SaveGame: rendererow NPC: " + lista.Count + ", wylaczonych: " + wylaczone + ".");
        return lista;
    }

    private void PrzywrocRendereryNpc(List<ScriptFlagSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, bool> mapa = new Dictionary<string, bool>();
        foreach (ScriptFlagSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.key))
                mapa[wpis.key] = wpis.value;
        }

        int licznik = 0;

        foreach (Renderer renderer in ZnajdzRendereryNpc())
        {
            if (renderer == null)
                continue;

            bool wlaczony;
            if (!mapa.TryGetValue(KluczRendereraNpc(renderer), out wlaczony))
                continue;

            if (renderer.enabled != wlaczony)
            {
                renderer.enabled = wlaczony;
                licznik++;
            }
        }

        if (!cichePowtorzenie)
            Debug.Log("LoadGame: poprawiono " + licznik + " rendererow NPC.");
    }

    private List<WallLampSaveData> ZbierzUchwytyLamp()
    {
        List<WallLampSaveData> lista = new List<WallLampSaveData>();

        foreach (Int_lv3_WallLampHolder uchwyt in FindObjectsByType<Int_lv3_WallLampHolder>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (uchwyt == null)
                continue;

            WallLampSaveData wpis = new WallLampSaveData();
            wpis.holderId = BuildObjectID(uchwyt.gameObject);
            wpis.mountedLampIndex = uchwyt.MountedLampIndex;
            lista.Add(wpis);
        }

        int zLampa = 0;
        foreach (WallLampSaveData wpis in lista)
        {
            if (wpis.mountedLampIndex > 0)
                zLampa++;
        }

        Debug.Log("SaveGame: sciennych uchwytow lamp: " + lista.Count +
                  ", z zawieszona lampa: " + zLampa + ".");
        return lista;
    }

    private void PrzywrocUchwytyLamp(List<WallLampSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        Dictionary<string, int> poId = new Dictionary<string, int>();
        foreach (WallLampSaveData wpis in zapisane)
        {
            if (wpis != null && !string.IsNullOrEmpty(wpis.holderId))
                poId[wpis.holderId] = wpis.mountedLampIndex;
        }

        int zawieszone = 0;

        foreach (Int_lv3_WallLampHolder uchwyt in FindObjectsByType<Int_lv3_WallLampHolder>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (uchwyt == null)
                continue;

            int index;
            if (!poId.TryGetValue(BuildObjectID(uchwyt.gameObject), out index))
                continue;

            if (index > 0)
            {
                uchwyt.RestoreMountedLamp(index);
                zawieszone++;
            }
            else
            {
                uchwyt.RefreshMountedLampLightFromSave();
            }
        }

        if (!cichePowtorzenie)
            Debug.Log("LoadGame: przywrocono " + zawieszone + " zawieszonych lamp.");
    }

    private void PrzywrocLampy(List<LampSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        // Lampy szukamy w calej scenie - zawieszenie na uchwycie przepina je
        // pod inny obiekt, wiec zapisana sciezka moze juz nie pasowac.
        Dictionary<string, Transform> poSciezce = new Dictionary<string, Transform>();
        List<Transform> wszystkie = new List<Transform>();

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
                continue;

            poSciezce[BuildObjectID(t.gameObject)] = t;
            wszystkie.Add(t);
        }

        Dictionary<string, Transform> poNazwie = ZbudujMapePoNazwie(wszystkie);
        int licznik = 0;

        foreach (LampSaveData wpis in zapisane)
        {
            if (wpis == null)
                continue;

            Transform lampa = ZnajdzWDrzewie(poSciezce, poNazwie, wpis.objectId, wpis.objectName);
            if (lampa == null)
                continue;

            Transform rodzic = ZnajdzWDrzewie(poSciezce, poNazwie, wpis.parentId, wpis.parentName);

            if (rodzic != null && lampa.parent != rodzic)
                lampa.SetParent(rodzic, false);

            lampa.localPosition = wpis.localPosition;
            lampa.localEulerAngles = wpis.localEuler;
            lampa.gameObject.SetActive(wpis.active);
            licznik++;
        }

        if (!cichePowtorzenie)
            Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count + " lamp.");
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

    // Buduje mape nazwa -> Transform, ale TYLKO dla nazw unikalnych w drzewie.
    // Sluzy jako zapas, gdy obiekt zostal przepiety i jego sciezka sie zmienila.
    private static Dictionary<string, Transform> ZbudujMapePoNazwie(IEnumerable<Transform> drzewo)
    {
        Dictionary<string, Transform> poNazwie = new Dictionary<string, Transform>();
        HashSet<string> duplikaty = new HashSet<string>();

        foreach (Transform t in drzewo)
        {
            if (t == null)
                continue;

            if (poNazwie.ContainsKey(t.name))
            {
                duplikaty.Add(t.name);
                continue;
            }

            poNazwie[t.name] = t;
        }

        foreach (string nazwa in duplikaty)
            poNazwie.Remove(nazwa);

        return poNazwie;
    }

    private static Transform ZnajdzWDrzewie(
        Dictionary<string, Transform> poSciezce,
        Dictionary<string, Transform> poNazwie,
        string objectId, string objectName)
    {
        Transform t;

        if (!string.IsNullOrEmpty(objectId) && poSciezce.TryGetValue(objectId, out t) && t != null)
            return t;

        if (!string.IsNullOrEmpty(objectName) && poNazwie.TryGetValue(objectName, out t) && t != null)
            return t;

        return null;
    }

    // Czarne plyty (material BLACK DOCS) znikaja przez wygaszenie alfy materialu.
    // Obiekty, ktore interakcje wykorzystuja jako 'Interactive Shader'
    // (np. wyglad kukly). Maja czarny material, ale NIE sa plytami
    // zakrywajacymi pokoj - ich gaszenie psuje wyglad obiektow.
    private static HashSet<Transform> ZnajdzObiektyShaderowInterakcji()
    {
        HashSet<Transform> wykluczone = new HashSet<Transform>();

        foreach (Interactable interakcja in FindObjectsByType<Interactable>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (interakcja != null && interakcja.interactiveShader != null)
                wykluczone.Add(interakcja.interactiveShader.transform);
        }

        return wykluczone;
    }

    // Czy nazwa obiektu wskazuje na czarna plyte zakrywajaca pomieszczenie.
    private static bool CzyNazwaPlyty(string nazwa)
    {
        if (string.IsNullOrEmpty(nazwa))
            return false;

        if (nazwa.EndsWith("bb", System.StringComparison.OrdinalIgnoreCase))
            return true;

        return nazwa.IndexOf("BlackBoard", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static Dictionary<GameObject, bool> GetBlackboardDiscoveryStates()
    {
        var states = new Dictionary<GameObject, bool>();
        foreach (var door in FindObjectsByType<lvl2_Int_DoorEthel>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door.UsesSaveStateRestoration)
                AddBlackboardDiscoveryState(states, door.Blackboard, door.IsOpen);
        }
        foreach (var trigger in FindObjectsByType<Int_lv3_RemoveBlackboard>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddBlackboardDiscoveryState(states, trigger.BlackBoard, trigger.HasTriggered);
        foreach (var door in FindObjectsByType<Int_lv3_SecretWallDoor>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddBlackboardDiscoveryState(states, door.Blackboard,
                door.IsOpened || IsBlackboardInteractionCompleted(door));
        foreach (var door in FindObjectsByType<Int_lv3_ClockSecretPassage>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddBlackboardDiscoveryState(states, door.Blackboard,
                door.IsOpened || IsBlackboardInteractionCompleted(door));
        foreach (var interaction in FindObjectsByType<Int_lv3_Ethel>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddBlackboardDiscoveryState(states, interaction.Blackboard,
                IsBlackboardInteractionCompleted(interaction));
        foreach (var interaction in FindObjectsByType<Int_lv3_Ethel_2>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddBlackboardDiscoveryState(states, interaction.Blackboard,
                IsBlackboardInteractionCompleted(interaction));
        // Trap boards can be covered again and later uncovered by Ethel.
        // Their actual saved visibility takes precedence over discovery history.
        foreach (var trap in FindObjectsByType<Int_lv3_Manequine>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (trap.TrapBlackboard != null)
                states.Remove(trap.TrapBlackboard);
        }
        return states;
    }

    private static bool IsBlackboardInteractionCompleted(Component owner)
    {
        Interactable interaction = owner.GetComponent<Interactable>();
        return interaction != null && interaction.IsCompleted;
    }

    private static void AddBlackboardDiscoveryState(
        Dictionary<GameObject, bool> states, GameObject board, bool revealed)
    {
        if (board == null)
            return;
        bool previous;
        states[board] = revealed || (states.TryGetValue(board, out previous) && previous);
    }

    private static List<BlackboardSaveData> ZbierzCzarnePlyty(List<Transform> drzewo)
    {
        List<BlackboardSaveData> lista = new List<BlackboardSaveData>();
        HashSet<Transform> wykluczone = ZnajdzObiektyShaderowInterakcji();
        var discoveryStates = GetBlackboardDiscoveryStates();

        foreach (Transform t in drzewo)
        {
            if (t == null)
                continue;

            // Diagnostyka: obiekt wyglada na czarna plyte po nazwie, ale zostal
            // odrzucony przez jeden z warunkow ponizej. Log mowi przez ktory.
            bool nazwaPlyty = CzyNazwaPlyty(t.name);

            Renderer renderer = t.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                if (nazwaPlyty)
                    Debug.LogWarning("SaveGame: '" + t.name + "' ma nazwe plyty, ale nie ma renderera - pomijam.");
                continue;
            }

            // Obiekty z wieloma materialami (sciany, filary, drzwi) maja czarna
            // warstwe TYLKO jako dodatek - nie sa plytami zakrywajacymi pokoj.
            // Gaszenie ich robilo dziury w geometrii.
            if (renderer.sharedMaterials.Length != 1)
            {
                if (nazwaPlyty)
                    Debug.LogWarning("SaveGame: '" + t.name + "' ma nazwe plyty, ale " +
                                     renderer.sharedMaterials.Length + " materialow - pomijam.");
                continue;
            }

            // Obiekt uzywany przez interakcje jako jej wyglad - nie gasimy.
            if (wykluczone.Contains(t))
            {
                if (nazwaPlyty)
                    Debug.LogWarning("SaveGame: '" + t.name + "' ma nazwe plyty, ale jest " +
                                     "przypisany jako Interactive Shader - pomijam.");
                continue;
            }

            // Obiekt bedacy czescia interakcji (np. wyglad kukly) - nie gasimy.
            if (t.GetComponentInParent<Interactable>() != null)
            {
                if (nazwaPlyty)
                    Debug.LogWarning("SaveGame: '" + t.name + "' ma nazwe plyty, ale lezy pod " +
                                     "obiektem z Interactable - pomijam.");
                continue;
            }

            // Za plyte uznajemy tylko obiekt nazwany zgodnie z konwencja sceny:
            // "lv3_R1bb" ... "lv3_R6bb", "BasementBlackBoard", "Level_3_BlackBoards".
            // Samo wykrywanie po materiale bylo za szerokie - lapalo sciany,
            // filary i elementy wygladu innych obiektow.
            if (!nazwaPlyty)
                continue;

            if (renderer.sharedMaterial.name.IndexOf("BLACK", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (nazwaPlyty)
                    Debug.LogWarning("SaveGame: '" + t.name + "' ma nazwe plyty, ale material to '" +
                                     renderer.sharedMaterial.name + "' (bez 'BLACK') - pomijam.");
                continue;
            }

            BlackboardSaveData wpis = new BlackboardSaveData();
            wpis.objectId = BuildObjectID(t.gameObject);
            wpis.objectName = t.name;
            wpis.active = t.gameObject.activeSelf;

            // Odslanianie pomieszczenia polega na wylaczeniu Mesh Renderera
            // plyty. Renderery zapisujemy WYLACZNIE dla czarnych plyt -
            // dla reszty sceny nimi zarzadza DOCS i nie wolno ich utrwalac.
            wpis.rendererEnabled = renderer.enabled;
            bool revealed;
            if (discoveryStates.TryGetValue(t.gameObject, out revealed))
            {
                // Inactive levels have not necessarily been discovered.
                wpis.active = !revealed;
                wpis.rendererEnabled = !revealed;
            }

            // UWAGA: czytamy sharedMaterial, nigdy renderer.material.
            // Odwolanie do .material tworzy prywatna kopie materialu dla tego
            // obiektu, przez co system DOCS przestaje na nim dzialac - wycina
            // dziure w oryginale, a obiekt rysuje swoja kopie (pelna czern).
            wpis.alpha = renderer.sharedMaterial.HasProperty("_BaseColor")
                ? renderer.sharedMaterial.GetColor("_BaseColor").a
                : renderer.sharedMaterial.color.a;

            lista.Add(wpis);
        }

        Debug.Log("SaveGame: czarnych plyt: " + lista.Count + ".");
        return lista;
    }

    private System.Collections.IEnumerator PonawiajPlyty(
        List<BlackboardSaveData> plyty, List<string> wyzwalacze,
        List<NpcSaveData> npc, List<WallLampSaveData> uchwyty,
        List<ScriptFlagSaveData> renderery)
    {
        for (int i = 0; i < powtorzeniaPlyt; i++)
        {
            yield return new WaitForSeconds(odstepPowtorzenPlyt);

            cichePowtorzenie = true;
            PrzywrocCzarnePlyty(plyty);
            PrzywrocUsunietePlyty(wyzwalacze);
            PrzywrocNpc(npc);
            PrzywrocUchwytyLamp(uchwyty);
            PrzywrocRendereryNpc(renderery);
            cichePowtorzenie = false;

            Sledz("powtorzenie plyt " + (i + 1));
        }

        Debug.Log("LoadGame: zakonczono ponawianie stanu czarnych plyt (" +
                  powtorzeniaPlyt + " powtorzen).");
    }

    private void PrzywrocUsunietePlyty(List<string> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        HashSet<string> usuniete = new HashSet<string>(zapisane);
        int licznik = 0;

        foreach (Int_lv3_RemoveBlackboard wyzwalacz in FindObjectsByType<Int_lv3_RemoveBlackboard>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (wyzwalacz == null || !usuniete.Contains(BuildObjectID(wyzwalacz.gameObject)))
                continue;

            wyzwalacz.ApplyTriggeredStateImmediately();
            licznik++;
        }

        if (!cichePowtorzenie)
            Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count + " usunietych plyt.");
    }

    private void PrzywrocCzarnePlyty(List<BlackboardSaveData> zapisane)
    {
        if (zapisane == null || zapisane.Count == 0)
            return;

        // Czarne plyty szukamy w CALEJ scenie, nie tylko w drzewie poziomow -
        // czesc z nich jest przepinana razem z pomieszczeniami i ich sciezka
        // przestaje pasowac do zapisanej.
        Dictionary<string, Transform> wSzystkie = new Dictionary<string, Transform>();
        List<Transform> lista = new List<Transform>();

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
                continue;

            wSzystkie[BuildObjectID(t.gameObject)] = t;
            lista.Add(t);
        }

        Dictionary<string, Transform> poNazwie = ZbudujMapePoNazwie(lista);
        var discoveryStates = GetBlackboardDiscoveryStates();
        int licznik = 0;
        List<string> nieznalezione = new List<string>();

        foreach (BlackboardSaveData wpis in zapisane)
        {
            if (wpis == null)
                continue;

            Transform t = ZnajdzWDrzewie(wSzystkie, poNazwie, wpis.objectId, wpis.objectName);
            if (t == null)
            {
                nieznalezione.Add(wpis.objectName + " (" + wpis.objectId + ")");
                continue;
            }

            Renderer renderer = t.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            // Also repairs legacy saves made before a level's Start activation.
            bool revealed;
            if (discoveryStates.TryGetValue(t.gameObject, out revealed))
            {
                renderer.enabled = !revealed;
                t.gameObject.SetActive(!revealed);
                licznik++;
                continue;
            }
            // DOCS material alpha is not a discovery flag.

            // Nie dotykamy materialu przy przywracaniu - patrz uwaga wyzej.
            t.gameObject.SetActive(wpis.active);
            renderer.enabled = wpis.rendererEnabled;
            licznik++;
        }

        if (!cichePowtorzenie)
            Debug.Log("LoadGame: przywrocono " + licznik + " z " + zapisane.Count + " czarnych plyt.");

        if (nieznalezione.Count > 0)
        {
            Debug.LogWarning("LoadGame: nie znaleziono czarnych plyt: " +
                             string.Join(" | ", nieznalezione));
        }
    }

    private static string BuildStableTransformID(Transform transform)
    {
        if (transform == null)
            return null;
        string suffix = "";
        for (Transform current = transform; current != null; current = current.parent)
        {
            var interaction = current.GetComponent<Interactable>();
            if (interaction != null && !string.IsNullOrEmpty(interaction.SaveId))
                return "interaction:" + interaction.SaveId + suffix;
            suffix = "/" + SegmentSciezki(current) + suffix;
        }
        return null;
    }

    private static Dictionary<string, Transform> BuildStableTransformMap(IEnumerable<Transform> transforms)
    {
        var map = new Dictionary<string, Transform>();
        var duplicates = new HashSet<string>();
        foreach (Transform transform in transforms)
        {
            string id = BuildStableTransformID(transform);
            if (string.IsNullOrEmpty(id) || duplicates.Contains(id))
                continue;
            if (map.ContainsKey(id))
            {
                map.Remove(id);
                duplicates.Add(id);
                Debug.LogWarning("LoadGame: duplicate stable transform ID: " + id);
            }
            else
                map.Add(id, transform);
        }
        return map;
    }

    private static Transform FindSavedTransform(TransformSaveData data,
        Dictionary<string, Transform> paths, Dictionary<string, Transform> names,
        Dictionary<string, Transform> stableTransforms)
    {
        Transform transform;
        if (!string.IsNullOrEmpty(data.stableId))
            return stableTransforms.TryGetValue(data.stableId, out transform) ? transform : null;
        return ZnajdzWDrzewie(paths, names, data.objectId, data.objectName);
    }

    private static void PrzywrocRodzicow(
        List<TransformSaveData> zapisane, Dictionary<string, Transform> poSciezce)
    {
        if (zapisane == null)
            return;

        int przepiete = 0;

        Dictionary<string, Transform> poNazwie = ZbudujMapePoNazwie(poSciezce.Values);
        var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var stableTransforms = BuildStableTransformMap(allTransforms);
        var parentPaths = new Dictionary<string, Transform>();
        foreach (var transform in allTransforms)
            parentPaths[BuildObjectID(transform.gameObject)] = transform;

        foreach (TransformSaveData dane in zapisane)
        {
            if (dane == null || string.IsNullOrEmpty(dane.parentId))
                continue;

            Transform obiekt = FindSavedTransform(dane, poSciezce, poNazwie, stableTransforms);
            if (obiekt == null)
                continue;

            Transform rodzic = null;
            bool foundParent = !string.IsNullOrEmpty(dane.parentStableId) &&
                               stableTransforms.TryGetValue(dane.parentStableId, out rodzic);
            if (!foundParent)
                foundParent = parentPaths.TryGetValue(dane.parentId, out rodzic);
            if (!foundParent || rodzic == null)
                continue;

            if (obiekt.parent == rodzic)
                continue;

            obiekt.SetParent(rodzic, false);
            przepiete++;
        }

        if (przepiete > 0)
            Debug.Log("LoadGame: przepieto " + przepiete + " obiektow pod zapisanych rodzicow.");
    }

    // Wypisuje stan sledzonego obiektu na kolejnych etapach wczytywania.
    // Dzieki temu widac, ktory etap go wlacza albo wylacza.
    private void Sledz(string etap)
    {
        if (string.IsNullOrWhiteSpace(sledzonyObiekt))
            return;

        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.name != sledzonyObiekt)
                continue;

            Renderer r = t.GetComponent<Renderer>();

            Debug.Log("[SLEDZENIE] " + etap + ": '" + t.name +
                      "' aktywny: " + t.gameObject.activeSelf +
                      ", renderer: " + (r != null ? r.enabled.ToString() : "brak") +
                      ", material: " + (r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "brak"));
            return;
        }

        Debug.LogWarning("[SLEDZENIE] " + etap + ": nie znaleziono obiektu '" + sledzonyObiekt + "'.");
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

        Dictionary<string, Transform> poNazwie = ZbudujMapePoNazwie(poSciezce.Values);
        var stableTransforms = BuildStableTransformMap(poSciezce.Values);

        foreach (TransformSaveData dane in zapisane)
        {
            if (dane == null)
                continue;

            Transform t = FindSavedTransform(dane, poSciezce, poNazwie, stableTransforms);
            if (t == null)
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

        if (!cichePowtorzenie)
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
