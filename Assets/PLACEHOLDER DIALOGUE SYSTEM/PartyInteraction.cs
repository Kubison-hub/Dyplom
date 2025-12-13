using UnityEngine;
using DialogueEditor; // Wymagane do obs³ugi dialogów

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
        // Jeœli wciœniêto "I" i druga postaæ jest blisko...
        if (Input.GetKeyDown(KeyCode.I) && innyGraczWZasiegu)
        {
            SprobujRozpoczacRozmowe();
        }
    }

    private void SprobujRozpoczacRozmowe()
    {
        // Sprawdzamy Quest Managera
        if (QuestManager.Instance != null)
        {
            // Warunek 1: Czy w ogóle mo¿emy gadaæ? (Czy oboje pogadali z NPC?)
            bool warunekSpelniony = QuestManager.Instance.CzyMogaRozmawiacZeSoba();

            // Warunek 2: Czy ju¿ przypadkiem nie pogadaliœmy?
            bool juzRozmawialismy = QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta;

            if (warunekSpelniony && !juzRozmawialismy)
            {
                // 1. Wy³¹czamy wykrzykniki (TO JEST TA KLUCZOWA LINIA)
                QuestManager.Instance.rozmowaMiedzyGraczamiOdbyta = true;

                // 2. Uruchamiamy dialog
                if (rozmowaMiedzyNami != null)
                {
                    ConversationManager.Instance.StartConversation(rozmowaMiedzyNami);
                }
                else
                {
                    Debug.LogWarning("Nie przypisano pliku rozmowy w PartyInteraction!");
                }
            }
            else
            {
                Debug.Log("Jeszcze nie mo¿ecie rozmawiaæ lub rozmowa ju¿ siê odby³a.");
            }
        }
    }
}