using UnityEngine;
using UnityEngine.Splines;

public class Int_NiebieskaFigurka : Lvl3InteractionDialogueBase
{
    public ItemType itemType;
    public GameObject spline;
    private bool collected;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Pickup Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] pickupDialogue =
    {
        new Lvl3DialogueLine { speaker = Lvl3DialogueSpeaker.Sherlock, text = "Hmm... Niebieska figurka.", duration = 2f }
    };
    [SerializeField] private Lvl3DialogueLine[] noSpaceDialogue =
    {
        new Lvl3DialogueLine { speaker = Lvl3DialogueSpeaker.Sherlock, text = "Nie mam miejsca w ekwipunku.", duration = 2f }
    };

    private bool destroyAfterDialogue;
    protected override Lvl3DialogueLine[] DefaultDialogueLines => pickupDialogue;

    public void PerformInteraction(PlayerController player)
    {
        if (collected)
            return;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddItem(itemType, inventoryIcon))
        {
            PlayDialogue(player, noSpaceDialogue);
            return;
        }

        collected = true;
        if (spline != null)
            spline.SetActive(false);

        HideCollectedFigure();
        destroyAfterDialogue = true;
        PlayDialogue(player, pickupDialogue);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (destroyAfterDialogue && lines == pickupDialogue)
            Destroy(gameObject);
    }

    private void HideCollectedFigure()
    {
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
            interactable.interactiveShader = null;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;

        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }
}
