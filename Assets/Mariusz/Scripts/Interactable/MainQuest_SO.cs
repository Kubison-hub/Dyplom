using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMainQuest", menuName = "Investigation/MainQuest")]
public class MainQuest_SO : ScriptableObject
{
    public string displayName;
    public string shortDescription;

    [TextArea] public string mainDescription;
    

    public List<Quest_SO> requiredQuests;

        
}
