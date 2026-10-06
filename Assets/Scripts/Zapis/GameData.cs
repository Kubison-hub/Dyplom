using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // --- Wersja formatu zapisu ---
    // Podnies SaveLoadManager.WERSJA_ZAPISU po kazdej zmianie struktury tej klasy.
    // Pliki z inna wersja sa ignorowane zamiast psuc wczytywanie.
    public int saveVersion;

    // --- Pozycje Graczy ---
    public Vector3 sherlockPosition;
    public Vector3 watsonPosition;

    // --- Ekwipunek ---
    public List<ItemType> itemsInInventory;

    // --- Przedmioty podniesione ze swiata ---
    // Lista ID obiektow, ktore gracz juz podniosl.
    // Po wczytaniu zapisu te obiekty sa usuwane ze sceny.
    public List<string> pickedUpItemIDs;

    // --- Ukonczone interakcje ---
    // Lista SaveId z komponentow Interactable (zagadki, drzwi, dialogi).
    // Obsluguje je GameProgressManager.
    public List<string> completedInteractionIDs;

    // --- Panel zadan (lista celow w lewym gornym rogu) ---
    public CluesLogSaveData questLog;

    // --- Etap samouczka / sekwencji startowej (TutorialTimeline) ---
    // Nazwa wartosci z enuma TutorialStage, np. "Completed".
    public string tutorialStage;

    // --- Odkryte punkty sledztwa (DetectiveIdeaPoint) ---
    // Lista ideaId. To z nich CluesLog przelicza licznik "Zbadaj pomieszczenie".
    public List<string> discoveredIdeaPointIDs;

    // --- Badanie ciala Lady Edith (Int_EdithExamBody) ---
    public EdithExamSaveData edithExam;

    // --- Pozycje NPC (obiekty z nazwa konczaca sie na "_NPC") ---
    public List<NpcSaveData> npcs;

    // --- Aktywna postac (SwitchCharacter.activePlayerIndex) ---
    // 0 = Sherlock, 1 = Watson. Decyduje tez o tym, ktora kamera ma priorytet.
    public int activePlayerIndex;

    // --- Aktywne grupy poziomow (LEVEL_1, LEVEL_2, ...) ---
    // Poziomy sa w jednej scenie jako grupy obiektow. Przejscie na kolejny
    // poziom wlacza jedna grupe i wylacza poprzednia - bez tego zapis
    // wczytywalby sie zawsze na pierwszym poziomie.
    public List<LevelGroupSaveData> levelGroups;

    // Stan zagadek w drzewie poziomow: flagi skryptow, pozycje obiektow
    // i stany animatorow. Bez tego otwarte drzwi wracaja zamkniete,
    // a przesuniete mechanizmy do pozycji wyjsciowej.
    public List<ScriptFlagSaveData> levelFlags;
    public List<TransformSaveData> levelTransforms;
    public List<AnimatorSaveData> levelAnimators;

    // Rozwiazane zagadki sekwencyjne (DetectiveSequencePuzzle).
    // IsSolved jest wlasciwoscia, wiec zapisujemy je osobno, a nie przez flagi.
    public List<string> solvedSequencePuzzles;

    // --- Lampy niesione przez postacie w piwnicy (Lvl3LampVisualManager) ---
    // Bez tego po wczytaniu piwnica jest calkowicie ciemna, bo swiatlo
    // niesionej lampy gasnie w Start() managera.
    public string sherlockCarriedLampId;
    public string watsonCarriedLampId;

    // --- Stan obiektow pod Sherlockiem i Watsonem ---
    // Swiatla i wizualizacje trzymanych lamp wisza pod postaciami, czyli POZA
    // drzewem LEVELS. Zapisujemy wylaczone obiekty i stan ich komponentow.
    public List<LevelGroupSaveData> playerSubtree;
    public List<ScriptFlagSaveData> playerComponents;

    // --- Przezroczystosc czarnych plyt (BLACK DOCS) ---
    // Plyty zakrywajace pomieszczenia znikaja przez wygaszenie alfy materialu,
    // a material to stan runtime - po wczytaniu wracalby do pelnej czerni.
    public List<BlackboardSaveData> blackboards;

    // Wyzwalacze Int_lv3_RemoveBlackboard, przez ktore gracz juz przeszedl.
    // Po wczytaniu odtwarzamy na nich stan koncowy: wygaszony material
    // i zgaszona plyta.
    public List<string> removedBlackboardTriggers;

    // --- Lampy (niesione, zawieszone na uchwytach, stojace) ---
    // Zawieszenie lampy przepina ja pod gniazdo na scianie, wiec zmienia sie
    // jej sciezka w hierarchii. Zapisujemy rodzica, pozycje i stan wprost.
    public List<LampSaveData> lamps;

    // Lampy zawieszone na sciennych uchwytach (Int_lv3_WallLampHolder).
    // Zapisujemy numer lampy przy kazdym uchwycie - sam uchwyt wie,
    // gdzie sa jego lampy, wiec nie zalezymy od nazw obiektow.
    public List<WallLampSaveData> wallLamps;

    // Renderery modeli NPC. Postacie bywaja ukrywane przez wylaczenie
    // renderera, a nie calego obiektu - stad Ethel pojawiala sie po wczytaniu
    // mimo identycznego activeSelf i pozycji. Rendererow ogolnie NIE zapisujemy
    // (koliduje to z systemem DOCS od przezroczystych scian), wiec robimy
    // wyjatek tylko dla drzewa NPC.
    public List<ScriptFlagSaveData> npcRenderers;

    // Parametry rozmow (Dialogue Editor). To one decyduja, ktora galaz dialogu
    // zobaczy gracz - bez nich po wczytaniu wracaja stare wersje rozmow.
    public List<ScriptFlagSaveData> conversationFlags;

    // Uruchomione pulapki manekina (Int_lv3_Manequine). Zamykaja przejscie,
    // przepinaja sciane i wylaczaja poprzedni pokoj.
    public List<string> triggeredMannequinTraps;
    public List<string> unlockedBasementLadderIDs;

    // Parametry liczbowe rozmow (int i float). Niektore galezie dialogow
    // zaleza od licznikow, nie tylko od flag bool.
    public List<ConversationNumberSaveData> conversationNumbers;

    // Animatory spoza drzewa LEVELS - drzwi i mechanizmy stojace poza grupami
    // poziomow, np. na NPC albo w korzeniu sceny.
    public List<AnimatorSaveData> sceneAnimators;

    // Pozycje obiektow spoza drzewa LEVELS, ktore gracz moze przesuwac
    // (np. skrzynki Watsona). Zapisujemy tylko te z Rigidbody albo z nazwa
    // wskazujaca na ruchomy obiekt - calej sceny nie ma sensu utrwalac.
    public List<TransformSaveData> movableObjects;

    // --- Sledztwo (ClueManager): poszlaki, wnioski i questy ---
    // Zapisujemy nazwy assetow, bo indeksy przestaja pasowac po kazdej
    // zmianie kolejnosci w bazie.
    public List<string> collectedClues;
    public List<string> collectedConclusions;
    public List<string> collectedQuestConclusions;
    public List<string> completedQuests;
    public List<string> activeQuests;

    // Liczniki w skryptach (int i float): etap sekwencji, liczba zbadanych
    // elementow, liczba prob. Bez nich zagadki licza od zera po wczytaniu.
    public List<ScriptNumberSaveData> scriptNumbers;

    // --- Cele kamer postaci (LookAt) ---
    // Scena startuje z kamera wycelowana w Selme (seans), a przesuwa ja dopiero
    // dialog otwierajacy. Po wczytaniu ten dialog nie leci, wiec cel trzeba zapisac.
    public List<string> cameraLookAtIDs;

    // --- Questy (Postep) ---
    public bool sherlock_NPC1;
    public bool watson_NPC1;
    public bool sherlock_NPC2;
    public bool watson_NPC2;
    public bool rozmowaMiedzyGraczami;
    public bool czyPrzedmiotySiePojawily; // Czy QuestManager.SpawnHiddenItems zostal wywolany

    // --- Dziennik (stary JournalManager) ---
    public List<int> unlockedNoteIDs;

    // --- Notatnik (NotebookManager) ---
    // Nazwy assetow NoteData. To ten system faktycznie zbiera notatki w grze.
    public List<string> notebookNotes;
    public List<string> notebookUnreadNotes;

    // --- Tutoriale ---
    // Lista ID tutoriali, ktore gracz juz widzial (zeby nie pokazywac ich ponownie)
    public List<string> shownTutorialIDs;
}

// Postep panelu zadan (CluesLog). Osobna klasa, zeby GameData
// nie rozrastalo sie o czterdziesci pol.
[System.Serializable]
public class CluesLogSaveData
{
    // --- Miejsce zbrodni ---
    public bool crimeSceneCompleted;
    public bool crimeSceneVisible = true;
    public bool ideaPointPuzzleSolved;
    public int collectedEdithBodyClues;
    public int discoveredRoomIdeaPoints;

    // --- Mechanizm w stole ---
    public bool tableMechanismObjectiveStarted;
    public bool tableMechanismCompleted;
    public bool greenTableMechanismElementCollected;
    public bool redTableMechanismElementCollected;
    public bool blueTableMechanismElementCollected;

    // --- Przejscia i drzwi ---
    public bool secretDoorOpeningPuzzleSolved;
    public bool secretPassageExplored;

    // --- Powiazania Lady Edith / swiadkowie ---
    public bool connectionsCompleted;
    public bool connectionsVisible = true;
    public bool sessionWitnessesVisible = true;
    public int interviewedSessionWitnesses;
    public List<int> interviewedSessionWitnessIds;
    public bool askSelmaAboutHenrySpiritVisible;
    public bool askArthurAboutFoundItemsVisible;
    public bool askedArthurAboutRing;
    public bool askedArthurAboutPaper;

    // --- Pietro ---
    public bool upperFloorEvidenceVisible;
    public int collectedUpperFloorEvidence;

    // --- Piwnica ---
    public bool basementEvidenceVisible;
    public int collectedBasementEvidence;
    public List<string> collectedBasementEvidenceIds;
    public bool basementIdeaPointSearchCompleted;
    public int discoveredBasementIdeaPoints;

    // --- Szukanie Ethel ---
    public bool findEthelVisible;
    public bool findEthelCompleted;
    public bool findWayUpstairsVisible;
    public bool searchUpperFloorVisible;
    public bool findEthelHiddenDoorVisible;
    public bool investigateEthelPassageVisible;
    public bool searchBasementVisible;
    public bool findBasementExitVisible;
    public bool findBasementHiddenDoorVisible;
    public bool confrontSessionVisible;
    public bool followEthelVisible;
}

// Postep badania ciala Lady Edith (Int_EdithExamBody).
// Z tego odtwarza sie licznik "Zbadaj cialo Lady Edith. x/3".
[System.Serializable]
public class EdithExamSaveData
{
    public bool performed;                    // czy cialo zostalo w ogole zbadane
    public int collectedExamClueCount;        // licznik zebranych punktow badania
    public bool ringCpCollected;
    public bool paperCpCollected;
    public bool bulletCpExamAttempted;        // byla nieudana proba badania kuli
    public bool edithIdeaRevealed;
    public bool examinationCompleted;
    public bool allCpCollectedClueAdded;
    public bool arthurFoundItemsObjectiveAdded;
    public bool tutorialPopupShown;

    // Stan poszczegolnych punktow badania na ciele
    public bool ringPerformed;
    public bool paperPerformed;
    public bool bulletPerformed;
}

// Pozycja i stan jednego NPC.
[System.Serializable]
public class NpcSaveData
{
    public string objectId;      // sciezka w hierarchii, np. "NPCs/Selma_NPC"
    public Vector3 position;
    public Vector3 eulerAngles;  // obrot, zeby NPC nie patrzyl w zla strone
    public bool active;          // czy obiekt byl wlaczony
}

// Stan flagi bool w skrypcie (np. 'performed', 'isOpen', 'canOpen').
[System.Serializable]
public class ScriptFlagSaveData
{
    public string key;   // sciezka|typKomponentu|nazwaPola
    public bool value;
}

// Pozycja i obrot obiektu wzgledem rodzica.
[System.Serializable]
public class TransformSaveData
{
    public string objectId;
    public string stableId;       // Interactable GUID and relative child path; optional in older saves
    public string parentStableId;
    public string objectName;   // zapas, gdy obiekt zostal przepiety i sciezka sie zmienila
    public string parentId;   // zagadki potrafia przepinac obiekty pod innego rodzica
    public Vector3 localPosition;
    public Vector3 localEuler;
}

// Stan animatora - stan aktualny i parametry.
[System.Serializable]
public class AnimatorSaveData
{
    public string objectId;
    public int stateHash;
    public float normalizedTime;
    public List<string> boolNames;
    public List<bool> boolValues;
    public List<string> floatNames;
    public List<float> floatValues;
    public List<string> intNames;
    public List<int> intValues;
}

// Stan wlaczenia jednej grupy poziomu.
[System.Serializable]
public class LevelGroupSaveData
{
    public string objectId;   // sciezka w hierarchii
    public bool active;
}

// Alfa materialu czarnej plyty zakrywajacej pomieszczenie.
[System.Serializable]
public class BlackboardSaveData
{
    public string objectId;
    public string objectName;
    public float alpha;
    public bool active;            // czy obiekt plyty byl wlaczony
    public bool rendererEnabled;   // czy Mesh Renderer plyty byl wlaczony
}

// Stan pojedynczej lampy.
[System.Serializable]
public class LampSaveData
{
    public string objectId;
    public string objectName;
    public string parentId;
    public string parentName;
    public Vector3 localPosition;
    public Vector3 localEuler;
    public bool active;
}

// Lampa zawieszona na jednym sciennym uchwycie.
[System.Serializable]
public class WallLampSaveData
{
    public string holderId;
    public int mountedLampIndex;   // 0 = brak, 1 = Held Lamp 1, 2 = Held Lamp 2
}

// Parametr liczbowy rozmowy.
[System.Serializable]
public class ConversationNumberSaveData
{
    public string key;        // nazwaRozmowy|nazwaParametru
    public bool isInt;        // true = int, false = float
    public int intValue;
    public float floatValue;
}

// Licznik w skrypcie.
[System.Serializable]
public class ScriptNumberSaveData
{
    public string key;      // sciezka|typKomponentu|nazwaPola
    public bool isInt;
    public int intValue;
    public float floatValue;
}
