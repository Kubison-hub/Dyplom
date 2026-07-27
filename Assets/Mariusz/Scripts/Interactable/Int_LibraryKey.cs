using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_LibraryKey : MonoBehaviour
{
    private Interactable interactable;

    [SerializeField] private Int_LibrarySafe librarySafe;
    [SerializeField] private Renderer keyRenderer;
    [SerializeField] private AudioSource pickupAudio;

    public bool performed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;

        if (keyRenderer != null)
            keyRenderer.enabled = false;

        if (pickupAudio != null)
            pickupAudio.Play();

        if (librarySafe != null)
            librarySafe.SetKeyFound(true);

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText("I jest kluczyk, jakie to proste...", "");

        interactable.isInteractableActive = false;
        interactable.interactiveShader = null;

        if (player != null)
            player.currentInteractable = null;
    }
}
