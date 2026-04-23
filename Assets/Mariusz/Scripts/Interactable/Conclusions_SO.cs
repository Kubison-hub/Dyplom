using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConclusion", menuName = "Investigation/Conclusion")]
public class Conclusions_SO : ScriptableObject
{
    public string displayName;
    public string type = "Wniosek";
    public List<Clues_SO> requiredClues;
    public float displayDuration = 10f;

    public string questTask;
    [TextArea] public string shortDescription;
}
