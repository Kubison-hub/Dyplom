using UnityEngine;

public class Int_HidenWall : MonoBehaviour
{
    [Header("Discovery")]
    [SerializeField] private Collider loupeCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.1f)] private float loupeHoldDuration = 1.5f;

    [Header("Result")]
    [SerializeField, TextArea] private string topText =
        "Ta sciana skrywa cos wiecej niz tylko stare cegly.";
    [SerializeField] private Transform cardPosition;
    [SerializeField] private DetectiveIdeaPoint ideaPoint;
    [SerializeField] private GameObject[] nextInteractions;

    private Interactable interactable;
    private float loupeHoldStartedAt = -1f;

    public bool performed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (loupeCollider == null)
            loupeCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (ideaPoint != null)
            ideaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;

        if (interactable != null)
            interactable.isInteractableActive = false;

        HiddenWallTracePuzzle tracePuzzle = GetComponent<HiddenWallTracePuzzle>();
        if (tracePuzzle != null)
            tracePuzzle.enabled = false;
    }

    private void Update()
    {
        if (performed || magnifier == null || loupeCollider == null)
            return;

        bool isScanningWall = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                              IsLoupeHitOnWall(hit.collider);

        if (!isScanningWall)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        if (loupeHoldStartedAt < 0f)
            loupeHoldStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoldStartedAt >= loupeHoldDuration)
            ResolveLoupeDiscovery();
    }

    public void PerformInteraction(PlayerController player)
    {
        ResolveLoupeDiscovery();

        if (player != null)
            player.currentInteractable = null;
    }

    public void ResolveTracePuzzle()
    {
        ResolveLoupeDiscovery();
    }

    private bool IsLoupeHitOnWall(Collider hitCollider)
    {
        return hitCollider == loupeCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeCollider.transform);
    }

    private void ResolveLoupeDiscovery()
    {
        if (performed)
            return;

        performed = true;
        loupeHoldStartedAt = -1f;

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");

        if (interactable != null)
        {
            if (interactable.clues != null && interactable.clues.Length > 0)
                interactable.AddClue(0, cardPosition);

            interactable.isInteractableActive = false;
        }

        ideaPoint?.RevealFromExternalSource();
        ActivateNextInteractions();

        if (loupeCollider != null)
            loupeCollider.enabled = false;
    }

    private void ActivateNextInteractions()
    {
        foreach (GameObject nextInteraction in nextInteractions)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }
    }
}
