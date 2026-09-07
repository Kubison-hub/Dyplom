using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
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