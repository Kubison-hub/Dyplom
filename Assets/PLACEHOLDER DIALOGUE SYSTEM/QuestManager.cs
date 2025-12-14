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

    [Header("Postacie")]
    public PlayerController sherlockController;
    public PlayerController watsonController;

    [Header("Ikony Wykrzykników")]
    public GameObject wykrzyknikNadSherlockiem;
    public GameObject wykrzyknikNadWatsonem;

    // --- NOWOŒÆ: Lista przedmiotów do pojawienia siê ---
    [Header("Przedmioty do odblokowania")]
    public GameObject[] hiddenItems; // <--- Tutaj wrzucisz te 3 przedmioty
    // ---------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // --- NOWOŒÆ: Na starcie gry automatycznie ukrywamy te przedmioty ---
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

    // --- NOWOŒÆ: Funkcja odkrywaj¹ca przedmioty ---
    public void SpawnHiddenItems()
    {
        if (hiddenItems != null)
        {
            foreach (GameObject item in hiddenItems)
            {
                if (item != null)
                {
                    item.SetActive(true);
                    // Opcjonalnie: Mo¿esz tu dodaæ efekt dŸwiêkowy lub cz¹steczkowy
                    // Instantiate(spawnEffect, item.transform.position, Quaternion.identity);
                }
            }
            Debug.Log("QuestManager: Przedmioty pojawi³y siê na mapie!");
        }
    }
    // ----------------------------------------------

    private void ManageNotifications()
    {
        if (wykrzyknikNadSherlockiem) wykrzyknikNadSherlockiem.SetActive(false);
        if (wykrzyknikNadWatsonem) wykrzyknikNadWatsonem.SetActive(false);

        if (rozmowaMiedzyGraczamiOdbyta) return;
        if (!CzyMogaRozmawiacZeSoba()) return;

        if (sherlockController != null && watsonController != null)
        {
            var sherlockInput = sherlockController.GetComponent<PlayerInput>();
            var watsonInput = watsonController.GetComponent<PlayerInput>();

            if (sherlockInput != null && sherlockInput.enabled)
            {
                if (wykrzyknikNadWatsonem) wykrzyknikNadWatsonem.SetActive(true);
            }
            else if (watsonInput != null && watsonInput.enabled)
            {
                if (wykrzyknikNadSherlockiem) wykrzyknikNadSherlockiem.SetActive(true);
            }
        }
    }

    public bool CzyMogaRozmawiacZeSoba()
    {
        bool npc1_Done = sherlock_Gadal_Z_NPC1 && watson_Gadal_Z_NPC1;
        bool npc2_Done = sherlock_Gadal_Z_NPC2 && watson_Gadal_Z_NPC2;
        return npc1_Done && npc2_Done;
    }
}