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
    [Tooltip("Numer notatki w JournalManager, która ma się odkryć po rozmowie. Wpisz -1 jeśli ten NPC nie daje notatki.")]
    public int noteIDToUnlock = -1; // <--- NOWOŚĆ

    [Header("Przypisz pliki dialogów")]
    public NPCConversation rozmowaDlaPostaciA; // Sherlock
    public NPCConversation rozmowaDlaPostaciB; // Watson

    [Header("Dialogue Camera")]
    [Tooltip("Point the camera looks at during this NPC conversation. Falls back to the NPC root when empty.")]
    [SerializeField] private Transform dialogueCameraTarget;
    [SerializeField, Min(0.01f)] private float dialogueCameraRestoreTransitionSpeed = 3f;

    private bool czyPostacA_W_Zasiegu = false;
    private bool czyPostacB_W_Zasiegu = false;

    private GameObject obiektGraczaA;
    private GameObject obiektGraczaB;

    public NavMeshAgent navMeshAgent;
    public Transform movePoint;
    private Coroutine movementCoroutine;
    private CameraController dialogueCameraController;
    private bool dialogueCameraFocusActive;
    private Transform dialogueCameraReturnTarget;

    private void OnDestroy()
    {
        RestoreDialogueCameraFocus();
    }

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
            Debug.Log($"NPC {npcID}: Rozmawiam z Postacią A");

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OdnotujRozmowe("PlayerA", npcID);
            }

            BeginDialogueCameraFocus(rozmowaDlaPostaciA);
            ConversationManager.Instance.StartConversation(rozmowaDlaPostaciA);
            dialogRozpoczety = true;
        }
        // --- Interakcja dla Watsona (PlayerB) ---
        else if (czyPostacB_W_Zasiegu && CzyToAktywnyGracz(obiektGraczaB))
        {
            Debug.Log($"NPC {npcID}: Rozmawiam z Postacią B");

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OdnotujRozmowe("PlayerB", npcID);
            }

            BeginDialogueCameraFocus(rozmowaDlaPostaciB);
            ConversationManager.Instance.StartConversation(rozmowaDlaPostaciB);
            dialogRozpoczety = true;
        }

        // --- OBSŁUGA DZIENNIKA (Wspólna dla obu graczy) ---
        if (dialogRozpoczety)
        {
            // Jeśli przypisano poprawny ID notatki (różny od -1)
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

    public void BeginDialogueCameraFocus()
    {
        BeginDialogueCameraFocus(null);
    }

    public void BeginDialogueCameraFocus(NPCConversation conversation)
    {
        if (conversation != null && !conversation.UseAutomaticDialogueCamera)
            return;

        Transform target = dialogueCameraTarget != null ? dialogueCameraTarget : transform;
        CameraController activeCameraController = GetActiveCameraController();
        if (target == null || activeCameraController == null)
            return;

        if (dialogueCameraFocusActive && dialogueCameraController != activeCameraController)
            RestoreDialogueCameraFocus();

        dialogueCameraController = activeCameraController;
        float preRollDuration = conversation != null
            ? conversation.AutomaticDialogueCameraPreRollTime
            : 0.35f;
        dialogueCameraController.BeginDialogueLookAt(target, preRollDuration);

        if (!dialogueCameraFocusActive)
        {
            ConversationManager.OnConversationUIHidden += RestoreDialogueCameraFocus;
            dialogueCameraFocusActive = true;
        }
    }

    public void BeginDialogueCameraFocusWithZoom(NPCConversation conversation, string zoomPresetName)
    {
        BeginDialogueCameraFocus(conversation);

        if (!string.IsNullOrWhiteSpace(zoomPresetName))
        {
            float preRollDuration = conversation != null
                ? conversation.AutomaticDialogueCameraPreRollTime
                : 0.35f;
            dialogueCameraController?.BeginDialogueZoomPreset(zoomPresetName, preRollDuration);
        }
    }

    public void BeginDialogueCameraFocusWithZoom(string zoomPresetName, float preRollDuration)
    {
        BeginDialogueCameraFocus();
        if (!string.IsNullOrWhiteSpace(zoomPresetName))
            dialogueCameraController?.BeginDialogueZoomPreset(zoomPresetName, preRollDuration);
    }

    public void EndDialogueCameraFocus()
    {
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            return;

        RestoreDialogueCameraFocus();
    }

    public void SetDialogueCameraReturnTarget(Transform target)
    {
        dialogueCameraReturnTarget = target;
    }

    private void RestoreDialogueCameraFocus()
    {
        if (dialogueCameraFocusActive)
            ConversationManager.OnConversationUIHidden -= RestoreDialogueCameraFocus;

        dialogueCameraController?.RestoreDialogueLookAt(
            dialogueCameraReturnTarget,
            dialogueCameraRestoreTransitionSpeed);
        dialogueCameraController = null;
        dialogueCameraFocusActive = false;
        dialogueCameraReturnTarget = null;
    }

    private static CameraController GetActiveCameraController()
    {
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null && switchCharacter.playersCamera != null)
        {
            int activeIndex = switchCharacter.activePlayerIndex;
            if (activeIndex >= 0 && activeIndex < switchCharacter.playersCamera.Length &&
                switchCharacter.playersCamera[activeIndex] != null)
            {
                CameraController controller = switchCharacter.playersCamera[activeIndex]
                    .GetComponent<CameraController>();
                if (controller != null)
                    return controller;
            }
        }

        return FindFirstObjectByType<CameraController>();
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
