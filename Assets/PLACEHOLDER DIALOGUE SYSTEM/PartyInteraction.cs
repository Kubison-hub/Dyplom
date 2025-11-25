using UnityEngine;
using DialogueEditor;

public class PartyInteraction : MonoBehaviour
{
    [Header("Dialog miêdzy graczami")]
    public NPCConversation rozmowaDruzynowa;

    private bool innyGraczBlisko = false;

    // Wykrywamy drugiego gracza (zak³adamy, ¿e ten skrypt jest na PlayerA i PlayerB)
    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy to "kolega z dru¿yny" (czyli tag PlayerA lub PlayerB)
        if (other.CompareTag("PlayerA") || other.CompareTag("PlayerB"))
        {
            // Upewniamy siê, ¿e nie wykrywamy samego siebie
            if (other.gameObject != this.gameObject)
            {
                innyGraczBlisko = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerA") || other.CompareTag("PlayerB"))
        {
            innyGraczBlisko = false;
        }
    }

    private void Update()
    {
        // Jeœli wciœniêto "I" i kolega jest blisko
        if (Input.GetKeyDown(KeyCode.I) && innyGraczBlisko)
        {
            // 1. Sprawdzamy czy nie trwa ju¿ jakaœ rozmowa (¿eby nie odpaliæ dwa razy)
            if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
                return;

            // 2. KLUCZOWE: Pytamy Managera, czy warunek jest spe³niony
            if (QuestManager.Instance.CzyMogaRozmawiacZeSoba())
            {
                Debug.Log("Warunek spe³niony! Rozmowa dru¿ynowa.");
                ConversationManager.Instance.StartConversation(rozmowaDruzynowa);
            }
            else
            {
                Debug.Log("Jeszcze nie pogadaliœmy z NPC-em!");
                // Opcjonalnie: Mo¿esz tu wyœwietliæ dymek "Musimy najpierw pogadaæ z Wodzem."
            }
        }
    }
}