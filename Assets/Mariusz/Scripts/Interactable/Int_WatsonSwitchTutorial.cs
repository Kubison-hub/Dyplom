using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

[RequireComponent(typeof(Interactable))]
public class Int_WatsonSwitchTutorial : MonoBehaviour
{
    private static bool tutorialCompleted;
    private static bool tutorialInProgress;

    [Header("Tutorial Trigger")]
    [SerializeField] private bool startsSwitchTutorial = true;
    [SerializeField] private bool representsWatson = true;
    [SerializeField, Min(0)] private int sherlockPlayerIndex = 0;
    [SerializeField, Min(0)] private int watsonPlayerIndex = 1;

    [Header("Opening Dialogue")]
    [SerializeField, TextArea] private string sherlockOpeningText =
        "Watsonie, czas spojrzeć na tę sprawę z innej perspektywy.";
    [SerializeField, TextArea] private string watsonOpeningText =
        "W rzeczy samej, Sherlocku. Jestem do usług.";

    [Header("Tutorial Popup")]
    [SerializeField] private string tutorialTitle = "Watson";
    [SerializeField, TextArea] private string tutorialText =
        "W grze możesz sterować także Watsonem. Wciśnij spację, aby przełączać się pomiędzy postaciami.";
    [SerializeField] private VideoClip tutorialVideoClip;

    [Header("After Switching")]
    [SerializeField, TextArea] private string sherlockAfterSwitchText =
        "Spróbuj wyciągnąć jakieś informacje, Watsonie.";
    [SerializeField, TextArea] private string watsonAfterSwitchText =
        "W rzeczy samej, Sherlock.";
    [SerializeField, TextArea] private string watsonRepeatText = "Sherlock?";
    [SerializeField, TextArea] private string sherlockRepeatText = "Watson?";

    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_WatsonSwitchTutorial);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (tutorialCompleted)
        {
            ShowRepeatLine();
            ClearCurrentInteraction(player);
            return;
        }

        if (!startsSwitchTutorial || tutorialInProgress)
        {
            ClearCurrentInteraction(player);
            return;
        }

        StartCoroutine(RunTutorial(player));
    }

    private IEnumerator RunTutorial(PlayerController player)
    {
        tutorialInProgress = true;
        ClearCurrentInteraction(player);

        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null)
            switchCharacter.canSwitch = false;

        PlayerTopText topText = PlayerTopText.Instance;
        if (topText != null)
        {
            topText.ShowTopText(sherlockOpeningText, watsonOpeningText);
            yield return new WaitForSeconds(topText.textTime * 2f);
        }

        TutorialTimeline timeline = TutorialTimeline.Instance;
        topText?.ShowTopTextPersistent("", "");
        if (timeline != null)
        {
            timeline.ShowGameplayTutorialPopup(tutorialTitle, tutorialText, tutorialVideoClip);
            yield return null;

            while (timeline.BlocksWorldInput)
                yield return null;
        }

        if (switchCharacter == null)
        {
            Debug.LogWarning($"{name}: SwitchCharacter is missing; Watson tutorial cannot enable switching.", this);
            tutorialInProgress = false;
            yield break;
        }

        switchCharacter.canSwitch = true;

        while (switchCharacter.activePlayerIndex != watsonPlayerIndex)
            yield return null;

        switchCharacter.canSwitch = false;

        if (PlayerTopText.Instance != null)
        {
            PlayerTopText.Instance.ShowTopText(sherlockAfterSwitchText, watsonAfterSwitchText);
            yield return new WaitForSeconds(PlayerTopText.Instance.textTime * 2f);
        }

        tutorialCompleted = true;
        tutorialInProgress = false;
        switchCharacter.canSwitch = true;
    }

    private void ShowRepeatLine()
    {
        if (PlayerTopText.Instance == null || SwitchCharacter.Instance == null)
            return;

        if (representsWatson && SwitchCharacter.Instance.activePlayerIndex == sherlockPlayerIndex)
            PlayerTopText.Instance.ShowTopText("", watsonRepeatText);
        else if (!representsWatson && SwitchCharacter.Instance.activePlayerIndex == watsonPlayerIndex)
            PlayerTopText.Instance.ShowTopText(sherlockRepeatText, "");
    }

    private static void ClearCurrentInteraction(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;
    }
}
