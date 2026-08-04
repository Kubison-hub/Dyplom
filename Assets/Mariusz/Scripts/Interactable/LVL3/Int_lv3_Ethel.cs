using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_Ethel : MonoBehaviour
{
    private Interactable interactable;

    private void Reset()
    {
        SetupInteractable();
    }

    private void OnValidate()
    {
        SetupInteractable();
    }

    private void Awake()
    {
        SetupInteractable();
    }

    public void PerformInteraction(PlayerController player)
    {
        Debug.Log("ETHEL INTERACTION");
    }

    private void SetupInteractable()
    {
        lvl3_LayerUtility.SetOutlinedObjectsLayer(gameObject);

        interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.Int_lv3_Ethel);
        }
    }
}
