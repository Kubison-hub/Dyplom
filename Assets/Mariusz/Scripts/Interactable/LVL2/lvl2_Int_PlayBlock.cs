using UnityEngine;

public class lvl2_Int_PlayBlock : Lvl3InteractionDialogueBase
{
    private static readonly Lvl3DialogueLine[] DefaultPlayBlocksDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Te klocki muszą do czegoś służyć.",
            duration = 2f
        }
    };

    private static readonly Lvl3DialogueLine[] DefaultEthelRoomBlocksDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Klocki Ethel tworzą znajomy wzór.",
            duration = 2f
        }
    };

    private Interactable interactable;

    public bool performed = false;
    public bool EthelRoomBlocks;

    protected override Lvl3DialogueLine[] DefaultDialogueLines =>
        EthelRoomBlocks ? DefaultEthelRoomBlocksDialogue : DefaultPlayBlocksDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");
        if (EthelRoomBlocks)
        {
            interactable.AddClue(0);
            PlayInteractionDialogue(player);
            Debug.Log("Ethel Room PlayBlocks interacted");
        }
        else
        {
            interactable.AddClue(1);
            PlayInteractionDialogue(player);
            Debug.Log("PlayBlock interacted");
        }
        

        interactable.isInteractableActive = false;
        if (player != null)
            player.currentInteractable = null;

        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

}
