using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI speakerLineText; // main dialogue line
    public Transform optionsContainer; // parent for option buttons
    public Button optionButtonPrefab; // prefab (Button with TMP text child)

    [Header("Special Word Styling")]
    [Tooltip("Regex token format: <<TAG:word or phrase>> e.g. <<K:Golden Key>>")]
    public string tokenPattern = @"<<(?<tag>[A-Za-z0-9_]+):(?<content>.+?)>>";

    [Tooltip("TMP rich text template for highlighted token. Use {0} for content and {1} for tag.")]
    public string highlightTemplate = "<mark=#FFFF0055>{0}</mark><color=#FFD54D><b>{0}</b></color>";

    private void Reset()
    {
        // attempt to auto-find references if added to scene
        canvasGroup = GetComponentInChildren<CanvasGroup>();
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        ClearOptions();
    }

    public void HideImmediate()
    {
        gameObject.SetActive(false);
    }

    public void SetLine(string rawLine)
    {
        // Parse tokens for special words using regex and replace with TMP rich text markup.
        if (string.IsNullOrEmpty(rawLine))
        {
            speakerLineText.text = "";
            return;
        }

        string parsed = ParseTokensToTMP(rawLine);
        speakerLineText.text = parsed;
    }

    private string ParseTokensToTMP(string line)
    {
        var regex = new Regex(tokenPattern);
        return regex.Replace(line, match =>
        {
            var tag = match.Groups["tag"].Value;
            var content = match.Groups["content"].Value;

            // Customize template: we use mark + color + bold. Could vary by tag.
            string styled = string.Format(highlightTemplate, content, tag);
            return styled;
        });
    }

    public void BuildOptions(List<DialogueOption> options, System.Action<DialogueOption> onOptionChosen)
    {
        ClearOptions();

        if (options == null || options.Count == 0)
        {
            // If no options — create a default "Close" button
            var closeBtn = Instantiate(optionButtonPrefab, optionsContainer);
            closeBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Close";
            closeBtn.onClick.AddListener(() => onOptionChosen?.Invoke(null));
            return;
        }

        foreach (var opt in options)
        {
            var btn = Instantiate(optionButtonPrefab, optionsContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = ParseTokensToTMP(opt.optionText);
            btn.onClick.AddListener(() => onOptionChosen?.Invoke(opt));
        }
    }

    private void ClearOptions()
    {
        for (int i = optionsContainer.childCount - 1; i >= 0; i--)
            Destroy(optionsContainer.GetChild(i).gameObject);
    }
}
