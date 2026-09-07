using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using DialogueEditor;

public class int_lv1_ArthurNPC : Int_lv1_NpcDialogBase
{
    [Header("Dialogue Rig")]
    [SerializeField] private Rig dialogueRig;
    [SerializeField, Min(0.01f)] private float rigBlendDuration = 0.25f;

    [Header("Repeat Dialogue")]
    [SerializeField] private SmartNPC arthurSmartNpc;
    [SerializeField] private string firstDialogueParameterName = "First";

    private Coroutine rigBlendCoroutine;

    protected override InteractionType RequiredInteractionType => InteractionType.int_lv1_ArthurNPC;
    protected override bool ShouldFacePlayerBeforeDialogue => false;
    protected override bool ShouldFaceInteractingPlayerTowardNpcBeforeDialogue => true;

    private void Awake()
    {
        if (dialogueRig == null)
            dialogueRig = GetComponentInChildren<Rig>(true);

        SetRigWeightImmediate(0f);
    }

    protected override void Start()
    {
        base.Start();

        if (arthurSmartNpc == null)
            arthurSmartNpc = GetComponentInChildren<SmartNPC>(true);

        SetRigWeightImmediate(0f);
    }

    protected override void OnNpcDialogueStarted()
    {
        BlendRigWeight(1f);
    }

    protected override void OnNpcDialogueFinished()
    {
        BlendRigWeight(0f);
        SetFirstDialogueParameter(false);
    }

    private void SetFirstDialogueParameter(bool value)
    {
        if (arthurSmartNpc == null || string.IsNullOrWhiteSpace(firstDialogueParameterName))
            return;

        SetConversationBool(arthurSmartNpc.rozmowaDlaPostaciA, value);
        SetConversationBool(arthurSmartNpc.rozmowaDlaPostaciB, value);
    }

    private void SetConversationBool(NPCConversation conversation, bool value)
    {
        if (conversation == null)
            return;

        string requestedParameterName = firstDialogueParameterName.Trim();
        string resolvedParameterName = requestedParameterName;

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

        conversation.SetRuntimeBoolParameter(resolvedParameterName, value);
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
