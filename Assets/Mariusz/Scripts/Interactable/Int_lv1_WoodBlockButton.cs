using UnityEngine;

public class Int_lv1_WoodBlockButton : WoodBlockButtonPickupBase
{
    protected override InteractionType AssignedInteractionType => InteractionType.Int_lv1_WoodBlockButton;
    protected override ItemType CollectedItemType => ItemType.WoodBlockLevel1;
}
