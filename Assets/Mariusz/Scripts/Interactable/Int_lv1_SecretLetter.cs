using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SecretLetter : MonoBehaviour
{
    [SerializeField, TextArea] private string topText = "1A4 - to warto zapamiętać";

    private Interactable interactable;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_lv1_SecretLetter);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");

        if (player != null)
            player.currentInteractable = null;
    }
}
