using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ManequineExamCP : Lvl3InteractionDialogueBase
{
    [SerializeField] private Int_lv3_ManequineExam mannequinExam;
    [Tooltip("Leave empty to automatically use every collider on this object and its children.")]
    [SerializeField] private Collider[] clueColliders;

    private bool discovered;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ten detal należy do mechanizmu manekina.",
            duration = 2.5f
        }
    };

    private void Reset() => SetupInteractable(InteractionType.Int_lv3_ManequineExamCP);
    private void OnValidate() => SetupInteractable(InteractionType.Int_lv3_ManequineExamCP);
    private void Awake()
    {
        SetupInteractable(InteractionType.Int_lv3_ManequineExamCP);
        CacheChildColliders();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (discovered)
            return;

        discovered = true;
        PlayInteractionDialogue(player);
        mannequinExam?.RegisterClue();

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.SetQuestionFXEagleVisionState(false);
        }

        foreach (Collider clueCollider in clueColliders)
        {
            if (clueCollider != null)
                clueCollider.enabled = false;
        }
    }

    private void CacheChildColliders()
    {
        if (clueColliders == null || clueColliders.Length == 0)
            clueColliders = GetComponentsInChildren<Collider>(true);
    }
}
