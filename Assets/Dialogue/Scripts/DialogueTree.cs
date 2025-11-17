using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueTree", menuName = "Dialogue/Dialogue Tree", order = 1)]
public class DialogueTree : ScriptableObject
{
    [Tooltip("Unikalny identyfikator tego drzewka dialogowego.")]
    public string DialogueID;

    [Tooltip("Startowy Node w drzewku dialogowym.")]
    public DialogueNode StartNode;

    [Tooltip("Lista wszystkich wêz³ów w tym drzewku (u¿ywana przez edytor GraphView).")]
    public List<DialogueNode> AllNodes = new List<DialogueNode>();

    [Header("Dialog Wewnêtrzny (Interrogation)")]
    [Tooltip("Drzewko dialogowe uruchamiane, gdy obie postacie skoñcz¹ rozmowê z NPC.")]
    public DialogueTree InterrogationDialogue;

    // Metoda pomocnicza do pobierania wêz³a po ID
    public DialogueNode GetNodeByID(string id)
    {
        return AllNodes.Find(n => n.NodeID == id);
    }
}