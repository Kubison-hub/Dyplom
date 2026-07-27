using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_EthelPassageDoor : MonoBehaviour
{
    [SerializeField, TextArea] private string firstInteractionText =
        "Tutaj zamyka sie ten rozdzial, Watsonie. Musze isc na gore poszukac Malej Ethel. Czuje, ze ona jest kluczem do zagadki.";

    [SerializeField, TextArea] private string repeatedInteractionText =
        "Tedy nie przejde. Musze poszukac Malej Ethel na gorze.";

    private Interactable interactable;
    private bool hasBeenUsed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_lv1_EthelPassageDoor);
    }

    public void PerformInteraction(PlayerController player)
    {
        string text = hasBeenUsed ? repeatedInteractionText : firstInteractionText;
        hasBeenUsed = true;

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(text, "");

        if (player != null)
            player.currentInteractable = null;
    }
}
