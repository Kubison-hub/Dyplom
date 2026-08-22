using UnityEngine;

public class Int_lv3_ManequineExam : MonoBehaviour
{
    [SerializeField, Min(1)] private int requiredClueCount = 3;
    [SerializeField] private DetectiveIdeaPoint mannequinIdeaPoint;

    private int discoveredClueCount;
    private bool completed;

    public void RegisterClue()
    {
        if (completed)
            return;

        discoveredClueCount++;
        if (discoveredClueCount < requiredClueCount)
            return;

        completed = true;
        mannequinIdeaPoint?.RevealFromExternalSource();
    }
}
