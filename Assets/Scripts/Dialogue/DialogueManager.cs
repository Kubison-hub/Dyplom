using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;

    [Header("UI")]
    public DialogueUI dialogueUI; // assign DialogueUI prefab/instance in inspector

    private bool isInDialogue = false;
    private NPCDialogue currentNPC;
    private DialogueNode currentNode;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        if (dialogueUI == null)
        {
            Debug.LogError("DialogueManager: DialogueUI reference missing!");
        }
        dialogueUI.HideImmediate();
    }

    public void StartDialogue(DialogueNode root, NPCDialogue npc)
    {
        if (isInDialogue) return;

        isInDialogue = true;
        currentNPC = npc;
        currentNode = root;

        // notify systems (Disable movement, inputs, interactions)
        OnDialogueStarted?.Invoke();

        // show UI and display the first node
        dialogueUI.Show();
        ShowNode(currentNode);
    }

    private void ShowNode(DialogueNode node)
    {
        currentNode = node;
        if (currentNode == null)
        {
            EndDialogue();
            return;
        }

        dialogueUI.SetLine(currentNode.line);
        dialogueUI.BuildOptions(currentNode.options, OnOptionSelected);
    }

    private void OnOptionSelected(DialogueOption option)
    {
        if (option == null)
        {
            EndDialogue();
            return;
        }

        if (option.endDialogue || option.nextNode == null)
        {
            EndDialogue();
            return;
        }

        // move to next node
        ShowNode(option.nextNode);
    }

    public void EndDialogue()
    {
        if (!isInDialogue) return;

        isInDialogue = false;
        dialogueUI.Hide();
        currentNode = null;
        currentNPC = null;

        OnDialogueEnded?.Invoke();
    }

    // Public helpers
    public bool IsInDialogue() => isInDialogue;
}
