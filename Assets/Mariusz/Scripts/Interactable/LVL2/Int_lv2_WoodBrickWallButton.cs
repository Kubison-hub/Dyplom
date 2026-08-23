using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv2_WoodBrickWallButton : MonoBehaviour
{
    [SerializeField] private Int_lv2_WoodBrickWall ownerWall;

    private void Awake()
    {
        SetupInteractable();
    }

    private void Reset() => SetupInteractable();
    private void OnValidate() => SetupInteractable();

    public void PerformInteraction(PlayerController player)
    {
        if (ownerWall == null)
            ownerWall = GetComponentInParent<Int_lv2_WoodBrickWall>();

        ownerWall?.UseMountedBlock(player);
    }

    private void SetupInteractable()
    {
        if (ownerWall == null)
            ownerWall = GetComponentInParent<Int_lv2_WoodBrickWall>();

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_lv2_WoodBrickWallButton);
    }
}
