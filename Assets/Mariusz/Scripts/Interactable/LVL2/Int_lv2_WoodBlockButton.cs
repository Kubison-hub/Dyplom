using UnityEngine;

public class Int_lv2_WoodBlockButton : WoodBlockButtonPickupBase
{
    protected override InteractionType AssignedInteractionType => InteractionType.Int_lv2_WoodBlockButton;
    protected override ItemType CollectedItemType => ItemType.WoodBlockLevel2;
}
