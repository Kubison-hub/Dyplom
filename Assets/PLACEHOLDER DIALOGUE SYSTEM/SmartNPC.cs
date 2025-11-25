using UnityEngine;
using DialogueEditor;

public class SmartNPC : MonoBehaviour
{
    [Header("Przypisz pliki dialogów")]
    public NPCConversation rozmowaDlaPostaciA;
    public NPCConversation rozmowaDlaPostaciB;

    private bool czyPostacA_W_Zasiegu = false;
    private bool czyPostacB_W_Zasiegu = false;

    private GameObject obiektGraczaA;
    private GameObject obiektGraczaB;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerA"))
        {
            czyPostacA_W_Zasiegu = true;
            obiektGraczaA = other.gameObject;
        }
        else if (other.CompareTag("PlayerB"))
        {
            czyPostacB_W_Zasiegu = true;
            obiektGraczaB = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerA"))
        {
            czyPostacA_W_Zasiegu = false;
            obiektGraczaA = null;
        }
        else if (other.CompareTag("PlayerB"))
        {
            czyPostacB_W_Zasiegu = false;
            obiektGraczaB = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
            {
                SprawdzIZacznijRozmowe();
            }
        }
    }

    private void SprawdzIZacznijRozmowe()
    {
        if (czyPostacA_W_Zasiegu && CzyToAktywnyGracz(obiektGraczaA))
        {
            Debug.Log("NPC: Rozmawiam z Postaci¹ A");
            if (QuestManager.Instance != null) QuestManager.Instance.postacA_Rozmawiala = true;
            ConversationManager.Instance.StartConversation(rozmowaDlaPostaciA);
        }
        else if (czyPostacB_W_Zasiegu && CzyToAktywnyGracz(obiektGraczaB))
        {
            Debug.Log("NPC: Rozmawiam z Postaci¹ B");
            if (QuestManager.Instance != null) QuestManager.Instance.postacB_Rozmawiala = true;
            ConversationManager.Instance.StartConversation(rozmowaDlaPostaciB);
        }
    }

    private bool CzyToAktywnyGracz(GameObject gracz)
    {
        if (gracz == null) return false;
        return true;
    }
}