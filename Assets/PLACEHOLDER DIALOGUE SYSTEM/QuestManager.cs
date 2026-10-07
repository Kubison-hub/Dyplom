using UnityEngine;
using UnityEngine.InputSystem;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Postêp Dialogów - NPC")]
    public bool sherlock_Gadal_Z_NPC1 = false;
    public bool watson_Gadal_Z_NPC1 = false;
    public bool sherlock_Gadal_Z_NPC2 = false;
    public bool watson_Gadal_Z_NPC2 = false;

    [Header("Fina³")]
    public bool rozmowaMiedzyGraczamiOdbyta = false;

    [Header("Postacie (do sprawdzania Inputu)")]
    public PlayerController sherlockController;
    public PlayerController watsonController;

    [Header("Ikony Wykrzykników")]
    public GameObject wykrzyknikNadSherlockiem;
    public GameObject wykrzyknikNadWatsonem;

    [Header("Przedmioty do odblokowania")]
    public GameObject[] hiddenItems; // <--- Tutaj wrzucisz te 3 przedmioty

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Na starcie gry automatycznie ukrywamy przedmioty z listy
        if (hiddenItems != null)
        {
            foreach (GameObject item in hiddenItems)
            {
                if (item != null) item.SetActive(false);
            }
        }
    }

    private void Update()
    {
        ManageNotifications();
    }

    // Funkcja wywo³ywana przez SmartNPC
    public void OdnotujRozmowe(string tagGracza, int idNPC)
    {
        if (tagGracza == "PlayerA")
        {
            if (idNPC == 1) sherlock_Gadal_Z_NPC1 = true;
            if (idNPC == 2) sherlock_Gadal_Z_NPC2 = true;
        }
        else if (tagGracza == "PlayerB")
        {
            if (idNPC == 1) watson_Gadal_Z_NPC1 = true;
            if (idNPC == 2) watson_Gadal_Z_NPC2 = true;
        }
    }

    // --- FUNKCJA ODKRYWAJ¥CA PRZEDMIOTY + PO£¥CZONY TUTORIAL ---
    // Wywo³ywana przez PartyInteraction po zakoñczeniu rozmowy miêdzy detektywami
    public void SpawnHiddenItems()
    {
        if (hiddenItems != null)
        {
            // 1. Poka¿ przedmioty na scenie
            foreach (GameObject item in hiddenItems)
            {
                if (item != null)
                {
                    item.SetActive(true);
                    // Opcjonalnie: Instantiate(spawnEffect, item.transform.position, Quaternion.identity);
                }
            }
            Debug.Log("QuestManager: Przedmioty pojawi³y siê na mapie!");

            // 2. Wyœwietl PO£¥CZONY Tutorial (Przedmioty + Klawisz J)
            // if (TutorialManager.Instance != null)
            {
                //string trescKomunikatu =
                //    "Na mapie pojawi³y siê przedmioty. PodejdŸ do nich, aby je zebraæ.\n\n" +
                //    "Wskazówka: Wciœnij 'J', aby w³¹czyæ tryb Detektywa i ³atwiej je znaleŸæ.";

                //// U¿ywamy unikalnego ID, ¿eby pokaza³o siê to tylko raz
                //TutorialManager.Instance.PokazTutorial(trescKomunikatu, "ItemsAndDetectiveMode");
            }
        }
    }
    // -------------------------------------------------

    private void ManageNotifications()
    {
        if (wykrzyknikNadSherlockiem) wykrzyknikNadSherlockiem.SetActive(false);
        if (wykrzyknikNadWatsonem) wykrzyknikNadWatsonem.SetActive(false);

        // Jeœli ju¿ pogadali ze sob¹, nie pokazuj wykrzykników
        if (rozmowaMiedzyGraczamiOdbyta) return;

        // Jeœli jeszcze nie odblokowali mo¿liwoœci rozmowy (nie pogadali z NPC), te¿ nie pokazuj
        if (!CzyMogaRozmawiacZeSoba()) return;

        // Jeœli mog¹ gadaæ, poka¿ wykrzyknik nad t¹ postaci¹, któr¹ NIE sterujemy
        if (sherlockController != null && watsonController != null)
        {
            var sherlockInput = sherlockController.GetComponent<PlayerInput>();
            var watsonInput = watsonController.GetComponent<PlayerInput>();

            if (sherlockInput != null && sherlockInput.enabled)
            {
                // Gracz steruje Sherlockiem -> Wykrzyknik nad Watsonem
                if (wykrzyknikNadWatsonem) wykrzyknikNadWatsonem.SetActive(true);
            }
            else if (watsonInput != null && watsonInput.enabled)
            {
                // Gracz steruje Watsonem -> Wykrzyknik nad Sherlockiem
                if (wykrzyknikNadSherlockiem) wykrzyknikNadSherlockiem.SetActive(true);
            }
        }
    }

    public bool CzyMogaRozmawiacZeSoba()
    {
        // Warunek: Obaj gracze musieli porozmawiaæ z oboma NPC
        bool npc1_Done = sherlock_Gadal_Z_NPC1 && watson_Gadal_Z_NPC1;
        bool npc2_Done = sherlock_Gadal_Z_NPC2 && watson_Gadal_Z_NPC2;
        return npc1_Done && npc2_Done;
    }
}