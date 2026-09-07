using System.Collections;
using UnityEngine;

public class lvl2_Int_Letter : Lvl3InteractionDialogueBase
{
    private Interactable interactable;

    public bool performed = false;
    public bool deactivateAfterPerform = true;
    private bool letterAdded = false;
    public lvl2_Int_HatchExit hatchExit;
    public lvl2_Int_StairsExit stairsExit;

    [SerializeField] private AudioSource audioFX;
    [SerializeField] private Renderer rend;

    [Header("Pickup Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] pickupDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ten list mo¿e wyjaœniæ wiêcej, ni¿ siê wydaje.",
            duration = 3f
        }
    };

    [Header("Notebook")]
    [SerializeField] private bool openNotebookNoteAfterPickup = true;
    [SerializeField, Min(0)] private int notebookNoteIndex = 0;

    private Coroutine pickupCoroutine;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => pickupDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null && openNotebookNoteAfterPickup)
            interactable.addDatabaseNotesAutomatically = false;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        interactable.AddClue(0);

        if (audioFX != null)
        {
            audioFX.pitch = Random.Range(0.8f, 1.2f);
            audioFX.Play();
        }

        if (openNotebookNoteAfterPickup)
            interactable.AddAndOpenNote(notebookNoteIndex, -60f, true);

        interactable.isInteractableActive = false;
        if (player != null)
            player.currentInteractable = null;

        if (deactivateAfterPerform && rend != null)
            rend.enabled = false;

        if (pickupCoroutine != null)
            StopCoroutine(pickupCoroutine);

        pickupCoroutine = StartCoroutine(FinishPickupSequence(player));
    }

    private IEnumerator FinishPickupSequence(PlayerController player)
    {
        if (openNotebookNoteAfterPickup)
        {
            yield return null;
            while (NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen)
                yield return null;
        }

        PlayDialogue(player, pickupDialogue);
        yield return new WaitForSeconds(GetDialogueDuration(pickupDialogue));

        AddLetter();
        pickupCoroutine = null;
    }

    public void AddLetter()
    {
        if (letterAdded)
            return;

        letterAdded = true;
        if (hatchExit != null)
            hatchExit.AddLetter();

        if (stairsExit != null)
            stairsExit.AddLetter();

        Debug.Log("Letter Added");
    }

    private static float GetDialogueDuration(Lvl3DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return 0f;

        float totalDuration = 0f;
        foreach (Lvl3DialogueLine line in lines)
            totalDuration += line.duration > 0f ? line.duration : 3f;

        return totalDuration;
    }
}