using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance; // Singleton

    // Flagi (czy zadanie wykonane?)
    public bool postacA_Rozmawiala = false;
    public bool postacB_Rozmawiala = false;

    private void Awake()
    {
        // Singleton - zapewnia, ¿e jest tylko jeden QuestManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Funkcja sprawdzaj¹ca, czy dialog miêdzy graczami jest odblokowany
    public bool CzyMogaRozmawiacZeSoba()
    {
        // Zwraca prawdê tylko, jeœli OBOJE ju¿ rozmawiali
        return postacA_Rozmawiala && postacB_Rozmawiala;
    }
}