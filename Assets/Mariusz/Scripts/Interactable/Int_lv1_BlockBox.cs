using UnityEngine;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class Int_lv1_BlockBox : MonoBehaviour
{
    [SerializeField, TextArea] private string[] blockedAreaTexts =
    {
        "Na razie tam nie wracam.",
        "Teraz mam szanse udac sie na gore.",
        "Piwnica poczeka. Mala Ethel niekoniecznie."
    };

    private Interactable interactable;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_lv1_BlockBox);

        int interactableLayer = LayerMask.NameToLayer("Outlined Objects");
        if (interactableLayer >= 0)
            gameObject.layer = interactableLayer;

        Collider blockerCollider = GetComponent<Collider>();
        blockerCollider.isTrigger = false;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player != null && player.navMeshAgent != null && player.navMeshAgent.isOnNavMesh)
            player.navMeshAgent.ResetPath();

        if (blockedAreaTexts != null && blockedAreaTexts.Length > 0 && PlayerTopText.Instance != null)
        {
            string text = blockedAreaTexts[Random.Range(0, blockedAreaTexts.Length)];
            PlayerTopText.Instance.ShowTopText(text, "");
        }

        if (player != null)
            player.currentInteractable = null;
    }
}
