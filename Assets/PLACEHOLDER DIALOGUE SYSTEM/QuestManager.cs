using UnityEngine;
using UnityEngine.InputSystem;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    // --- LOGIKA ZADANIA ---
    public bool postacA_Rozmawiala = false;
    public bool postacB_Rozmawiala = false;

    // NOWA ZMIENNA: Czy Sherlock i Watson ju¿ ze sob¹ pogadali?
    public bool rozmowaMiedzyGraczamiOdbyta = false;

    [Header("Postacie")]
    public PlayerController sherlockController;
    public PlayerController watsonController;

    [Header("Ikony Wykrzykników")]
    public GameObject wykrzyknikNadSherlockiem;
    public GameObject wykrzyknikNadWatsonem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        ManageNotifications();
    }

    private void ManageNotifications()
    {
        // 1. Reset (wy³¹cz oba)
        if (wykrzyknikNadSherlockiem) wykrzyknikNadSherlockiem.SetActive(false);
        if (wykrzyknikNadWatsonem) wykrzyknikNadWatsonem.SetActive(false);

        // --- NOWY WARUNEK ---
        // Jeœli rozmowa ju¿ siê odby³a, wychodzimy z funkcji (wykrzykniki pozostaj¹ wy³¹czone)
        if (rozmowaMiedzyGraczamiOdbyta) return;

        // 2. Jeœli nie s¹ gotowi do rozmowy, te¿ wychodzimy
        if (!CzyMogaRozmawiacZeSoba()) return;

        // 3. Sprawdzamy Input
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
        return postacA_Rozmawiala && postacB_Rozmawiala;
    }
}