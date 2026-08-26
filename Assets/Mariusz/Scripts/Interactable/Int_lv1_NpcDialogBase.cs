using DialogueEditor;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public abstract class Int_lv1_NpcDialogBase : MonoBehaviour
{
    [SerializeField] private SmartNPC smartNPC;
    [Header("Face Player Before Dialogue")]
    [SerializeField] private bool facePlayerBeforeDialogue = true;
    [SerializeField, Min(1f)] private float facePlayerTurnSpeed = 360f;
    [SerializeField, Min(0.1f)] private float facePlayerTolerance = 1f;

    private Interactable interactable;
    private bool dialogueStarting;
    private Coroutine dialogueEndWatcher;

    protected abstract InteractionType RequiredInteractionType { get; }
    protected virtual bool ShouldFacePlayerBeforeDialogue => facePlayerBeforeDialogue;
    protected virtual void OnNpcDialogueStarted() { }
    protected virtual void OnNpcDialogueFinished() { }

    protected virtual void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(RequiredInteractionType);

        if (smartNPC == null)
            smartNPC = GetComponent<SmartNPC>();

        if (smartNPC == null)
            smartNPC = GetComponentInChildren<SmartNPC>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (dialogueStarting || ConversationManager.Instance == null || ConversationManager.Instance.IsConversationActive)
        {
            Debug.LogWarning($"Cannot start {name} dialog while another conversation is active.");
            ClearPlayerInteraction(player);
            return;
        }

        if (smartNPC == null)
        {
            Debug.LogWarning($"{name}: SmartNPC is not assigned.", this);
            ClearPlayerInteraction(player);
            return;
        }

        ClearPlayerInteraction(player);
        StartCoroutine(BeginDialogue(player));
    }

    private IEnumerator BeginDialogue(PlayerController player)
    {
        dialogueStarting = true;

        if (ShouldFacePlayerBeforeDialogue)
            yield return NpcDialogueFacingUtility.FacePlayer(
                smartNPC.transform,
                player != null ? player.transform : null,
                facePlayerTurnSpeed,
                facePlayerTolerance);

        smartNPC.SprawdzIZacznijRozmowe();

        if (!ConversationManager.Instance.IsConversationActive)
            StartConversationWithoutTrigger(player);

        OnNpcDialogueStarted();

        if (dialogueEndWatcher != null)
            StopCoroutine(dialogueEndWatcher);

        dialogueEndWatcher = StartCoroutine(WaitForDialogueToFinish());
        dialogueStarting = false;
    }

    private IEnumerator WaitForDialogueToFinish()
    {
        // Let ConversationManager update its active state before checking it.
        yield return null;

        while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            yield return null;

        dialogueEndWatcher = null;
        OnNpcDialogueFinished();
    }

    private void StartConversationWithoutTrigger(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning($"{name}: PlayerController is missing, so the NPC dialog cannot be selected.", this);
            return;
        }

        NPCConversation conversation = player.playerCharacter == PlayerCharacter.Watson
            ? smartNPC.rozmowaDlaPostaciB
            : smartNPC.rozmowaDlaPostaciA;

        if (conversation == null)
        {
            Debug.LogWarning($"{name}: The selected player has no assigned NPC conversation.", this);
            return;
        }

        string playerId = player.playerCharacter == PlayerCharacter.Watson ? "PlayerB" : "PlayerA";

        if (QuestManager.Instance != null)
            QuestManager.Instance.OdnotujRozmowe(playerId, smartNPC.npcID);

        ConversationManager.Instance.StartConversation(conversation);

        if (smartNPC.noteIDToUnlock >= 0 && JournalManager.Instance != null)
            JournalManager.Instance.UnlockNote(smartNPC.noteIDToUnlock);
    }

    private static void ClearPlayerInteraction(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;
    }
}
