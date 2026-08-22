using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_WindowBulletCP : Lvl3InteractionDialogueBase
{
    [SerializeField] private Int_lv1_WindowBullet windowExamination;
    [SerializeField] private AudioSource foundAudio;
    [Header("Bullet Found Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] bulletFoundDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Doskonale.",
            duration = 2f
        }
    };

    private Interactable interactable;
    private Collider interactionCollider;
    private Renderer[] objectRenderers;
    private bool performed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => bulletFoundDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        objectRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        foundAudio?.Play();
        windowExamination?.RegisterWindowClue();
        PlayDialogue(player, bulletFoundDialogue);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;

        foreach (Renderer objectRenderer in objectRenderers)
        {
            if (objectRenderer != null)
                objectRenderer.enabled = false;
        }

    }
}
