using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

// Wymaga u¿ycia TextMeshPro w projekcie
public class DialogueUI : MonoBehaviour
{
    [Header("Elementy Canvasu Dialogowego")]
    public GameObject dialoguePanel;
    public Image npcIllustration;
    public Image playerIllustration;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Transform optionsContainer;
    public Button optionButtonPrefab;

    [Header("Ustawienia Wizualne")]
    [Tooltip("Kolor podœwietlanych s³ów kluczowych.")]
    public Color keywordColor = Color.yellow;

    // Akcja wywo³ywana, gdy gracz wybierze opcjê
    public Action<DialogueOption> OnOptionSelectedAction;

    private void Start()
    {
        dialoguePanel.SetActive(false);
    }

    public void ShowPanel(bool show)
    {
        dialoguePanel.SetActive(show);
        // Resetowanie przycisków
        if (!show)
        {
            ClearOptions();
        }
    }

    // Ustawia i formatuje tekst dialogu, w tym podœwietla s³owa kluczowe
    public void SetDialogueText(DialogueLine line, string highlightedText)
    {
        ShowPanel(true);

        // Prosta logika dla nazwy
        string name = line.Speaker.ToString();
        if (line.Speaker == CharacterType.NPC)
        {
            name = "Œwiadek (Tymczasowa)"; // Powinno byæ pobrane z NPC
        }
        speakerNameText.text = name;

        // Podœwietlanie s³ów kluczowych (zamiana [KEYWORD] na tagi TMPro)
        string coloredText = highlightedText.Replace("[KEYWORD]", $"<color=#{ColorUtility.ToHtmlStringRGB(keywordColor)}>");
        coloredText = coloredText.Replace("[/KEYWORD]", "</color>");

        dialogueText.text = coloredText;

        // TODO: Zaimplementuj logikê zmiany ilustracji w zale¿noœci od line.Speaker
    }

    // Generuje przyciski opcji wyboru dla gracza (statyczne + dynamiczne s³owa kluczowe)
    public void DisplayOptions(List<DialogueOption> availableOptions, List<KeywordData> activeKeywords)
    {
        ClearOptions();

        // 1. Dodaj statyczne opcje
        List<DialogueOption> allOptions = new List<DialogueOption>(availableOptions);

        // 2. Dodaj dynamiczne opcje s³ów kluczowych
        foreach (var keyword in activeKeywords)
        {
            // Tworzymy pseudo-opcjê dla s³owa kluczowego
            DialogueOption keywordOption = new DialogueOption
            {
                OptionText = $"Zapytaj o: {keyword.Keyword}",
                // Specjalny identyfikator dla mened¿era
                NextNodeID = $"KEYWORD:{keyword.Keyword}",
                // Przechowujemy tu notatkê, aby ³atwo j¹ przekazaæ do DialogueManager
                Actions = new List<string> { JsonUtility.ToJson(keyword.NoteOnAsk) }
            };
            allOptions.Add(keywordOption);
        }

        // 3. Stwórz przyciski
        foreach (var option in allOptions)
        {
            Button button = Instantiate(optionButtonPrefab, optionsContainer);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = option.OptionText;
            }

            // Przypisanie akcji
            button.onClick.AddListener(() =>
            {
                // Wyczyœæ UI przed wywo³aniem akcji
                ClearOptions();
                OnOptionSelectedAction?.Invoke(option);
            });
        }
    }

    private void ClearOptions()
    {
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }
    }
}