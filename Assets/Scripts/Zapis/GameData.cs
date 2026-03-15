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

    // --- Questy (Postêp) ---
    public bool sherlock_NPC1;
    public bool watson_NPC1;
    public bool sherlock_NPC2;
    public bool watson_NPC2;
    public bool rozmowaMiedzyGraczami;
    public bool czyPrzedmiotySiePojawily; // Czy QuestManager.SpawnHiddenItems zosta³ wywo³any

    // --- Dziennik ---
    public List<int> unlockedNoteIDs;

    // --- NOWOŒÆ: Tutoriale ---
    // Lista ID tutoriali, które gracz ju¿ widzia³ (¿eby nie pokazywaæ ich ponownie)
    public List<string> shownTutorialIDs;
}