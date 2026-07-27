using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_BigGramm : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip windingClip;

    [Header("Narration")]
    [SerializeField, TextArea] private string firstInteractionText =
        "Tuba tego jakze wielkiego patefonu skierowana jest ku scianie. Chyba moge go wlaczyc.";
    [SerializeField, TextArea] private string windingText =
        "Nastawilem patefon na ostatnia plyte.";
    [SerializeField, TextArea] private string readyInteractionText =
        "Nastawilem patefon. Brakuje mi tu wlacznika.";

    private Interactable interactable;
    private int interactionCount;

    public bool IsReady { get; private set; }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_lv1_BigGramm);
    }

    public void PerformInteraction(PlayerController player)
    {
        interactionCount++;

        if (interactionCount == 1)
        {
            ShowTopText(firstInteractionText);
        }
        else if (interactionCount == 2)
        {
            if (audioSource != null && windingClip != null)
                audioSource.PlayOneShot(windingClip);

            IsReady = true;
            ShowTopText(windingText);
        }
        else
        {
            ShowTopText(readyInteractionText);
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private static void ShowTopText(string text)
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(text, "");
    }
}
