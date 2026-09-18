using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ClockSecretPassage : Lvl3ClockworkInteraction
{
    [SerializeField] private Lvl3ClockworkPuzzleController puzzleController;
    [SerializeField] private Animator passageAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string openedBool = "Opened";
    [SerializeField] private AudioSource openAudioSource;
    [SerializeField] private Collider[] collidersToDisable;
    [Tooltip("Room content activated immediately before the secret passage starts opening.")]
    [SerializeField] private GameObject roomToActivate;
    [Tooltip("Objects to disable once the real passage opens, for example the temporary vision-passage GameObject.")]
    [SerializeField] private GameObject[] gameObjectsToDisableOnOpen;

    [Header("Opening Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] openingDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Udało się.",
            duration = 2f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Przejście stoi otworem.",
            duration = 2f
        }
    };

    [Header("Blackboard Reveal")]
    [Tooltip("Temporary blackboard hiding the room behind this passage.")]
    [SerializeField] private GameObject blackBoardToDisableOnOpen;
    [SerializeField] private Material blackBoardFadeMaterial;
    [Tooltip("Delay after triggering the Open animation before the blackboard begins to fade.")]
    [SerializeField, Min(0f)] private float blackBoardFadeDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float blackBoardFadeDuration = 1f;

    private bool opened;

    public bool IsOpened => opened;
    protected override Lvl3DialogueLine[] DefaultDialogueLines => openingDialogue;

    protected override void Awake()
    {
        base.Awake();

        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponents<Collider>();
    }

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        if (opened)
            return;

        if (puzzleController == null || !puzzleController.IsSecretOpeningAllowed)
        {
            ShowTopText("Mechanizm w murze stawia opór.", "Potrzebuje właściwego momentu.");
            return;
        }

        opened = true;
        Interactable?.MarkCompleted();
        CluesLog.Instance?.RemoveFindBasementHiddenDoorObjective();
        openAudioSource?.Play();

        if (roomToActivate != null)
            roomToActivate.SetActive(true);

        if (passageAnimator != null)
        {
            passageAnimator.SetTrigger(openTrigger);
            passageAnimator.SetBool(openedBool, true);
        }

        StartCoroutine(FadeBlackBoardAfterDoorOpen());

        foreach (Collider wallCollider in collidersToDisable)
        {
            if (wallCollider != null)
                wallCollider.enabled = false;
        }

        if (Interactable != null)
            Interactable.isInteractableActive = false;

        DisableObjectsOnOpen();

        puzzleController.CompletePuzzle();
        PlayInteractionDialogue(player);
    }

    public void OpenFromPuzzle(PlayerController player)
    {
        if (opened)
            return;

        opened = true;
        Interactable?.MarkCompleted();
        CluesLog.Instance?.RemoveFindBasementHiddenDoorObjective();
        openAudioSource?.Play();

        if (roomToActivate != null)
            roomToActivate.SetActive(true);

        if (passageAnimator != null)
        {
            passageAnimator.SetTrigger(openTrigger);
            passageAnimator.SetBool(openedBool, true);
        }

        StartCoroutine(FadeBlackBoardAfterDoorOpen());

        foreach (Collider wallCollider in collidersToDisable)
        {
            if (wallCollider != null)
                wallCollider.enabled = false;
        }

        if (Interactable != null)
            Interactable.isInteractableActive = false;

        DisableObjectsOnOpen();
        PlayInteractionDialogue(player);
    }

    private void DisableObjectsOnOpen()
    {
        if (gameObjectsToDisableOnOpen == null)
            return;

        foreach (GameObject target in gameObjectsToDisableOnOpen)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    private IEnumerator FadeBlackBoardAfterDoorOpen()
    {
        if (blackBoardFadeDelay > 0f)
            yield return new WaitForSeconds(blackBoardFadeDelay);

        FadeBlackBoard();
    }

    private void FadeBlackBoard()
    {
        if (blackBoardToDisableOnOpen == null)
            return;

        Renderer blackBoardRenderer = blackBoardToDisableOnOpen.GetComponent<Renderer>();
        if (blackBoardRenderer == null)
        {
            blackBoardToDisableOnOpen.SetActive(false);
            return;
        }

        if (blackBoardFadeMaterial != null)
            blackBoardRenderer.material = blackBoardFadeMaterial;

        StartCoroutine(FadeBlackBoardAlpha(blackBoardRenderer.material));
    }

    private IEnumerator FadeBlackBoardAlpha(Material material)
    {
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (!material.HasProperty(colorProperty))
        {
            blackBoardToDisableOnOpen.SetActive(false);
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < blackBoardFadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / blackBoardFadeDuration);
            material.SetColor(colorProperty, color);
            yield return null;
        }

        color.a = 0f;
        material.SetColor(colorProperty, color);
        blackBoardToDisableOnOpen.SetActive(false);
    }
}
