using System;
using System.Collections.Generic;
using DialogueEditor;
using UnityEngine;

/// <summary>
/// Keeps the investigation state used by the optional Sherlock-Watson hint dialogue.
/// Gameplay interactions call the public Mark methods; the component then mirrors the
/// matching condition into both of the SmartNPC conversation variants.
/// </summary>
public sealed class SherlockWatsonHintConditions : MonoBehaviour
{
    public bool IsTableHoverDiscovered => tableHover;

    [Header("Sherlock-Watson Dialogue")]
    [SerializeField] private SmartNPC sherlockWatsonSmartNpc;

    [Header("Dialogue Parameter Names")]
    [SerializeField] private string bodyExaminedParameterName = "BodyExamined";
    [SerializeField] private string ringFoundParameterName = "RingFound";
    [SerializeField] private string paperFoundParameterName = "PaperFound";
    [SerializeField] private string bulletWoundFoundParameterName = "BulletWoundFound";

    [Header("Act I IdeaPoint Parameters")]
    [SerializeField] private string ideaPointParameterName = "IdeaPoint";
    [SerializeField] private string lightsParameterName = "Lights";
    [SerializeField] private string movingTableParameterName = "MovingTable";
    [SerializeField] private string emptyWallParameterName = "EmptyWall";
    [SerializeField] private string fireplaceParameterName = "FirePlace";
    [SerializeField] private string edithBulletParameterName = "EdithBullet";
    [SerializeField] private string windowBulletParameterName = "WindowBullet";
    [SerializeField] private string ideaPuzzleCompleteParameterName = "IdeaPuzzleComplete";
    [SerializeField] private string tableHoverParameterName = "TableHover";
    [SerializeField] private string libraryGateParameterName = "LibraryGate";
    [SerializeField] private string magicBallParameterName = "MagicBall";
    [SerializeField] private string easyTableCompleteParameterName = "EasyTableComplete";

    [Header("Investigation Progress")]
    [SerializeField] private bool bodyExamined;
    [SerializeField] private bool ringFound;
    [SerializeField] private bool paperFound;
    [SerializeField] private bool bulletWoundFound;

    [Header("Act I IdeaPoint Progress")]
    [SerializeField] private bool ideaPointFound;
    [SerializeField] private bool lightsFound;
    [SerializeField] private bool movingTableFound;
    [SerializeField] private bool emptyWallFound;
    [SerializeField] private bool fireplaceFound;
    [SerializeField] private bool edithBulletFound;
    [SerializeField] private bool windowBulletFound;
    [SerializeField] private bool ideaPuzzleComplete;
    [SerializeField] private bool tableHover;
    [SerializeField] private bool libraryGate;
    [SerializeField] private bool magicBall;
    [SerializeField] private bool easyTableComplete;

    [Header("Act I IdeaPoints")]
    [Tooltip("When left empty, the component finds IdeaPoint_Light automatically.")]
    [SerializeField] private DetectiveIdeaPoint lightsIdeaPoint;
    [Tooltip("When left empty, the component finds IdeaPoint_Table automatically.")]
    [SerializeField] private DetectiveIdeaPoint movingTableIdeaPoint;
    [Tooltip("When left empty, the component finds IdeaPoint_Wall automatically.")]
    [SerializeField] private DetectiveIdeaPoint emptyWallIdeaPoint;
    [Tooltip("When left empty, the component finds IdeaPoint_FirePlace automatically.")]
    [SerializeField] private DetectiveIdeaPoint fireplaceIdeaPoint;
    [Tooltip("When left empty, the component finds IdeaPoint_Edith automatically.")]
    [SerializeField] private DetectiveIdeaPoint edithBulletIdeaPoint;
    [Tooltip("When left empty, the component finds IdeaPoint_Window automatically.")]
    [SerializeField] private DetectiveIdeaPoint windowBulletIdeaPoint;

    [Header("Optional Notebook Notes")]
    [SerializeField] private List<NoteData> notes = new List<NoteData>();

    private void Reset()
    {
        FindSmartNpcIfNeeded();
    }

    private void Awake()
    {
        FindSmartNpcIfNeeded();
        FindActOneIdeaPointsIfNeeded();
        SubscribeToActOneIdeaPoints();
    }

    private void Start()
    {
        // Some scene IdeaPoints receive their ID from Awake. Resolve once more here
        // so the dialogue never depends on Unity's Awake execution order.
        FindActOneIdeaPointsIfNeeded();
        SubscribeToActOneIdeaPoints();
        SynchronizeActOneIdeaPointProgress();
        ApplyAllKnownConditions();
    }

    private void OnDestroy()
    {
        UnsubscribeFromActOneIdeaPoints();
    }

    public void MarkBodyExamined()
    {
        bodyExamined = true;
        ApplyConditionToBothConversations(bodyExaminedParameterName, true);
    }

    public void MarkRingFound()
    {
        ringFound = true;
        ApplyConditionToBothConversations(ringFoundParameterName, true);
    }

    public void MarkPaperFound()
    {
        paperFound = true;
        ApplyConditionToBothConversations(paperFoundParameterName, true);
    }

    public void MarkBulletWoundFound()
    {
        bulletWoundFound = true;
        ApplyConditionToBothConversations(bulletWoundFoundParameterName, true);
    }

    public void MarkLightsFound()
    {
        lightsFound = true;
        MarkIdeaPointFound();
        ApplyConditionToBothConversations(lightsParameterName, true);
    }

    public void MarkMovingTableFound()
    {
        movingTableFound = true;
        MarkIdeaPointFound();
        ApplyConditionToBothConversations(movingTableParameterName, true);
    }

    public void MarkEmptyWallFound()
    {
        emptyWallFound = true;
        MarkIdeaPointFound();
        ApplyConditionToBothConversations(emptyWallParameterName, true);
    }

    public void MarkFireplaceFound()
    {
        fireplaceFound = true;
        MarkIdeaPointFound();
        ApplyConditionToBothConversations(fireplaceParameterName, true);
    }

    public void MarkEdithBulletFound()
    {
        edithBulletFound = true;
        MarkIdeaPointFound();
        ApplyConditionToBothConversations(edithBulletParameterName, true);
    }

    public void MarkWindowBulletFound()
    {
        windowBulletFound = true;
        MarkIdeaPointFound();
        ApplyConditionToBothConversations(windowBulletParameterName, true);
    }

    public void MarkIdeaPuzzleComplete()
    {
        ideaPuzzleComplete = true;
        ApplyConditionToBothConversations(ideaPuzzleCompleteParameterName, true);
    }

    public void MarkTableHover()
    {
        if (tableHover)
            return;

        tableHover = true;
        ApplyConditionToBothConversations(tableHoverParameterName, true);
    }

    public void MarkLibraryGateOpened()
    {
        if (libraryGate)
            return;

        libraryGate = true;
        ApplyConditionToBothConversations(libraryGateParameterName, true);
    }

    public void MarkMagicBallFound()
    {
        if (magicBall)
            return;

        magicBall = true;
        ApplyConditionToBothConversations(magicBallParameterName, true);
    }

    public void MarkEasyTableComplete()
    {
        if (easyTableComplete)
            return;

        easyTableComplete = true;
        ApplyConditionToBothConversations(easyTableCompleteParameterName, true);
    }

    /// <summary>
    /// Reapplies all serialized investigation flags to both dialogue variants.
    /// Call this after restoring a saved investigation state.
    /// </summary>
    public void ApplyAllKnownConditions()
    {
        ApplyConditionToBothConversations(bodyExaminedParameterName, bodyExamined);
        ApplyConditionToBothConversations(ringFoundParameterName, ringFound);
        ApplyConditionToBothConversations(paperFoundParameterName, paperFound);
        ApplyConditionToBothConversations(bulletWoundFoundParameterName, bulletWoundFound);
        ApplyConditionToBothConversations(ideaPointParameterName, ideaPointFound);
        ApplyConditionToBothConversations(lightsParameterName, lightsFound);
        ApplyConditionToBothConversations(movingTableParameterName, movingTableFound);
        ApplyConditionToBothConversations(emptyWallParameterName, emptyWallFound);
        ApplyConditionToBothConversations(fireplaceParameterName, fireplaceFound);
        ApplyConditionToBothConversations(edithBulletParameterName, edithBulletFound);
        ApplyConditionToBothConversations(windowBulletParameterName, windowBulletFound);
        ApplyConditionToBothConversations(ideaPuzzleCompleteParameterName, ideaPuzzleComplete);
        ApplyConditionToBothConversations(tableHoverParameterName, tableHover);
        ApplyConditionToBothConversations(libraryGateParameterName, libraryGate);
        ApplyConditionToBothConversations(magicBallParameterName, magicBall);
        ApplyConditionToBothConversations(easyTableCompleteParameterName, easyTableComplete);
    }

    /// <summary>
    /// Generic entry point for gameplay code that needs to set another dialogue bool.
    /// </summary>
    public void SetCondition(string parameterName, bool value)
    {
        ApplyConditionToBothConversations(parameterName, value);
    }

    public void AddNote(int noteIndex)
    {
        AddNote(noteIndex, true);
    }

    public void AddNoteSilently(int noteIndex)
    {
        AddNote(noteIndex, false);
    }

    public void AddAllNotes()
    {
        for (int i = 0; i < notes.Count; i++)
            AddNote(i);
    }

    private void AddNote(int noteIndex, bool showUpdateNotification)
    {
        if (noteIndex < 0 || noteIndex >= notes.Count)
        {
            Debug.LogWarning($"{name}: notebook note index {noteIndex} is outside the assigned Notes list.", this);
            return;
        }

        NoteData note = notes[noteIndex];
        if (note == null)
        {
            Debug.LogWarning($"{name}: notebook note at index {noteIndex} is not assigned.", this);
            return;
        }

        if (NotebookManager.Instance == null)
        {
            Debug.LogWarning($"{name}: NotebookManager.Instance was not found.", this);
            return;
        }

        NotebookManager.Instance.AddNote(note, showUpdateNotification);
    }

    private void ApplyConditionToBothConversations(string parameterName, bool value)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return;

        FindSmartNpcIfNeeded();
        if (sherlockWatsonSmartNpc == null)
        {
            Debug.LogWarning($"{name}: Sherlock-Watson SmartNPC is not assigned.", this);
            return;
        }

        SetConversationBool(sherlockWatsonSmartNpc.rozmowaDlaPostaciA, parameterName, value);
        SetConversationBool(sherlockWatsonSmartNpc.rozmowaDlaPostaciB, parameterName, value);
    }

    private void FindSmartNpcIfNeeded()
    {
        if (sherlockWatsonSmartNpc != null)
            return;

        sherlockWatsonSmartNpc = GetComponent<SmartNPC>();
        if (sherlockWatsonSmartNpc == null)
            sherlockWatsonSmartNpc = GetComponentInChildren<SmartNPC>(true);
    }

    private void FindActOneIdeaPointsIfNeeded()
    {
        lightsIdeaPoint ??= FindIdeaPoint("IdeaPoint_Light");
        movingTableIdeaPoint ??= FindIdeaPoint("IdeaPoint_Table");
        emptyWallIdeaPoint ??= FindIdeaPoint("IdeaPoint_Wall");
        fireplaceIdeaPoint ??= FindIdeaPoint("IdeaPoint_FirePlace");
        edithBulletIdeaPoint ??= FindIdeaPoint("IdeaPoint_Edith");
        windowBulletIdeaPoint ??= FindIdeaPoint("IdeaPoint_Window");
    }

    private static DetectiveIdeaPoint FindIdeaPoint(string ideaId)
    {
        DetectiveIdeaPoint[] points = FindObjectsByType<DetectiveIdeaPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        DetectiveIdeaPoint inactiveMatch = null;

        foreach (DetectiveIdeaPoint point in points)
        {
            if (point == null ||
                (!string.Equals(point.ideaId, ideaId, StringComparison.Ordinal) &&
                 !string.Equals(point.gameObject.name, ideaId, StringComparison.Ordinal)))
            {
                continue;
            }

            // Scenes can contain disabled prefab leftovers with the same IdeaPoint ID.
            // Always bind dialogue conditions to the live gameplay instance first.
            if (point.enabled && point.gameObject.activeInHierarchy)
                return point;

            inactiveMatch ??= point;
        }

        return inactiveMatch;
    }

    private void SubscribeToActOneIdeaPoints()
    {
        Subscribe(lightsIdeaPoint);
        Subscribe(movingTableIdeaPoint);
        Subscribe(emptyWallIdeaPoint);
        Subscribe(fireplaceIdeaPoint);
        Subscribe(edithBulletIdeaPoint);
        Subscribe(windowBulletIdeaPoint);
    }

    private void UnsubscribeFromActOneIdeaPoints()
    {
        Unsubscribe(lightsIdeaPoint);
        Unsubscribe(movingTableIdeaPoint);
        Unsubscribe(emptyWallIdeaPoint);
        Unsubscribe(fireplaceIdeaPoint);
        Unsubscribe(edithBulletIdeaPoint);
        Unsubscribe(windowBulletIdeaPoint);
    }

    private void Subscribe(DetectiveIdeaPoint ideaPoint)
    {
        if (ideaPoint == null)
            return;

        ideaPoint.OnDiscovered -= HandleActOneIdeaPointDiscovered;
        ideaPoint.OnDiscovered += HandleActOneIdeaPointDiscovered;
    }

    private void Unsubscribe(DetectiveIdeaPoint ideaPoint)
    {
        if (ideaPoint != null)
            ideaPoint.OnDiscovered -= HandleActOneIdeaPointDiscovered;
    }

    private void SynchronizeActOneIdeaPointProgress()
    {
        if (lightsIdeaPoint != null && lightsIdeaPoint.IsDiscovered)
            MarkLightsFound();
        if (movingTableIdeaPoint != null && movingTableIdeaPoint.IsDiscovered)
            MarkMovingTableFound();
        if (emptyWallIdeaPoint != null && emptyWallIdeaPoint.IsDiscovered)
            MarkEmptyWallFound();
        if (fireplaceIdeaPoint != null && fireplaceIdeaPoint.IsDiscovered)
            MarkFireplaceFound();
        if (edithBulletIdeaPoint != null && edithBulletIdeaPoint.IsDiscovered)
            MarkEdithBulletFound();
        if (windowBulletIdeaPoint != null && windowBulletIdeaPoint.IsDiscovered)
            MarkWindowBulletFound();
    }

    private void HandleActOneIdeaPointDiscovered(DetectiveIdeaPoint discoveredPoint)
    {
        if (discoveredPoint == lightsIdeaPoint)
            MarkLightsFound();
        else if (discoveredPoint == movingTableIdeaPoint)
            MarkMovingTableFound();
        else if (discoveredPoint == emptyWallIdeaPoint)
            MarkEmptyWallFound();
        else if (discoveredPoint == fireplaceIdeaPoint)
            MarkFireplaceFound();
        else if (discoveredPoint == edithBulletIdeaPoint)
            MarkEdithBulletFound();
        else if (discoveredPoint == windowBulletIdeaPoint)
            MarkWindowBulletFound();
    }

    private void MarkIdeaPointFound()
    {
        if (ideaPointFound)
            return;

        ideaPointFound = true;
        ApplyConditionToBothConversations(ideaPointParameterName, true);
    }

    private static void SetConversationBool(NPCConversation conversation, string parameterName, bool value)
    {
        if (conversation == null)
            return;

        string requestedName = parameterName.Trim();
        string resolvedName = requestedName;

        if (conversation.ParameterList == null)
            conversation.DeserializeForEditor();

        if (conversation.ParameterList != null)
        {
            foreach (EditableParameter parameter in conversation.ParameterList)
            {
                if (parameter != null &&
                    string.Equals(parameter.ParameterName?.Trim(), requestedName, StringComparison.Ordinal))
                {
                    resolvedName = parameter.ParameterName;
                    break;
                }
            }
        }

        conversation.SetRuntimeBoolParameter(resolvedName, value);
    }
}
