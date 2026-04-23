
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestConclusion", menuName = "Investigation/QuestConclusion")]
public class QuestConclusions_SO : ScriptableObject
{
   
    public string displayName;
    public string type = "Konkluzja";
    public string shortDescription;
    [TextArea] public string questDescription;
    [TextArea] public string mainDescription;

    public List<Conclusions_SO> requiredConclusions;

    
    
}
