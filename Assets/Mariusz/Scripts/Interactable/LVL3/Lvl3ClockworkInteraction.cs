using UnityEngine;

public abstract class Lvl3ClockworkInteraction : Lvl3InteractionDialogueBase
{
    [Header("Playable Characters")]
    [SerializeField] private bool allowSherlock = true;
    [SerializeField] private bool allowWatson = true;

    protected Interactable Interactable { get; private set; }

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    protected virtual void Awake()
    {
        SetupInteractable();
    }

    protected virtual void OnValidate()
    {
        SetupInteractable();
    }

    public virtual bool CanPlayerUse(PlayerController player)
    {
        if (player == null)
            return false;

        if (player.playerCharacter == PlayerCharacter.Sherlock || player.CompareTag("PlayerA"))
            return allowSherlock;

        if (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"))
            return allowWatson;

        return SwitchCharacter.Instance != null &&
               (SwitchCharacter.Instance.activePlayerIndex == 0 ? allowSherlock : allowWatson);
    }

    public abstract void PerformInteraction(PlayerController player);

    protected void ClearPlayerInteraction(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;
    }

    protected void ShowTopText(string title, string description = "")
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(title, description);
        else
            Debug.Log($"{title} {description}");
    }

    protected void ShowTopTextForPlayer(PlayerController player, string text)
    {
        if (PlayerTopText.Instance == null)
        {
            Debug.Log(text);
            return;
        }

        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        if (isWatson)
            PlayerTopText.Instance.ShowWatsonTopText(text);
        else
            PlayerTopText.Instance.ShowTopText(text, string.Empty);
    }

    protected void SetupInteractable()
    {
        lvl3_LayerUtility.SetOutlinedObjectsLayer(gameObject);
        Interactable = GetComponent<Interactable>();
    }
}
