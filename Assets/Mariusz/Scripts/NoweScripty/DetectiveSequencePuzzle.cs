using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectiveSequencePuzzle : MonoBehaviour
{
    [System.Serializable]
    public class IdeaConnectionStep
    {
        public DetectiveIdeaPoint from;
        public DetectiveIdeaPoint to;
    }

    public enum ConnectionResult
    {
        Correct,
        CorrectReverse,
        Connected,
        Wrong,
        AlreadySolved,
        PuzzleSolved
    }

    [Header("Sequence")]
    public List<IdeaConnectionStep> correctSequence = new List<IdeaConnectionStep>();

    [Header("Wrong Connection Text")]
    public string wrongTitle = "Nie.";
    [TextArea] public string wrongDescription = "To nie uklada sie w logiczny ciag.";

    [Header("Solved")]
    public string solvedTitle = "Oczywiscie.";
    [TextArea] public string solvedDescription = "Teraz rozumiem przebieg wydarzen.";

    [Header("Reparent Before Solved Actions")]
    [Tooltip("Optional. This object is moved under New Parent before Activate On Solved and On Solved are invoked.")]
    [SerializeField] private Transform objectToReparentBeforeSolvedActions;
    [SerializeField] private Transform newParentBeforeSolvedActions;
    [SerializeField] private bool keepWorldPositionWhenReparenting = true;

    public GameObject[] activateOnSolved;
    public GameObject[] deactivateOnSolved;
    public Interactable clueSource;
    public int clueIndex = -1;
    [Tooltip("Enable only on the ground-floor crime-scene puzzle.")]
    [SerializeField] private bool completeCrimeSceneObjectiveOnSolved;
    public UnityEvent onSolved;

    public bool IsSolved { get; private set; }

    private readonly HashSet<string> discoveredConnections = new HashSet<string>();
    private readonly HashSet<string> attemptedConnections = new HashSet<string>();

    public ConnectionResult TryConnect(DetectiveIdeaPoint from, DetectiveIdeaPoint to)
    {
        if (IsSolved)
            return ConnectionResult.AlreadySolved;

        if (from == null || to == null || from == to)
        {
            ShowTopText(wrongTitle, wrongDescription);
            return ConnectionResult.Wrong;
        }

        DetectiveIdeaPoint.IdeaConnection connection = FindConnection(from, to);
        if (connection == null)
        {
            ShowTopText(wrongTitle, wrongDescription);
            Debug.LogWarning($"{name}: Missing connection data for '{GetIdeaName(from)}' -> '{GetIdeaName(to)}'.");
            return ConnectionResult.Wrong;
        }

        string connectionKey = GetConnectionKey(from, to);
        bool wasDiscovered = discoveredConnections.Contains(connectionKey);
        bool wasAttempted = attemptedConnections.Contains(connectionKey);

        if (!DependenciesMet(connection))
        {
            ShowTopText(connection.lockedTitle, connection.lockedDescription);
            return ConnectionResult.Wrong;
        }

        ShowConnectionText(connection, wasAttempted);
        attemptedConnections.Add(connectionKey);

        if (!connection.canConnect)
            return ConnectionResult.Wrong;

        if (wasDiscovered)
            return ConnectionResult.AlreadySolved;

        discoveredConnections.Add(connectionKey);

        bool isReverseConnection;
        int matchingStepIndex = FindMatchingStepIndex(from, to, connection.canConnectReverse, out isReverseConnection);
        if (matchingStepIndex < 0)
            return ConnectionResult.Connected;

        return isReverseConnection ? ConnectionResult.CorrectReverse : ConnectionResult.Correct;
    }

    public void SolveFromOrderedSequence()
    {
        Solve();
    }

    private bool AreAllRequiredConnectionsDiscovered()
    {
        if (correctSequence.Count == 0)
            return false;

        for (int i = 0; i < correctSequence.Count; i++)
        {
            IdeaConnectionStep step = correctSequence[i];
            if (step == null || step.from == null || step.to == null)
                return false;

            if (!IsConnectionDiscovered(step.from, step.to))
                return false;
        }

        return true;
    }

    public bool IsConnectionDiscovered(DetectiveIdeaPoint first, DetectiveIdeaPoint second)
    {
        return first != null && second != null && discoveredConnections.Contains(GetConnectionKey(first, second));
    }

    public bool IsForwardSequenceConnection(DetectiveIdeaPoint from, DetectiveIdeaPoint to)
    {
        for (int i = 0; i < correctSequence.Count; i++)
        {
            IdeaConnectionStep step = correctSequence[i];
            if (step != null && step.from == from && step.to == to)
                return true;
        }

        return false;
    }

    public DetectiveIdeaPoint GetForwardContinuationPoint(DetectiveIdeaPoint startingPoint)
    {
        if (startingPoint == null)
            return null;

        DetectiveIdeaPoint current = startingPoint;
        HashSet<DetectiveIdeaPoint> visited = new HashSet<DetectiveIdeaPoint> { current };

        while (TryGetDiscoveredForwardStep(current, out DetectiveIdeaPoint next) && visited.Add(next))
            current = next;

        return current;
    }

    private bool TryGetDiscoveredForwardStep(DetectiveIdeaPoint from, out DetectiveIdeaPoint next)
    {
        for (int i = 0; i < correctSequence.Count; i++)
        {
            IdeaConnectionStep step = correctSequence[i];
            if (step == null || step.from != from || step.to == null)
                continue;

            if (IsConnectionDiscovered(step.from, step.to))
            {
                next = step.to;
                return true;
            }
        }

        next = null;
        return false;
    }

    private DetectiveIdeaPoint.IdeaConnection FindConnection(DetectiveIdeaPoint from, DetectiveIdeaPoint to)
    {
        DetectiveIdeaPoint.IdeaConnection direct = from.FindConnectionTo(to);
        if (direct != null)
            return direct;

        DetectiveIdeaPoint.IdeaConnection reverse = to.FindConnectionTo(from);
        return reverse != null && reverse.canConnectReverse ? reverse : null;
    }

    private bool DependenciesMet(DetectiveIdeaPoint.IdeaConnection connection)
    {
        if (connection.requiredConnections == null || connection.requiredConnections.Count == 0)
            return true;

        bool requiresAll = connection.dependencyMode == DetectiveIdeaPoint.DependencyMode.All;
        for (int i = 0; i < connection.requiredConnections.Count; i++)
        {
            DetectiveIdeaPoint.ConnectionDependency dependency = connection.requiredConnections[i];
            bool discovered = dependency != null && IsConnectionDiscovered(dependency.first, dependency.second);

            if (requiresAll && !discovered)
                return false;

            if (!requiresAll && discovered)
                return true;
        }

        return requiresAll;
    }

    private void ShowConnectionText(DetectiveIdeaPoint.IdeaConnection connection, bool repeated)
    {
        // A repeated attempt should always be readable. Most connections only
        // need one authored line, so fall back to it when no repeat text exists.
        string description = repeated && !string.IsNullOrWhiteSpace(connection.repeatDescription)
            ? connection.repeatDescription
            : connection.firstDescription;

        ShowTopText(description, "");
    }

    private string GetConnectionKey(DetectiveIdeaPoint first, DetectiveIdeaPoint second)
    {
        int firstId = first.GetInstanceID();
        int secondId = second.GetInstanceID();
        return firstId < secondId ? $"{firstId}:{secondId}" : $"{secondId}:{firstId}";
    }

    private string GetIdeaName(DetectiveIdeaPoint point)
    {
        if (point == null)
            return "None";

        return string.IsNullOrWhiteSpace(point.ideaTitle) ? point.name : point.ideaTitle;
    }

    private int FindMatchingStepIndex(
        DetectiveIdeaPoint from,
        DetectiveIdeaPoint to,
        bool allowReverse,
        out bool isReverseConnection)
    {
        isReverseConnection = false;

        for (int i = 0; i < correctSequence.Count; i++)
        {
            IdeaConnectionStep step = correctSequence[i];
            if (step == null)
                continue;

            if (step.from == from && step.to == to)
                return i;

            if (allowReverse && step.from == to && step.to == from)
            {
                isReverseConnection = true;
                return i;
            }
        }

        return -1;
    }

    private void Solve()
    {
        if (IsSolved)
            return;

        IsSolved = true;

        ReparentBeforeSolvedActions();

        foreach (GameObject target in activateOnSolved)
        {
            if (target != null)
                target.SetActive(true);
        }

        foreach (GameObject target in deactivateOnSolved)
        {
            if (target != null)
                target.SetActive(false);
        }

        if (clueSource != null && clueIndex >= 0)
            clueSource.AddClue(clueIndex);

        if (completeCrimeSceneObjectiveOnSolved)
            CluesLog.Instance?.CompleteCrimeSceneInvestigation();

        ShowTopText(solvedTitle, solvedDescription);
        onSolved?.Invoke();
    }

    private void ReparentBeforeSolvedActions()
    {
        if (objectToReparentBeforeSolvedActions == null || newParentBeforeSolvedActions == null)
            return;

        if (objectToReparentBeforeSolvedActions.parent != newParentBeforeSolvedActions)
        {
            objectToReparentBeforeSolvedActions.SetParent(
                newParentBeforeSolvedActions,
                keepWorldPositionWhenReparenting);
        }
    }

    private void ShowTopText(string title, string description)
    {
        if (PlayerTopText.Instance != null)
        {
            string sherlockText = string.IsNullOrWhiteSpace(description) ? title : description;
            PlayerTopText.Instance.ShowTopText(sherlockText, "");
        }
    }
}
