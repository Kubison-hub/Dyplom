using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Investigation/Quest")]
public class Quest_SO : ScriptableObject
{
    public string displayName;
    public string shortDescription;
    [Space]
    [TextArea] public string mainDescription;

    public List<QuestConclusions_SO> requiredQuestConclusions;

    public List<GameObject> FirstInteractions;

    public Quest_SO nextQuest;
}
