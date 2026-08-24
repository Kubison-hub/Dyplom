using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_HidenPassageButton : MonoBehaviour
{
    [Header("Secret Door")]
    [SerializeField] private Animator secretDoorAnimator;
    [SerializeField] private string openedBoolName = "Opened";
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";
    [SerializeField] private bool isDoorOpen;

    [Header("Button Animation")]
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressTriggerName = "Press";

    [Header("First Use Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] firstUseDialogueLines;
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioSource selmaVoiceSource;

    private bool firstUseCompleted;
    private Coroutine dialogueCoroutine;

    private void Awake()
    {
        GetComponent<Interactable>()?.SetInteractionType(InteractionType.Int_lv1_HidenPassageButton);
    }

    private void Start()
    {
        if (secretDoorAnimator != null && HasAnimatorBool(openedBoolName))
            isDoorOpen = secretDoorAnimator.GetBool(openedBoolName);
    }

    public void PerformInteraction(PlayerController player)
    {
        PlayButtonPressAnimation();
        ToggleSecretDoor();

        if (!firstUseCompleted)
        {
            firstUseCompleted = true;
            if (dialogueCoroutine != null)
                StopCoroutine(dialogueCoroutine);

            dialogueCoroutine = StartCoroutine(PlayDialogueLines(firstUseDialogueLines));
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private void ToggleSecretDoor()
    {
        if (secretDoorAnimator == null)
        {
            Debug.LogWarning($"{name}: Secret Door Animator is not assigned.", this);
            return;
        }

        isDoorOpen = !isDoorOpen;
        if (HasAnimatorBool(openedBoolName))
            secretDoorAnimator.SetBool(openedBoolName, isDoorOpen);
        else
            Debug.LogWarning($"{name}: Animator has no bool parameter named '{openedBoolName}'.", this);

        string triggerName = isDoorOpen ? openTriggerName : closeTriggerName;
        if (HasAnimatorTrigger(triggerName))
            secretDoorAnimator.SetTrigger(triggerName);
        else
            Debug.LogWarning($"{name}: Animator has no trigger parameter named '{triggerName}'.", this);
    }

    private void PlayButtonPressAnimation()
    {
        if (buttonAnimator == null || string.IsNullOrWhiteSpace(pressTriggerName))
            return;

        buttonAnimator.SetTrigger(pressTriggerName);
    }

    private bool HasAnimatorBool(string parameterName)
    {
        if (secretDoorAnimator == null || secretDoorAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in secretDoorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private bool HasAnimatorTrigger(string parameterName)
    {
        if (secretDoorAnimator == null || secretDoorAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in secretDoorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private IEnumerator PlayDialogueLines(Lvl3DialogueLine[] lines)
    {
        if (lines == null)
        {
            dialogueCoroutine = null;
            yield break;
        }

        foreach (Lvl3DialogueLine line in lines)
        {
            string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
            string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
            string selmaText = line.speaker == Lvl3DialogueSpeaker.Selma ? line.text : string.Empty;

            PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);
            PlayerTopText.Instance?.ShowSelmaTopTextPersistent(selmaText);
            PlayVoice(line);

            float duration = line.duration > 0f
                ? line.duration
                : PlayerTopText.Instance != null ? PlayerTopText.Instance.textTime : 3f;
            yield return new WaitForSeconds(duration);

            PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
            PlayerTopText.Instance?.ClearSelmaTopTextIfMatches(selmaText);
        }

        dialogueCoroutine = null;
    }

    private void PlayVoice(Lvl3DialogueLine line)
    {
        if (line.voiceClip == null)
            return;

        AudioSource source = line.speaker switch
        {
            Lvl3DialogueSpeaker.Sherlock => sherlockVoiceSource,
            Lvl3DialogueSpeaker.Watson => watsonVoiceSource,
            Lvl3DialogueSpeaker.Selma => selmaVoiceSource,
            _ => null
        };

        if (source == null)
            return;

        source.Stop();
        source.PlayOneShot(line.voiceClip);
    }
}
