using System.Text;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InvestigationLog : MonoBehaviour
{
    public TextMeshProUGUI logTextDisplay;
    public GameObject logPanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        bool isOpen = !logPanel.activeSelf;
        logPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshJournalView();
        }
    }

    public void RefreshJournalView()
    {
        StringBuilder sb = new StringBuilder();

        // 0. Active Quests
        foreach (var quest in ClueManager.Instance.activeQuests)
        {
            sb.AppendLine($"<size=120%><b>{quest.displayName}</b></size>");
            // Jeśli quest ma ogólny opis zadania
            sb.AppendLine($"<size=100%>{quest.shortDescription}</size>");

            foreach (var questConclusion in quest.requiredQuestConclusions)
            {
                // Sprawdzamy, czy ta konkretna konkluzja została już odkryta przez gracza
                if (ClueManager.Instance.collectedQuestConclusions.Contains(questConclusion))
                {
                    // ZMIANA: Wyświetlamy shortDescription zamiast questDescription
                    sb.AppendLine($"    <color=#00FFCC>✓ {questConclusion.shortDescription}</color>");
                }
                else
                {
                    // Opcjonalnie: wyświetlamy "???", jeśli jeszcze nie odkryto tego etapu
                    sb.AppendLine($"    <color=#777777>• {questConclusion.shortDescription}</color>");
                }
            }
            sb.AppendLine(); // Odstęp między questami
        }

        // 1. Quest Conclusions
        foreach (var questConclusion in ClueManager.Instance.collectedQuestConclusions)
        {
            sb.AppendLine($"<color=#FF4500><size=120%><b>{questConclusion.displayName}</b></size></color>");
            sb.AppendLine($"<i>{questConclusion.shortDescription}</i>");

            foreach (var reqConclusion in questConclusion.requiredConclusions)
            {
                if (ClueManager.Instance.collectedConclusions.Contains(reqConclusion))
                {
                    AppendConclusionToSB(sb, reqConclusion, "    ");
                }
            }
            sb.AppendLine("<color=#555555>──────────────────────────────────</color>");
        }

        // 2. Conclusions
        bool hasFreeConclusions = false;
        foreach (var conclusion in ClueManager.Instance.collectedConclusions)
        {
            if (!IsConclusionPartOfAnyQuestConclusion(conclusion))
            {
                if (!hasFreeConclusions)
                {
                    hasFreeConclusions = true;
                }
                AppendConclusionToSB(sb, conclusion, "");
                
            }
        }

        // 3. Clue
        sb.AppendLine("");
        bool hasFreeClues = false;
        foreach (var clue in ClueManager.Instance.collectedClues)
        {
            if (!IsCluePartOfAnyConclusion(clue))
            {
                if (!hasFreeClues)
                {
                    sb.AppendLine("<b>poszlaki:</b>");
                    hasFreeClues = true;
                    sb.AppendLine($"<color=#ffffff> • <b>{clue.displayName}</b></color>");
                    sb.AppendLine($"<size=90%> • {clue.shortDescription}</size></color>");
                }
                
            }
        }

        logTextDisplay.text = sb.ToString();
    }

    // Pomocnicza metoda budująca tekst dla pojedynczej konkluzji i jej wskazówek
    private void AppendConclusionToSB(StringBuilder sb, Conclusions_SO conclusion, string indent)
    {
        // Nagłówek Konkluzji
        sb.AppendLine($"{indent}<size=115%><b>{conclusion.displayName}</b></size>");
        sb.AppendLine($"{indent}{conclusion.shortDescription}");
        

        foreach (var clue in conclusion.requiredClues)
        {
            if (ClueManager.Instance.collectedClues.Contains(clue))
            {
                // Główna nazwa wskazówki
                //sb.AppendLine($"{indent}   <color=#ffffff>• <b>{clue.displayName}</b></color>");

                // Short Description wskazówki (wcięty pod nazwą, innym kolorem)
                if (!string.IsNullOrEmpty(clue.shortDescription))
                {
                    sb.AppendLine($"{indent}     <size=90%>• {clue.shortDescription}</size>");
                }
            }
        }
    }

    private bool IsQuestConclusionPartOfAnyQuest(QuestConclusions_SO questConclusion)
    {
        foreach (var qc in ClueManager.Instance.activeQuests)
        {
            if (qc.requiredQuestConclusions.Contains(questConclusion)) return true;
        }
        return false;
    }

    // Czy konkluzja jest już częścią jakiegoś przełomu?
    private bool IsConclusionPartOfAnyQuestConclusion(Conclusions_SO conclusion)
    {
        foreach (var qc in ClueManager.Instance.collectedQuestConclusions)
        {
            if (qc.requiredConclusions.Contains(conclusion)) return true;
        }
        return false;
    }

    // Czy wskazówka jest już częścią jakiegoś wniosku?
    private bool IsCluePartOfAnyConclusion(Clues_SO clue)
    {
        foreach (var conclusion in ClueManager.Instance.collectedConclusions)
        {
            if (conclusion.requiredClues.Contains(clue)) return true;
        }
        return false;
    }
}