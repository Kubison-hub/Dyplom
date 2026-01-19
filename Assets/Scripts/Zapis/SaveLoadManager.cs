using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SaveGame()
    {
        // 1. Tworzymy nowy obiekt danych
        GameData data = new GameData();

        // 2. Zbieramy dane ze sceny

        // -- Gracze --
        if (QuestManager.Instance != null)
        {
            if (QuestManager.Instance.sherlockController != null)
                data.sherlockPosition = QuestManager.Instance.sherlockController.transform.position;

            if (QuestManager.Instance.watsonController != null)
                data.watsonPosition = QuestManager.Instance.watsonController.transform.position;

            // -- Questy --
            data.sherlock_NPC1 = QuestManager.Instance.sherlock_Gadal_Z_NPC1;
            data.watson_NPC1 = QuestManager.Instance.watson_Gadal_Z_NPC1;
            data.sherlock_NPC2 = QuestManager.Instance.sherlock_Gadal_Z_NPC2;
            data.watson_NPC2 = QuestManager.Instance.watson_Gadal_Z_NPC2;
            data.rozmowaMiedzyGraczami = QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta;

            if (QuestManager.Instance.hiddenItems != null && QuestManager.Instance.hiddenItems.Length > 0)
            {
                data.czyPrzedmiotySiePojawily = QuestManager.Instance.hiddenItems[0].activeSelf;
            }
        }

        // -- Ekwipunek --
        if (InventoryManager.Instance != null)
            data.itemsInInventory = InventoryManager.Instance.items;

        // -- Dziennik --
        if (JournalManager.Instance != null)
            data.unlockedNoteIDs = JournalManager.Instance.unlockedNoteIndices;

        // -- TUTORIALE (NOWOŒÆ) --
        if (TutorialManager.Instance != null)
        {
            data.shownTutorialIDs = TutorialManager.Instance.GetShownTutorials();
        }

        // 3. Zapis do pliku
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/savegame.json";
        File.WriteAllText(path, json);

        Debug.Log("Gra zapisana w: " + path);
    }

    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/savegame.json";

        if (File.Exists(path))
        {
            // 1. Wczytujemy
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);

            // 2. Rozprowadzamy dane

            // -- Gracze --
            if (QuestManager.Instance != null)
            {
                var sherlockCC = QuestManager.Instance.sherlockController.GetComponent<CharacterController>();
                var watsonCC = QuestManager.Instance.watsonController.GetComponent<CharacterController>();

                if (sherlockCC) sherlockCC.enabled = false;
                if (watsonCC) watsonCC.enabled = false;

                QuestManager.Instance.sherlockController.transform.position = data.sherlockPosition;
                QuestManager.Instance.watsonController.transform.position = data.watsonPosition;

                if (sherlockCC) sherlockCC.enabled = true;
                if (watsonCC) watsonCC.enabled = true;

                // -- Questy --
                QuestManager.Instance.sherlock_Gadal_Z_NPC1 = data.sherlock_NPC1;
                QuestManager.Instance.watson_Gadal_Z_NPC1 = data.watson_NPC1;
                QuestManager.Instance.sherlock_Gadal_Z_NPC2 = data.sherlock_NPC2;
                QuestManager.Instance.watson_Gadal_Z_NPC2 = data.watson_NPC2;
                QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta = data.rozmowaMiedzyGraczami;

                if (data.czyPrzedmiotySiePojawily)
                {
                    QuestManager.Instance.SpawnHiddenItems();
                }
            }

            // -- Ekwipunek --
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.items.Clear();
                foreach (ItemType item in data.itemsInInventory)
                {
                    InventoryManager.Instance.AddItem(item);
                }
            }

            // -- Dziennik --
            if (JournalManager.Instance != null && data.unlockedNoteIDs != null)
            {
                foreach (int id in data.unlockedNoteIDs)
                {
                    JournalManager.Instance.UnlockNote(id);
                }
            }

            // -- TUTORIALE (NOWOŒÆ) --
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.RestoreShownTutorials(data.shownTutorialIDs);
            }

            Debug.Log("Gra wczytana!");
        }
        else
        {
            Debug.LogWarning("Brak pliku zapisu!");
        }
    }
}