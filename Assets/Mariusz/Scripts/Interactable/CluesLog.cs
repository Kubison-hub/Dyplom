using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Lightweight objective log for the current story flow. It deliberately does
/// not depend on ClueManager, conclusions, or quest ScriptableObjects.
/// </summary>
public class CluesLog : MonoBehaviour
{
    public static CluesLog Instance;

    [SerializeField] private TextMeshProUGUI questLogText;

    [Header("Text")]
    [SerializeField] private string title = "Kto, jak, dlaczego?";
    [SerializeField] private string examineCrimeSceneText = "Zbadaj miejsce zbrodni";
    [SerializeField] private string connectionsText = "Dowiedz się więcej o Powiązaniach Lady Edith";
    [SerializeField] private string findEthelText = "Odszukaj małą Ethel";
    [SerializeField] private string confrontSessionText = "Skonfrontuj się z uczestnikami sesji";

    private bool crimeSceneCompleted;
    private bool crimeSceneVisible = true;
    private bool connectionsCompleted;
    private bool connectionsVisible = true;
    private bool findEthelVisible;
    private bool findEthelCompleted;
    private bool confrontSessionVisible;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateLog();
    }

    public void CompleteCrimeSceneInvestigation()
    {
        crimeSceneCompleted = true;
        UpdateLog();
    }

    public void RemoveCrimeSceneObjective()
    {
        crimeSceneVisible = false;
        UpdateLog();
    }

    public void AddFindEthelObjective()
    {
        findEthelVisible = true;
        UpdateLog();
    }

    public void CompleteConnectionsObjective()
    {
        connectionsCompleted = true;
        UpdateLog();
    }

    public void RemoveConnectionsObjective()
    {
        connectionsVisible = false;
        UpdateLog();
    }

    public void CompleteFindEthelObjective()
    {
        findEthelVisible = true;
        findEthelCompleted = true;
        UpdateLog();
    }

    public void ReplaceFindEthelWithSessionConfrontation()
    {
        findEthelVisible = false;
        confrontSessionVisible = true;
        UpdateLog();
    }

    public void UpdateLog()
    {
        if (questLogText == null)
            return;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<size=120%><u>{title}</u></size>");

        if (crimeSceneVisible)
            AppendObjective(builder, examineCrimeSceneText, true, crimeSceneCompleted);

        if (connectionsVisible)
            AppendObjective(builder, connectionsText, true, connectionsCompleted);

        if (findEthelVisible)
            AppendObjective(builder, findEthelText, true, findEthelCompleted);

        if (confrontSessionVisible)
            AppendObjective(builder, confrontSessionText, false, false);

        questLogText.text = builder.ToString();
    }

    private static void AppendObjective(StringBuilder builder, string text, bool canComplete, bool completed)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (canComplete && completed)
            builder.AppendLine($" • \u2713 <s>{text}</s>");
        else
            builder.AppendLine($" • {text}");
    }
}
