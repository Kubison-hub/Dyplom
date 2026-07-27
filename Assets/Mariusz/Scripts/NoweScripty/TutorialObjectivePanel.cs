using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Persistent, non-blocking guidance panel. Unlike CluesLog it represents the
/// player's current objectives, not the complete investigation journal.
/// </summary>
public class TutorialObjectivePanel : MonoBehaviour
{
    [Serializable]
    public class Objective
    {
        [Tooltip("Stable id used by TutorialTimeline and other gameplay scripts.")]
        public string id;
        [TextArea] public string description;
        public List<SubObjective> subObjectives = new List<SubObjective>();
    }

    [Serializable]
    public class SubObjective
    {
        [Tooltip("Stable id used by TutorialTimeline and other gameplay scripts.")]
        public string id;
        [TextArea] public string description;
        [HideInInspector] public bool isCompleted;
    }

    [Header("UI")]
    [Tooltip("One TMP text, styled like the existing CluesLogText.")]
    [SerializeField] private TextMeshProUGUI logText;

    [Header("Content")]
    [SerializeField] private string title = "TRYB DETEKTYWA";
    [SerializeField] private List<Objective> objectives = new List<Objective>();

    public string Title => title;

    private void Awake()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Reset()
    {
        CreateDefaultDetectiveObjectives();
    }

    public void SetTitle(string newTitle)
    {
        title = newTitle;
        Refresh();
    }

    public void CompleteSubObjective(string subObjectiveId)
    {
        if (string.IsNullOrWhiteSpace(subObjectiveId))
            return;

        SubObjective subObjective = FindSubObjective(subObjectiveId);
        if (subObjective == null || subObjective.isCompleted)
            return;

        subObjective.isCompleted = true;
        Refresh();
    }

    public void ResetSubObjective(string subObjectiveId)
    {
        SubObjective subObjective = FindSubObjective(subObjectiveId);
        if (subObjective == null || !subObjective.isCompleted)
            return;

        subObjective.isCompleted = false;
        Refresh();
    }

    public bool IsSubObjectiveCompleted(string subObjectiveId)
    {
        SubObjective subObjective = FindSubObjective(subObjectiveId);
        return subObjective != null && subObjective.isCompleted;
    }

    public void Refresh()
    {
        if (logText == null)
            return;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<size=120%><u>{title}</u></size>");

        for (int objectiveIndex = 0; objectiveIndex < objectives.Count; objectiveIndex++)
        {
            Objective objective = objectives[objectiveIndex];
            if (objective == null || string.IsNullOrWhiteSpace(objective.description))
                continue;

            bool objectiveCompleted = AreAllSubObjectivesCompleted(objective);
            builder.Append(" • ");
            builder.Append(Format(objective.description, objectiveCompleted));
            builder.AppendLine();

            if (objective.subObjectives != null)
            {
                foreach (SubObjective subObjective in objective.subObjectives)
                {
                    if (subObjective == null || string.IsNullOrWhiteSpace(subObjective.description))
                        continue;

                    builder.Append("    <size=90%>└ ");
                    builder.Append(Format(subObjective.description, subObjective.isCompleted));
                    builder.Append("</size>");
                    builder.AppendLine();
                }
            }

        }

        logText.text = builder.ToString().TrimEnd();
    }

    private SubObjective FindSubObjective(string subObjectiveId)
    {
        foreach (Objective objective in objectives)
        {
            if (objective?.subObjectives == null)
                continue;

            foreach (SubObjective subObjective in objective.subObjectives)
            {
                if (subObjective != null && subObjective.id == subObjectiveId)
                    return subObjective;
            }
        }

        return null;
    }

    private static bool AreAllSubObjectivesCompleted(Objective objective)
    {
        if (objective.subObjectives == null || objective.subObjectives.Count == 0)
            return false;

        foreach (SubObjective subObjective in objective.subObjectives)
        {
            if (subObjective == null || !subObjective.isCompleted)
                return false;
        }

        return true;
    }

    private string Format(string value, bool completed)
    {
        return completed ? "<s>" + value + "</s>" : value;
    }

    [ContextMenu("Create Default Detective Objectives")]
    private void CreateDefaultDetectiveObjectives()
    {
        title = "TRYB DETEKTYWA";
        objectives = new List<Objective>
        {
            new Objective
            {
                id = "investigate-crime-scene",
                description = "1. Zbadaj miejsce zbrodni",
                subObjectives = new List<SubObjective>
                {
                    new SubObjective { id = "examine-lady-edith", description = "Zbadaj cialo Lady Edith" },
                    new SubObjective { id = "connect-facts", description = "Polacz fakty" }
                }
            },
            new Objective
            {
                id = "learn-more",
                description = "2. Dowiedz sie wiecej",
                subObjectives = new List<SubObjective>
                {
                    new SubObjective { id = "talk-to-witnesses", description = "Porozmawiaj ze swiadkami w domu" }
                }
            }
        };

        Refresh();
    }
}
