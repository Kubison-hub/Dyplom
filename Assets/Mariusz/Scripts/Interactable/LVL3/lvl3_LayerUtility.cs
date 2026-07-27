using UnityEngine;

public static class lvl3_LayerUtility
{
    private const string OutlinedObjectsLayerName = "Outlined Objects";

    public static void SetOutlinedObjectsLayer(GameObject target)
    {
        if (target == null)
            return;

        int layer = LayerMask.NameToLayer(OutlinedObjectsLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"Layer '{OutlinedObjectsLayerName}' does not exist.");
            return;
        }

        target.layer = layer;
    }
}
