using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_HeavyBox : Lvl3InteractionDialogueBase
{
    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ciężka drewniana skrzynia.",
            duration = 2.5f
        }
    };

    private void Reset() => SetupHeavyBox();
    private void OnValidate() => SetupHeavyBox();
    private void Awake() => SetupHeavyBox();

    public void PerformInteraction(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        if (isWatson)
        {
            PlayerTopText.Instance?.ShowWatsonTopText(DefaultDialogueLines[0].text);
            player.currentInteractable = null;
            return;
        }

        PlayInteractionDialogue(player);
    }

    private void SetupHeavyBox()
    {
        SetupInteractable(InteractionType.Int_lv3_HeavyBox);

        int outlinedObjectsLayer = LayerMask.NameToLayer("Outlined Objects");
        if (outlinedObjectsLayer < 0)
            return;

        // The box model is stored in prefab children. The VisionEye renderer needs
        // the layer on the renderer GameObjects, not only on the interaction root.
        foreach (Renderer boxRenderer in GetComponentsInChildren<Renderer>(true))
            boxRenderer.gameObject.layer = outlinedObjectsLayer;
    }
}
