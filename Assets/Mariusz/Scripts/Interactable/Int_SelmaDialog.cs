using DialogueEditor;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class Int_SelmaDialog : Lvl3InteractionDialogueBase
{
    public SmartNPC smartNPC;
    public Interactable interactable;

    public bool performed = false;
    public CinemachineCamera dialogCam;
    //public CinemachineSplineDolly splineDolly;
    public Transform cardPosition;

    [Header("Face Player Before Dialogue")]
    [SerializeField] private bool facePlayerBeforeDialogue = true;
    [SerializeField, Min(1f)] private float facePlayerTurnSpeed = 360f;
    [SerializeField, Min(0.1f)] private float facePlayerTolerance = 1f;

    [Header("Intro Dialogue")]
    [Tooltip("Played after Selma faces the player and before the SmartNPC conversation begins.")]
    [SerializeField] private Lvl3DialogueLine[] introDialogue;

    private bool dialogueStarting;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => introDialogue;

    public WatsonEscortNPC LinkedEscortNpc => smartNPC != null
        ? smartNPC.GetComponent<WatsonEscortNPC>()
        : null;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        //splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

    }

    private void Update()
    {
       
    }

    public void PerformInteraction(PlayerController player)
    {
        if (!dialogueStarting && ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            player.currentInteractable = null;
            StartCoroutine(BeginDialogue(player));

        }
        else
        {
            Debug.LogWarning("Cannot start Selma dialog while another conversation is active.");
            player.currentInteractable = null;
        }

       
        

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private IEnumerator BeginDialogue(PlayerController player)
    {
        dialogueStarting = true;

        if (facePlayerBeforeDialogue && smartNPC != null)
        {
            yield return NpcDialogueFacingUtility.FacePlayer(
                smartNPC.transform,
                player != null ? player.transform : null,
                facePlayerTurnSpeed,
                facePlayerTolerance);
        }

        if (introDialogue != null && introDialogue.Length > 0)
        {
            PlayDialogue(player, introDialogue);
            while (IsDialoguePlaying)
                yield return null;
        }

        if (smartNPC == null)
        {
            Debug.LogWarning($"{name}: SmartNPC is not assigned.", this);
            dialogueStarting = false;
            yield break;
        }

        smartNPC.SprawdzIZacznijRozmowe();

        if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
            StartConversationWithoutTrigger(player);

        if (ConversationManager.Instance == null || !ConversationManager.Instance.IsConversationActive)
        {
            dialogueStarting = false;
            yield break;
        }

        yield return null;
        while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            yield return null;

        CluesLog.Instance?.RegisterSessionWitnessInterview(smartNPC);
        dialogueStarting = false;
    }

    private void StartConversationWithoutTrigger(PlayerController player)
    {
        if (player == null)
            return;

        NPCConversation conversation = player.playerCharacter == PlayerCharacter.Watson
            ? smartNPC.rozmowaDlaPostaciB
            : smartNPC.rozmowaDlaPostaciA;

        if (conversation == null)
        {
            Debug.LogWarning($"{name}: The selected player has no assigned NPC conversation.", this);
            return;
        }

        string playerId = player.playerCharacter == PlayerCharacter.Watson ? "PlayerB" : "PlayerA";
        QuestManager.Instance?.OdnotujRozmowe(playerId, smartNPC.npcID);
        ConversationManager.Instance.StartConversation(conversation);

        if (smartNPC.noteIDToUnlock >= 0)
            JournalManager.Instance?.UnlockNote(smartNPC.noteIDToUnlock);
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            //splineDolly.CameraPosition = .8f;
            dialogCam.Priority = 50;
        }
    }


    


    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
