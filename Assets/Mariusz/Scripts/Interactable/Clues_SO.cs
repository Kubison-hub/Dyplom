using UnityEngine;

[CreateAssetMenu(fileName = "NewClue", menuName = "Investigation/Clue")]
public class Clues_SO : ScriptableObject

{
    public string clueID;
    public string type = "Spostrze¿enie";
    public string displayName;
    public string shortDescription;

    public float displayDuration = 5f;

    public string sherlockText;
    public string watsonText;
}
