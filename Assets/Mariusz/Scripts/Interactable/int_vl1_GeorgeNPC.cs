using System;
using DialogueEditor;
using UnityEngine;

public class int_vl1_GeorgeNPC : Int_lv1_NpcDialogBase
{
    [Header("Selma Dialogue Unlock")]
    [SerializeField] private SmartNPC selmaSmartNpc;
    [SerializeField] private string selmaGeorgeParameterName = "George";

    protected override InteractionType RequiredInteractionType => InteractionType.int_vl1_GeorgeNPC;

    protected override void OnNpcDialogueFinished()
    {
        UnlockSelmaGeorgeDialogueOption();
    }

    private void UnlockSelmaGeorgeDialogueOption()
    {
        if (string.IsNullOrWhiteSpace(selmaGeorgeParameterName))
            return;

        if (selmaSmartNpc == null)
        {
            Int_SelmaDialog selmaDialogue = FindFirstObjectByType<Int_SelmaDialog>();
            selmaSmartNpc = selmaDialogue != null ? selmaDialogue.smartNPC : null;
        }

        if (selmaSmartNpc == null)
        {
            Debug.LogWarning("George dialogue finished, but Selma SmartNPC is not assigned.", this);
            return;
        }

        SetConversationBool(selmaSmartNpc.rozmowaDlaPostaciA);
        SetConversationBool(selmaSmartNpc.rozmowaDlaPostaciB);
    }

    private void SetConversationBool(NPCConversation conversation)
    {
        if (conversation == null)
            return;

        string requestedParameterName = selmaGeorgeParameterName.Trim();
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

        conversation.SetRuntimeBoolParameter(resolvedParameterName, true);
    }
}
