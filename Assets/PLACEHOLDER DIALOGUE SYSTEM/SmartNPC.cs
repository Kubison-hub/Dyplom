using DialogueEditor;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SmartNPC : MonoBehaviour
{
    [Header("Ustawienia NPC")]
    [Tooltip("Wpisz 1 dla pierwszego NPC, wpisz 2 dla drugiego NPC")]
    public int npcID = 1;

    [Header("Dziennik (Notatki)")]
    [Tooltip("Numer notatki w JournalManager, która ma siê odkryæ po rozmowie. Wpisz -1 jeœli ten NPC nie daje notatki.")]
    public int noteIDToUnlock = -1; // <--- NOWOŒÆ

    [Header("Przypisz pliki dialogów")]
    public NPCConversation rozmowaDlaPostaciA; // Sherlock
    public NPCConversation rozmowaDlaPostaciB; // Watson

    private bool czyPostacA_W_Zasiegu = false;
    private bool czyPostacB_W_Zasiegu = false;

    private GameObject obiektGraczaA;
    private GameObject obiektGraczaB;

    public NavMeshAgent navMeshAgent;
    public Transform movePoint;
    private Coroutine movementCoroutine;

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

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponentInParent<NavMeshAgent>();
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

    public void SprawdzIZacznijRozmowe()
    {

        bool dialogRozpoczety = false;

        // --- Interakcja dla Sherlocka (PlayerA) ---
        if (czyPostacA_W_Zasiegu && CzyToAktywnyGracz(obiektGraczaA))
        {
            Debug.Log($"NPC {npcID}: Rozmawiam z Postaci¹ A");

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OdnotujRozmowe("PlayerA", npcID);
            }

            ConversationManager.Instance.StartConversation(rozmowaDlaPostaciA);
            dialogRozpoczety = true;
        }
        // --- Interakcja dla Watsona (PlayerB) ---
        else if (czyPostacB_W_Zasiegu && CzyToAktywnyGracz(obiektGraczaB))
        {
            Debug.Log($"NPC {npcID}: Rozmawiam z Postaci¹ B");

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OdnotujRozmowe("PlayerB", npcID);
            }

            ConversationManager.Instance.StartConversation(rozmowaDlaPostaciB);
            dialogRozpoczety = true;
        }

        // --- OBS£UGA DZIENNIKA (Wspólna dla obu graczy) ---
        if (dialogRozpoczety)
        {
            // Jeœli przypisano poprawny ID notatki (ró¿ny od -1)
            if (noteIDToUnlock >= 0 && JournalManager.Instance != null)
            {
                JournalManager.Instance.UnlockNote(noteIDToUnlock);
            }
        }
    }

    private bool CzyToAktywnyGracz(GameObject gracz)
    {
        if (gracz == null) return false;

        var input = gracz.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null && !input.enabled) return false;

        return true;
    }

    public void GoToPoint(Vector3 destination, Action onReachedDestination = null) 
    {
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);

        navMeshAgent.SetDestination(destination);

        movementCoroutine = StartCoroutine(WaitForArrival(onReachedDestination));
    }

    private IEnumerator WaitForArrival(Action onReached)
    {
        yield return new WaitUntil(() => navMeshAgent.pathPending == false);

        while (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
        {
            yield return null;
        }

        if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
        {
            onReached?.Invoke();
            movementCoroutine = null;
        }
    }
}