using UnityEngine;

public class HighlightManager : MonoBehaviour
{
    public KeyCode highlightKey = KeyCode.V;
    private PickupableObject[] pickupObjects;

    private void Start()
    {
        pickupObjects = FindObjectsOfType<PickupableObject>();
    }

    private void Update()
    {
        bool highlightOn = Input.GetKey(highlightKey);

        foreach (var obj in pickupObjects)
        {
            obj.Highlight(highlightOn);
        }
    }
}
