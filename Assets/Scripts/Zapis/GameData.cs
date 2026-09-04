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

    // --- NOWE: przedmioty podniesione ze swiata ---
    // Lista ID obiektow PickupItem, ktore gracz juz podniosl.
    // Po wczytaniu zapisu te obiekty sa usuwane ze sceny.
    public List<string> pickedUpItemIDs;

    // --- Questy (Postep) ---
    public bool sherlock_NPC1;
    public bool watson_NPC1;
    public bool sherlock_NPC2;
    public bool watson_NPC2;
    public bool rozmowaMiedzyGraczami;
    public bool czyPrzedmiotySiePojawily; // Czy QuestManager.SpawnHiddenItems zostal wywolany

    // --- Dziennik ---
    public List<int> unlockedNoteIDs;

    // --- Tutoriale ---
    // Lista ID tutoriali, ktore gracz juz widzial (zeby nie pokazywac ich ponownie)
    public List<string> shownTutorialIDs;
}