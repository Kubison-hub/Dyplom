using UnityEngine;
using DialogueEditor; // Upewnij siê, ¿e to jest poprawny namespace dla Twojego assetu

public class StarterConversation : MonoBehaviour
{
    [SerializeField] private NPCConversation myConversation;

    // Ta flaga bêdzie œledziæ, czy gracz jest w zasiêgu.
    // Musi byæ na górze, poza funkcjami.
    private bool playerIsNear = false;

    // 1. Wywo³a siê RAZ, gdy gracz wejdzie w trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            Debug.Log("Gracz wszed³ w obszar.");
            // Tutaj mo¿na te¿ pokazaæ UI, np. "Naciœnij [I] aby rozmawiaæ"
        }
    }

    // 2. Wywo³a siê RAZ, gdy gracz opuœci trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            Debug.Log("Gracz opuœci³ obszar.");
            // Tutaj mo¿na ukryæ UI
        }
    }

    // 3. Ta funkcja dzia³a co klatkê (renderowania)
    private void Update()
    {
        // Sprawdzaj input tylko jeœli gracz jest blisko
        if (playerIsNear && Input.GetKeyDown(KeyCode.I))
        {
            // SprawdŸ, czy mened¿er istnieje I czy rozmowa ju¿ nie trwa
            if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
            {
                // SprawdŸ, czy konwersacja jest przypisana
                if (myConversation != null)
                {
                    Debug.Log("Uruchamiam konwersacjê: " + myConversation.name);
                    ConversationManager.Instance.StartConversation(myConversation);
                }
                else
                {
                    Debug.LogWarning("Brak przypisanej konwersacji na obiekcie: " + gameObject.name);
                }
            }
            else if (ConversationManager.Instance == null)
            {
                Debug.LogError("ConversationManager.Instance jest NULL!");
            }
            else if (ConversationManager.Instance.IsConversationActive)
            {
                Debug.LogWarning("Inna rozmowa jest ju¿ aktywna.");
            }
        }
    }
}