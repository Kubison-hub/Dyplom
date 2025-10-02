using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueNode", menuName = "Dialogue/Node", order = 0)]
public class DialogueNode : ScriptableObject
{
    [TextArea(3, 8)]
    public string line; // Raw line. Use tokens for highlighted words (see docs below).

    public List<DialogueOption> options = new List<DialogueOption>();

    // Optional: actions, conditions, IDs, tags... extendable for quest flags, vars etc.
}

[System.Serializable]
public class DialogueOption
{
    [TextArea(1, 3)]
    public string optionText; // text shown on the button

    public DialogueNode nextNode; // null => ends conversation

    [Tooltip("Optional: set to true to close dialogue after choosing this option.")]
    public bool endDialogue = false;

    // Optional: you can add conditions, callback events, modify game state, set keys, etc.
}
