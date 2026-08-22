using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SherlockWatsonSelmaDialog : Lvl3InteractionDialogueBase
{
    [Header("Opening Dialogue")]
    [SerializeField]
    private Lvl3DialogueLine[] openingDialogueLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Musz� poszuka� ma�ej Ethel na g�rze Watsonie.",
            duration = 3f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Pomog� Ci w tym Sherlock.",
            duration = 3f
        }
    };

    [SerializeField] private GameObject questionMarkToHide;

    [Header("Immediate Watson Reaction")]
    [SerializeField, Min(1f)] private float watsonTurnSpeed = 360f;

    private Interactable interactable;
    private Collider[] interactionColliders;
    private Coroutine watsonTurnCoroutine;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => openingDialogueLines;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactionColliders = GetComponents<Collider>();
        SetupInteractable(InteractionType.Int_lv1_SherlockWatsonSelmaDialog);
    }

    public void PerformInteraction(PlayerController player)
    {
        questionMarkToHide?.SetActive(false);
        DisableInteractionVisuals();
        PlayDialogue(player, openingDialogueLines);
    }

    public void NotifyInteractionSelected(PlayerController player)
    {
        if (player == null || player.playerCharacter != PlayerCharacter.Sherlock)
            return;

        PlayerController watson = FindWatson();
        if (watson == null)
            return;

        if (watsonTurnCoroutine != null)
            StopCoroutine(watsonTurnCoroutine);

        watsonTurnCoroutine = StartCoroutine(RotateWatsonToward(player.transform, watson));
    }

    private IEnumerator RotateWatsonToward(Transform sherlock, PlayerController watson)
    {
        if (sherlock == null || watson == null)
            yield break;

        bool restoreAgentRotation = watson.navMeshAgent != null && watson.navMeshAgent.updateRotation;
        if (watson.navMeshAgent != null)
            watson.navMeshAgent.updateRotation = false;

        while (sherlock != null && watson != null)
        {
            Vector3 direction = sherlock.position - watson.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                break;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            watson.transform.rotation = Quaternion.RotateTowards(
                watson.transform.rotation,
                targetRotation,
                watsonTurnSpeed * Time.deltaTime);

            if (Quaternion.Angle(watson.transform.rotation, targetRotation) <= 0.5f)
                break;

            yield return null;
        }

        if (watson != null && watson.navMeshAgent != null)
            watson.navMeshAgent.updateRotation = restoreAgentRotation;

        watsonTurnCoroutine = null;
    }

    private static PlayerController FindWatson()
    {
        foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.playerCharacter == PlayerCharacter.Watson)
                return candidate;
        }

        return null;
    }

    private void DisableInteractionVisuals()
    {
        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (interactionColliders == null)
            return;

        foreach (Collider interactionCollider in interactionColliders)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }
    }
}

