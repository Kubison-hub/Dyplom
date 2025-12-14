using UnityEngine;
using UnityEngine.InputSystem; // Potrzebne do sprawdzania, kto steruje
using DialogueEditor;

public class PartyInteraction : MonoBehaviour
{
    [Header("Przypisz plik dialogu miêdzy graczami")]
    public NPCConversation rozmowaMiedzyNami;

    // Czy druga postaæ jest blisko?
    private bool innyGraczWZasiegu = false;

    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy wesz³a w nas inna postaæ gracza (po Tagach)
        if (other.CompareTag("PlayerA") || other.CompareTag("PlayerB"))
        {
            // Upewniamy siê, ¿e to nie my sami (na wypadek dziwnej fizyki)
            if (other.gameObject != this.gameObject)
            {
                innyGraczWZasiegu = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerA") || other.CompareTag("PlayerB"))
        {
            innyGraczWZasiegu = false;
        }
    }

    private void Update()
    {
        // 1. ZABEZPIECZENIE: Sprawdzamy, czy to my sterujemy t¹ postaci¹.
        // Jeœli PlayerInput jest wy³¹czony, to znaczy, ¿e ta postaæ jest "botem" i nie powinna reagowaæ na klawisze.
        var input = GetComponent<PlayerInput>();
        if (input != null && !input.enabled) return;

        // 2. Jeœli sterujemy, wciœniêto "I" i druga postaæ jest blisko...
        if (Input.GetKeyDown(KeyCode.I) && innyGraczWZasiegu)
        {
            SprobujRozpoczacRozmowe();
        }
    }

    private void SprobujRozpoczacRozmowe()
    {
        // Upewniamy siê, ¿e QuestManager istnieje
        if (QuestManager.Instance != null)
        {
            // Warunek 1: Czy zaliczyliœmy wszystkie rozmowy z NPC?
            bool warunekSpelniony = QuestManager.Instance.CzyMogaRozmawiacZeSoba();

            // Warunek 2: Czy ta rozmowa ju¿ siê odby³a?
            bool juzRozmawialismy = QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta;

            if (warunekSpelniony && !juzRozmawialismy)
            {
                if (rozmowaMiedzyNami != null)
                {
                    // A. Uruchamiamy dialog
                    ConversationManager.Instance.StartConversation(rozmowaMiedzyNami);

                    // B. Oznaczamy w QuestManagerze, ¿e rozmowa siê odby³a (wykrzykniki znikn¹)
                    QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta = true;

                    // C. Odkrywamy ukryte przedmioty na mapie (NOWOŒÆ)
                    QuestManager.Instance.SpawnHiddenItems();
                }
                else
                {
                    Debug.LogError($"B£¥D: Postaæ {gameObject.name} nie ma przypisanego pliku dialogu w PartyInteraction!");
                }
            }
            else
            {
                // Opcjonalnie: Komunikat, jeœli gracz próbuje gadaæ za wczeœnie
                // Debug.Log("Jeszcze nie mo¿ecie rozmawiaæ lub rozmowa ju¿ siê odby³a.");
            }
        }
    }
}