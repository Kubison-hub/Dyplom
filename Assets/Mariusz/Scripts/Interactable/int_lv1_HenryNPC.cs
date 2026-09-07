using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;
using System;
using DialogueEditor;

public class int_lv1_HenryNPC : Int_lv1_NpcDialogBase
{
    [Header("Dialogue Rig")]
    [SerializeField] private Rig dialogueRig;
    [SerializeField, Min(0.01f)] private float rigBlendDuration = 0.25f;

    [Header("Repeat Dialogue")]
    [SerializeField] private SmartNPC henrySmartNpc;
    [SerializeField] private NPCConversation repeatConversationForSherlock;
    [SerializeField] private NPCConversation repeatConversationForWatson;

    [Header("Selma Dialogue Unlock")]
    [SerializeField] private SmartNPC selmaSmartNpc;
    [SerializeField] private string selmaHenryParameterName = "Henry";

    private Coroutine rigBlendCoroutine;

    protected override InteractionType RequiredInteractionType => InteractionType.int_lv1_HenryNPC;
    protected override bool ShouldFacePlayerBeforeDialogue => false;
    protected override bool ShouldFaceInteractingPlayerTowardNpcBeforeDialogue => true;

    private void Awake()
    {
        // Keeps the scene setup simple while still allowing an explicit rig assignment.
        if (dialogueRig == null)
            dialogueRig = GetComponentInChildren<Rig>(true);

        SetRigWeightImmediate(0f);
    }

    protected override void Start()
    {
        base.Start();

        if (henrySmartNpc == null)
            henrySmartNpc = GetComponentInChildren<SmartNPC>(true);

        SetRigWeightImmediate(0f);
    }

    protected override void OnNpcDialogueStarted()
    {
        BlendRigWeight(1f);
    }

    protected override void OnNpcDialogueFinished()
    {
        BlendRigWeight(0f);
        SwitchToRepeatDialogue();
        UnlockSelmaHenryDialogueOption();
    }

    private void SwitchToRepeatDialogue()
    {
        if (henrySmartNpc == null)
            return;

        if (repeatConversationForSherlock != null)
            henrySmartNpc.rozmowaDlaPostaciA = repeatConversationForSherlock;

        if (repeatConversationForWatson != null)
            henrySmartNpc.rozmowaDlaPostaciB = repeatConversationForWatson;
    }

    private void UnlockSelmaHenryDialogueOption()
    {
        if (string.IsNullOrWhiteSpace(selmaHenryParameterName))
            return;

        if (selmaSmartNpc == null)
        {
            Int_SelmaDialog selmaDialogue = FindFirstObjectByType<Int_SelmaDialog>();
            selmaSmartNpc = selmaDialogue != null ? selmaDialogue.smartNPC : null;
        }

        if (selmaSmartNpc == null)
        {
            Debug.LogWarning("Henry dialogue finished, but Selma SmartNPC is not assigned.", this);
            return;
        }

        SetConversationBool(selmaSmartNpc.rozmowaDlaPostaciA);
        SetConversationBool(selmaSmartNpc.rozmowaDlaPostaciB);
    }

    private void SetConversationBool(NPCConversation conversation)
    {
        if (conversation == null)
            return;

        string requestedParameterName = selmaHenryParameterName.Trim();
        string resolvedParameterName = requestedParameterName;

        // Conversation parameters live inside its serialized JSON. Resolve the
        // stored name so an accidental leading/trailing whitespace cannot make
        // the gameplay flag differ from the condition used by the dialogue.
        if (conversation.ParameterList == null)
            conversation.DeserializeForEditor();

        if (conversation.ParameterList != null)
        {
            foreach (EditableParameter parameter in conversation.ParameterList)
            {
                if (parameter != null &&
                    string.Equals(parameter.ParameterName?.Trim(), requestedParameterName, StringComparison.Ordinal))
                {
                    resolvedParameterName = parameter.ParameterName;
                    break;
                }
            }
        }

        conversation.SetRuntimeBoolParameter(resolvedParameterName, true);
    }

    private void OnDisable()
    {
        if (rigBlendCoroutine != null)
            StopCoroutine(rigBlendCoroutine);

        SetRigWeightImmediate(0f);
    }

    private void BlendRigWeight(float targetWeight)
    {
        if (dialogueRig == null)
            return;

        if (rigBlendCoroutine != null)
            StopCoroutine(rigBlendCoroutine);

        rigBlendCoroutine = StartCoroutine(BlendRigWeightRoutine(targetWeight));
    }

    private IEnumerator BlendRigWeightRoutine(float targetWeight)
    {
        float startWeight = dialogueRig.weight;
        float elapsed = 0f;

        while (elapsed < rigBlendDuration)
        {
            elapsed += Time.deltaTime;
            dialogueRig.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / rigBlendDuration);
            yield return null;
        }

        dialogueRig.weight = targetWeight;
        rigBlendCoroutine = null;
    }

    private void SetRigWeightImmediate(float weight)
    {
        if (dialogueRig != null)
            dialogueRig.weight = weight;
    }
}
