using UnityEngine;

// Definiuje aktualnie kontrolowan¹ postaæ.
public enum CurrentPlayer
{
    Sherlock,
    Watson
}

public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance { get; private set; }

    [Header("Ustawienia Kontekstu")]
    [Tooltip("Postaæ, któr¹ obecnie kontroluje gracz.")]
    public CurrentPlayer activePlayer = CurrentPlayer.Sherlock;

    // S³owniki œledz¹ce, czy dana postaæ rozmawia³a ju¿ z tym NPC w kontekœcie tego drzewka.
    private readonly string _appId = typeof(__app_id) != null ? __app_id.ToString() : "default-app-id";

    // Zast¹p lokalne œledzenie u¿yciem Firestore, aby umo¿liwiæ persistence
    // Zostawiam tu tylko definicjê, faktyczne dane bêd¹ w Firestore
    // private Dictionary<string, bool> sherlockSpoke = new Dictionary<string, bool>();
    // private Dictionary<string, bool> watsonSpoke = new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ten obiekt nie powinien byæ niszczony przy prze³adowaniu sceny, jeœli to konieczne
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public CurrentPlayer GetActivePlayer()
    {
        return activePlayer;
    }

    public bool IsSherlockActive()
    {
        return activePlayer == CurrentPlayer.Sherlock;
    }

    public bool IsWatsonActive()
    {
        return activePlayer == CurrentPlayer.Watson;
    }

    // Metoda do prze³¹czania aktywnej postaci (do zaimplementowania w mechanice gry)
    public void SwitchActivePlayer(CurrentPlayer newPlayer)
    {
        activePlayer = newPlayer;
        Debug.Log($"Aktywna postaæ prze³¹czona na: {activePlayer}");
        // TODO: Wywo³anie zdarzenia do aktualizacji UI gry
    }
}