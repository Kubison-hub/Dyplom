using System.Text;
using TMPro;
using UnityEngine;

public class CluesLog : MonoBehaviour
{
    public static CluesLog Instance;
    public TextMeshProUGUI questLogText;

    private void Awake() => Instance = this;

    private void Start()
    {
        UpdateLog();
    }

    public void UpdateLog()
    {
        if (questLogText == null) return;

        var manager = ClueManager.Instance;
        if (manager == null || manager.activeQuests == null)
        {
            questLogText.text = "";
            return;
        }

        StringBuilder sb = new StringBuilder();

        foreach (var quest in manager.activeQuests)
        {
            if (quest == null) continue;

            sb.AppendLine($"<size=120%><u>{quest.displayName}</u></size>");

            foreach (var reqQC in quest.requiredQuestConclusions)
            {
                if (reqQC == null) continue;

                bool isQCFinished = manager.collectedQuestConclusions.Contains(reqQC);

                string qcStatus = isQCFinished ? "<s>" : "";
                string qcEndStatus = isQCFinished ? "</s>" : "";

                sb.AppendLine($" • {qcStatus}{reqQC.questDescription}{qcEndStatus}");

                if (isQCFinished)
                {
                    sb.AppendLine($"    <size=90%>{reqQC.shortDescription}</size>");
                }
                else
                {
                    if (reqQC.requiredConclusions == null) continue;

                    foreach (var conclusion in reqQC.requiredConclusions)
                    {
                        if (conclusion != null && manager.collectedConclusions.Contains(conclusion))
                        {
                            sb.AppendLine($"    <size=90%>└ {conclusion.displayName}</size>");
                        }
                    }
                }
            }
            sb.AppendLine();
        }

        questLogText.text = sb.ToString();
    }
}