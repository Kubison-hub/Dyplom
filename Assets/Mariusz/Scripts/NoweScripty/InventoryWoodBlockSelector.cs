using UnityEngine;

/// <summary>
/// Attach to an inventory UI button and hook SelectBlock into Button.OnClick.
/// </summary>
public class InventoryWoodBlockSelector : MonoBehaviour
{
    [SerializeField] private ItemType woodBlock = ItemType.WoodBlockLevel1;

    public void SelectBlock()
    {
        InventoryManager.Instance?.TrySelectWoodBlock(woodBlock);
    }
}
