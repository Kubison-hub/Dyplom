using System;
using System.Collections;
using UnityEngine;

[Serializable]
public struct Lvl3DialogueLine
{
    public Lvl3DialogueSpeaker speaker;
    [TextArea] public string text;
    public AudioClip voiceClip;
    [Min(0.1f)] public float duration;
}

public enum Lvl3DialogueSpeaker
{
    Sherlock,
    Watson,
    Selma,
    Violet
}

public abstract class Lvl3InteractionDialogueBase : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private bool repeatable = true;
    [SerializeField] private Lvl3DialogueLine[] dialogueLines;

    [Header("Dialogue Audio Overrides")]
    [Tooltip("Optional per-interaction overrides. Leave empty to use DialogueAudioRegistry.")]
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioSource selmaVoiceSource;
    [SerializeField] private AudioSource violetVoiceSource;

    private bool performed;
    private Coroutine dialogueCoroutine;
    private static Lvl3InteractionDialogueBase activeDialogueOwner;
    private string activeSherlockText;
    private string activeWatsonText;
    private string activeSelmaText;
    private string activeVioletText;

    protected abstract Lvl3DialogueLine[] DefaultDialogueLines { get; }
    protected virtual void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines) { }

    protected Lvl3DialogueLine[] ConfiguredDialogueLines =>
        dialogueLines != null && dialogueLines.Length > 0
            ? dialogueLines
            : DefaultDialogueLines;

    protected void SetupInteractable(InteractionType interactionType)
    {
        lvl3_LayerUtility.SetOutlinedObjectsLayer(gameObject);

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(interactionType);

        EnsureDefaultDialogueLines();
    }

    protected void PlayInteractionDialogue(PlayerController player)
    {
        if (performed && !repeatable)
            return;

        performed = true;

        PlayDialogue(player, ConfiguredDialogueLines);
    }

    protected void PlayDialogue(PlayerController player, Lvl3DialogueLine[] lines)
    {
        if (activeDialogueOwner != null && activeDialogueOwner != this)
            activeDialogueOwner.StopActiveDialogue();
        else
            StopActiveDialogue();

        activeDialogueOwner = this;
        dialogueCoroutine = StartCoroutine(PlayDialogueSequence(lines));

        if (player != null)
            player.currentInteractable = null;
    }

    protected void PlayCompletionDialogue()
    {
        PlayDialogue(null, dialogueLines);
    }

    private void OnDisable()
    {
        // A dialogue coroutine stops with its GameObject. Clear the text explicitly
        // so a picked-up or removed world object cannot leave a stuck Top Text behind.
        if (activeDialogueOwner == this)
            StopActiveDialogue();
    }

    private IEnumerator PlayDialogueSequence(Lvl3DialogueLine[] lines)
    {
        if (lines == null)
        {
            dialogueCoroutine = null;
            if (activeDialogueOwner == this)
                activeDialogueOwner = null;
            yield break;
        }

        foreach (Lvl3DialogueLine line in lines)
        {
            // Audio-only lines are valid: they still need to play and wait for their duration.

            string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
            string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
            string selmaText = line.speaker == Lvl3DialogueSpeaker.Selma ? line.text : string.Empty;
            string violetText = line.speaker == Lvl3DialogueSpeaker.Violet ? line.text : string.Empty;
            activeSherlockText = sherlockText;
            activeWatsonText = watsonText;
            activeSelmaText = selmaText;
            activeVioletText = violetText;

            if (PlayerTopText.Instance != null)
            {
                PlayerTopText.Instance.ShowTopTextPersistent(sherlockText, watsonText);
                PlayerTopText.Instance.ShowSelmaTopTextPersistent(selmaText);
                PlayerTopText.Instance.ShowVioletTopTextPersistent(violetText);
            }

            PlayVoice(line);

            float duration = line.duration > 0f
                ? line.duration
                : PlayerTopText.Instance != null ? PlayerTopText.Instance.textTime : 3f;

            yield return new WaitForSeconds(duration);

            PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
            PlayerTopText.Instance?.ClearSelmaTopTextIfMatches(selmaText);
            PlayerTopText.Instance?.ClearVioletTopTextIfMatches(violetText);
            ClearActiveTextCache();
        }

        dialogueCoroutine = null;

        if (activeDialogueOwner == this)
            activeDialogueOwner = null;

        OnDialogueSequenceCompleted(lines);
    }

    private void StopActiveDialogue()
    {
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = null;
        GetVoiceSource(Lvl3DialogueSpeaker.Sherlock)?.Stop();
        GetVoiceSource(Lvl3DialogueSpeaker.Watson)?.Stop();
        GetVoiceSource(Lvl3DialogueSpeaker.Selma)?.Stop();
        GetVoiceSource(Lvl3DialogueSpeaker.Violet)?.Stop();
        PlayerTopText.Instance?.ClearTopTextIfMatches(activeSherlockText, activeWatsonText);
        PlayerTopText.Instance?.ClearSelmaTopTextIfMatches(activeSelmaText);
        PlayerTopText.Instance?.ClearVioletTopTextIfMatches(activeVioletText);
        ClearActiveTextCache();

        if (activeDialogueOwner == this)
            activeDialogueOwner = null;
    }

    private void ClearActiveTextCache()
    {
        activeSherlockText = string.Empty;
        activeWatsonText = string.Empty;
        activeSelmaText = string.Empty;
        activeVioletText = string.Empty;
    }

    private void PlayVoice(Lvl3DialogueLine line)
    {
        if (line.voiceClip == null)
            return;

        AudioSource source = GetVoiceSource(line.speaker);

        // Standalone test scenes, such as Level_2_DEV, may not contain the
        // scene-wide dialogue registry. Use the interaction's own source then.
        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null)
            return;

        source.Stop();
        source.PlayOneShot(line.voiceClip);
    }

    private AudioSource GetVoiceSource(Lvl3DialogueSpeaker speaker)
    {
        AudioSource localSource = speaker switch
        {
            Lvl3DialogueSpeaker.Sherlock => sherlockVoiceSource,
            Lvl3DialogueSpeaker.Watson => watsonVoiceSource,
            Lvl3DialogueSpeaker.Selma => selmaVoiceSource,
            Lvl3DialogueSpeaker.Violet => violetVoiceSource,
            _ => null
        };

        if (localSource != null)
            return localSource;

        DialogueAudioRegistry registry = DialogueAudioRegistry.Instance;
        return speaker switch
        {
            Lvl3DialogueSpeaker.Sherlock => registry != null ? registry.SherlockVoiceSource : null,
            Lvl3DialogueSpeaker.Watson => registry != null ? registry.WatsonVoiceSource : null,
            Lvl3DialogueSpeaker.Selma => registry != null ? registry.SelmaVoiceSource : null,
            Lvl3DialogueSpeaker.Violet => registry != null ? registry.VioletVoiceSource : null,
            _ => null
        };
    }

    private void EnsureDefaultDialogueLines()
    {
        if (dialogueLines != null && dialogueLines.Length > 0)
            return;

        dialogueLines = DefaultDialogueLines;
    }
}
