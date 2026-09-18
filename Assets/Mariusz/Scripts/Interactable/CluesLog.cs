using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aliasy chronia przed 'using System.Diagnostics;', ktory Visual Studio
// dopisuje po wklejeniu kodu i psuje kompilacje (CS0104).
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

/// <summary>
/// Lightweight objective log for the current story flow. It deliberately does
/// not depend on ClueManager, conclusions, or quest ScriptableObjects.
/// </summary>
public class CluesLog : MonoBehaviour
{
    public static CluesLog Instance;

    public event System.Action<string> OnLogUpdated;

    [SerializeField] private TextMeshProUGUI questLogText;

    [Header("Quest Log Rows")]
    [SerializeField] private Transform questLogContent;
    [SerializeField] private TextMeshProUGUI questLogTitleRow;
    [SerializeField] private TextMeshProUGUI questLogObjectiveRow;
    [SerializeField] private TextMeshProUGUI questLogSubObjectiveRow;

    public bool IsQuestLogVisible
    {
        get
        {
            if (questLogContent != null)
                return questLogContent.gameObject.activeSelf;

            return questLogText != null && questLogText.gameObject.activeSelf;
        }
    }

    public void SetQuestLogVisible(bool visible)
    {
        if (questLogContent != null)
            questLogContent.gameObject.SetActive(visible);

        if (questLogText != null &&
            (questLogContent == null || !questLogText.transform.IsChildOf(questLogContent)))
        {
            questLogText.gameObject.SetActive(visible);
        }
    }

    [Header("Text")]
    [SerializeField] private string title = "Kto, jak, dlaczego?";
    private string description;

    [Header("Starting Notebook Notes")]
    [Tooltip("Person notes added to the notebook when a new game starts.")]
    [SerializeField] private NoteData[] startingPersonNotes;

    [Header("Story Descriptions")]
    [SerializeField, TextArea(4, 8)] private string startDescription = "Lady Edith nie żyje. Została zamordowana na naszych oczach podczas spirytystycznego seansu w domu medium, madame Selmy. Gdy zgasły światła, napastnik przemknął przez salon niczym zjawa i zniknął w ciemności. Musimy ustalić, kto pociągnął za spust i dlaczego dokonał tej zbrodni.";
    [SerializeField, TextArea(4, 8)] private string groundFloorIdeasDescription = "Ślady prowadzą ku ścianie salonu. To stamtąd padł strzał, a napastnik musiał wejść przez ukryte przejście. Mechanizm okrągłego stołu otworzył drzwi tuż przed oddaniem strzału.";
    [SerializeField, TextArea(4, 8)] private string easyTableDescription = "Mechanizm ustąpił. Ukryte przejście stoi przed nami otworem, lecz nie wiemy jeszcze, dokąd prowadzi ani kto korzystał z niego przed nami. Każdy krok dalej może przybliżyć nas do zabójcy.";
    [SerializeField, TextArea(4, 8)] private string ethelPassageDescription = "Korytarz kończy się przy kolejnej zamkniętej ścianie, lecz mechanizm wymaga jeszcze jednego elementu. Znalazłem lalkę małej Ethel. Dziewczynka może wiedzieć więcej o tym domu, niż potrafią powiedzieć jego dorośli mieszkańcy. Muszę dostać się na piętro.";
    [SerializeField, TextArea(4, 8)] private string upperFloorDescription = "Muszę odnaleźć małą Ethel. Skoro już jestem na piętrze, przeszukam również pokoje i poszukam śladów, które pozwolą połączyć fakty dotyczące Lady Edith.";
    [SerializeField, TextArea(4, 8)] private string lettersDescription = "Listy rzucają nowe światło na tę sprawę. Madame Selma od lat gromadziła sekrety osób związanych z dworem, a jej seanse były dla wpływowych gości czymś więcej niż niewinną rozrywką. Lady Edith poznała prawdę o swoim pochodzeniu i mogła stać się zagrożeniem dla ludzi, którzy za wszelką cenę pragnęli utrzymać ją w ukryciu. Muszę ustalić, co dokładnie wiedziała Selma i kto próbował ją uciszyć.";
    [SerializeField, TextArea(4, 8)] private string ethelHiddenPassageDescription = "Ethel nie ma na piętrze, lecz w jej pokoju odkryłem kolejne ukryte przejście. Prowadzi w dół, do części domu, której gospodarze najwyraźniej nie chcieli nam pokazać. Muszę sprawdzić, co skrywa piwnica.";
    [SerializeField, TextArea(4, 8)] private string basementDescription = "Dotarliśmy do piwnic domu madame Selmy. Mała Ethel musi być gdzieś w tym labiryncie mechanizmów i zamkniętych przejść. Czujemy, że rozwiązanie tej zagadki jest już blisko.";
    [SerializeField, TextArea(4, 8)] private string basementIdeaPointPuzzleDescription = "Zegar, manekin i tarcza zegara wmurowana w ścianę są częściami jednego mechanizmu. Muszę rozgryźć ich wzajemne powiązania, aby odkryć drogę przez sekretną ścianę.";
    [SerializeField, TextArea(4, 8)] private string controlUnitDescription = "Odkryliśmy serce całej iluzji. Ukryte w piwnicy mechanizmy sterują światłami, dźwiękami i ruchomymi elementami domu, nadając seansom madame Selmy pozór zjawisk nadprzyrodzonych. To dzięki nim Duchy Przeszłości zdają się nawiedzać ten dom. Ktoś doskonale znał ten system i mógł wykorzystać go, by ukryć swoje prawdziwe zamiary wśród chaosu seansu.";
    [SerializeField, TextArea(4, 8)] private string ethelFirstConversationDescription = "Ethel zdradziła, że jej ojcem jest Arthur, działający w tajemnicy przy ochronie Lady Edith. List odsłania prawdę o pochodzeniu Edith: jej ojcem był książę Albert, a tajemnicę przez lata tuszowały służby królewskie. Selma naraziła siebie i swoich bliskich, poznając ten sekret. Pusty pergamin Edith może zawierać kopię dowodów.";
    [SerializeField, TextArea(4, 8)] private string ethelSecondConversationDescription = "Ethel pokazała nam drugie wyjście z piwnicy. To dowód, że do domu madame Selmy można było dostać się niepostrzeżenie, omijając salon i jego mieszkańców. Pokazała nam również tajne przejście prowadzące szybko na górę. Ktoś mógł wykorzystać tę drogę, by pojawić się podczas seansu niczym zjawa, a potem zniknąć bez śladu.";

    [Header("Story Description Audio")]
    [SerializeField] private AudioSource storyDescriptionAudioSource;
    [SerializeField] private AudioClip storyDescriptionChangedClip;

    [Header("Journal Update Notice")]
    [SerializeField] private TextMeshProUGUI noteUpdateLogText;
    [SerializeField] private string storyDescriptionUpdateNotice = "Dziennik został zaktualizowany.";
    [SerializeField, Min(0f)] private float noteUpdateStartDelay = 0.2f;
    [SerializeField, Min(0.01f)] private float noteUpdateFadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float noteUpdateVisibleDuration = 3f;

    [Header("Quest Log Animation")]
    [SerializeField, Min(0.01f)] private float questLogFadeDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugSlowMotionControls;
    [SerializeField, Range(0.01f, 1f)] private float debugSlowMotionTimeScale = 0.1f;
    [SerializeField, Min(1f)] private float debugSlowMotionFadeMultiplier = 8f;
    [SerializeField] private string examineCrimeSceneText = "Zbadaj miejsce zbrodni";
    [SerializeField] private string examineEdithBodyText = "Zbadaj ciało Lady Edith";
    [SerializeField] private string examineRoomText = "Zbadaj pomieszczenie";
    [SerializeField] private string findSecretPassageText = "Odnajdź tajne przejście";
    [SerializeField] private string findSecretDoorOpeningMethodText = "Znajdź sposób na otworzenie tajnych drzwi w ścianie";
    [SerializeField] private string completeTableMechanismText = "Skompletuj brakujące elementy mechanizmu stołu";
    [SerializeField] private string investigateSecretPassageText = "Zbadaj dokąd prowadzi tajne przejście";
    [SerializeField] private string connectionsText = "Dowiedz się więcej o Powiązaniach Lady Edith";
    [SerializeField] private string interviewSessionWitnessesText = "Przesłuchaj świadków biorących udział w sesji";
    [SerializeField] private string askSelmaAboutHenrySpiritText = "Porozmawiaj z Madame Selmą o duchu ojca Lady Edith";
    [SerializeField] private string askArthurAboutFoundItemsText = "Porozmawiaj z Sir Arthurem";
    [SerializeField, Min(1)] private int requiredSessionWitnesses = 5;
    [SerializeField] private string searchUpperFloorEvidenceText = "Przeszukaj piętro w celu zebrania dowodów";
    [SerializeField, Min(1)] private int requiredUpperFloorEvidence = 3;
    [SerializeField] private string searchBasementEvidenceText = "Przeszukaj piwnicę w celu zebrania dowodów";
    [SerializeField, Min(1)] private int requiredBasementEvidence = 4;
    [SerializeField] private string findEthelText = "Odszukaj małą Ethel";
    [SerializeField] private string findEthelUpstairsText = "Odszukaj małą Ethel na górze";
    [SerializeField] private string findWayUpstairsText = "Znajdź sposób, aby dostać się na górę";
    [SerializeField] private string searchUpperFloorText = "Przeszukaj piętro";
    [SerializeField] private string findEthelHiddenDoorText = "Znajdź sposób na otwarcie ukrytych drzwi w pokoju Ethel";
    [SerializeField] private string investigateEthelPassageText = "Zbadaj dokąd prowadzi tajne przejście w pokoju Ethel";
    [SerializeField] private string searchBasementText = "Przeszukaj piwnicę";
    [SerializeField] private string findBasementExitText = "Znajdź sposób, aby wydostać się z pomieszczenia w piwnicy";
    [SerializeField] private string findBasementHiddenDoorText = "Znajdź sposób na otwarcie ukrytych drzwi w piwnicy";
    [SerializeField] private string confrontSessionText = "Skonfrontuj się z uczestnikami sesji";
    [SerializeField] private string followEthelText = "Podążaj za małą Ethel";

    [Header("Crime Scene Progress")]
    [Tooltip("Five ground-floor IdeaPoints: Lamp, Fireplace, Circle Table, Hidden Wall and Window Bullet.")]
    [SerializeField] private DetectiveIdeaPoint[] roomIdeaPoints;
    [SerializeField, Min(1)] private int requiredEdithBodyClues = 3;

    [Header("Basement IdeaPoint Progress")]
    [Tooltip("Assign the four IdeaPoints used by the basement sequence.")]
    [SerializeField] private DetectiveIdeaPoint[] basementIdeaPoints;
    [SerializeField, Min(1)] private int requiredBasementIdeaPoints = 4;

    private bool crimeSceneCompleted;
    private bool crimeSceneVisible = true;
    private bool ideaPointPuzzleSolved;
    private bool tableMechanismObjectiveStarted;
    private bool tableMechanismCompleted;
    private bool secretDoorOpeningPuzzleSolved;
    private bool secretPassageExplored;
    private bool greenTableMechanismElementCollected;
    private bool redTableMechanismElementCollected;
    private bool blueTableMechanismElementCollected;
    private int collectedEdithBodyClues;
    private int discoveredRoomIdeaPoints;
    private int discoveredBasementIdeaPoints;
    private bool basementIdeaPointSearchCompleted;
    private bool connectionsCompleted;
    private bool connectionsVisible = true;
    private int interviewedSessionWitnesses;
    private readonly HashSet<int> interviewedSessionWitnessIds = new();
    private bool sessionWitnessesVisible = true;
    private bool askSelmaAboutHenrySpiritVisible;
    private bool askArthurAboutFoundItemsVisible;
    private bool askedArthurAboutRing;
    private bool askedArthurAboutPaper;
    private bool upperFloorEvidenceVisible;
    private int collectedUpperFloorEvidence;
    private bool basementEvidenceVisible;
    private int collectedBasementEvidence;
    private readonly HashSet<string> collectedBasementEvidenceIds = new();
    private bool findEthelVisible;
    private bool findEthelCompleted;
    private bool findWayUpstairsVisible;
    private bool searchUpperFloorVisible;
    private bool findEthelHiddenDoorVisible;
    private bool investigateEthelPassageVisible;
    private bool searchBasementVisible;
    private bool findBasementExitVisible;
    private bool findBasementHiddenDoorVisible;
    private bool confrontSessionVisible;
    private bool followEthelVisible;
    private float noteUpdateTextAlpha = 1f;
    private Coroutine noteUpdateFadeCoroutine;
    private float questLogTextAlpha = 1f;
    private Coroutine questLogFadeCoroutine;
    private Coroutine questLogRowsTransitionCoroutine;
    private bool hasPendingQuestLogRowsRefresh;
    private bool pendingQuestLogRowsAnimate;
    private bool debugSlowMotionEnabled;
    private float timeScaleBeforeDebugSlowMotion = 1f;
    private readonly List<QuestLogRowView> activeQuestRows = new();

    private sealed class QuestLogRowView
    {
        public string Key;
        public string Text;
        public bool IsProgressCounter;
        public TextMeshProUGUI Label;
        public CanvasGroup CanvasGroup;
    }

    private sealed class QuestLogRowData
    {
        public string Key;
        public string Text;
        public bool IsSubObjective;
        public bool IsProgressCounter;
    }

    public string Title => title;
    public string Description => description;

    private void Awake()
    {
        Instance = this;
        ConfigureJournalUpdateNotice();
        ConfigureQuestLogText();
        ConfigureQuestLogRows();
    }

    private void Start()
    {
        description = startDescription;
        AddStartingPersonNotes();

        SubscribeToRoomIdeaPoints();
        SubscribeToBasementIdeaPoints();
        RefreshRoomIdeaPointProgress(false);
        RefreshBasementIdeaPointProgress(false);
        UpdateLog(false);
    }

    private void AddStartingPersonNotes()
    {
        NotebookManager notebookManager = NotebookManager.Instance;
        if (notebookManager == null || startingPersonNotes == null)
            return;

        foreach (NoteData note in startingPersonNotes)
        {
            if (note == null)
                continue;

            if (note.category != NoteCategory.Osoby)
            {
                Debug.LogWarning($"CluesLog: starting note '{note.name}' is not in the Osoby category.", note);
                continue;
            }

            notebookManager.AddNote(note, showUpdateNotification: false);
        }
    }

    private void Update()
    {
        if (!enableDebugSlowMotionControls)
        {
            if (debugSlowMotionEnabled)
                ToggleDebugSlowMotion();

            return;
        }

        if (Input.GetKeyDown(KeyCode.Y))
            ToggleDebugSlowMotion();
    }

    private void OnDestroy()
    {
        if (debugSlowMotionEnabled)
            Time.timeScale = timeScaleBeforeDebugSlowMotion;

        UnsubscribeFromRoomIdeaPoints();
        UnsubscribeFromBasementIdeaPoints();
    }

    private void ToggleDebugSlowMotion()
    {
        debugSlowMotionEnabled = !debugSlowMotionEnabled;

        if (debugSlowMotionEnabled)
        {
            timeScaleBeforeDebugSlowMotion = Time.timeScale;
            Time.timeScale = debugSlowMotionTimeScale;
            Debug.Log("Quest log debug slow motion: enabled.");
        }
        else
        {
            Time.timeScale = timeScaleBeforeDebugSlowMotion;
            Debug.Log("Quest log debug slow motion: disabled.");
        }
    }

    public void CompleteCrimeSceneInvestigation()
    {
        ideaPointPuzzleSolved = true;
        SetDescription(groundFloorIdeasDescription);
        UpdateLog();
    }

    public void CompleteSecretDoorOpeningPuzzle()
    {
        secretDoorOpeningPuzzleSolved = true;
        SetDescription(easyTableDescription);
        UpdateLog();
    }

    public void StartTableMechanismObjective()
    {
        tableMechanismObjectiveStarted = true;
        SyncTableMechanismProgressWithInventory();
        UpdateLog();
    }

    public void RegisterTableMechanismElement(ItemType itemType)
    {
        bool progressChanged = itemType switch
        {
            ItemType.Zielona when !greenTableMechanismElementCollected =>
                greenTableMechanismElementCollected = true,
            ItemType.Czerwona when !redTableMechanismElementCollected =>
                redTableMechanismElementCollected = true,
            ItemType.Niebieska when !blueTableMechanismElementCollected =>
                blueTableMechanismElementCollected = true,
            _ => false
        };

        if (!progressChanged)
            return;

        if (tableMechanismObjectiveStarted)
            UpdateLog(false);
    }

    public void CompleteTableMechanismObjective()
    {
        tableMechanismCompleted = true;
        UpdateLog();
    }

    public void SetLadyEdithBodyProgress(int collectedClues, int requiredClues)
    {
        requiredEdithBodyClues = Mathf.Max(1, requiredClues);
        collectedEdithBodyClues = Mathf.Clamp(collectedClues, 0, requiredEdithBodyClues);
        RefreshCrimeSceneProgress();
    }

    public void RemoveCrimeSceneObjective()
    {
        crimeSceneVisible = false;
        UpdateLog();
    }

    public void AddFindEthelObjective()
    {
        secretPassageExplored = true;
        crimeSceneVisible = false;
        findEthelVisible = true;
        findWayUpstairsVisible = false;
        UpdateLog();
    }

    public void AddFindEthelUpstairsObjective()
    {
        findEthelVisible = true;
        findWayUpstairsVisible = true;
        UpdateLog();
    }

    public void SetFindEthelUpstairsObjective()
    {
        secretPassageExplored = true;
        crimeSceneVisible = false;
        findEthelVisible = true;
        findWayUpstairsVisible = false;
        SetDescription(ethelPassageDescription);
        UpdateLog();
    }

    public void CompleteFindWayUpstairsObjective()
    {
        findEthelVisible = true;
        findWayUpstairsVisible = false;
        searchUpperFloorVisible = true;
        SetDescription(upperFloorDescription);
        UpdateLog();
    }

    public void AddFindEthelHiddenDoorObjective()
    {
        findEthelVisible = true;
        findEthelHiddenDoorVisible = true;
        UpdateLog();
    }

    public void RemoveFindEthelHiddenDoorObjective()
    {
        findEthelHiddenDoorVisible = false;
        UpdateLog();
    }

    public void CompleteEthelHiddenDoorObjective()
    {
        findEthelVisible = true;
        searchUpperFloorVisible = false;
        findEthelHiddenDoorVisible = false;
        investigateEthelPassageVisible = true;
        SetDescription(ethelHiddenPassageDescription);
        UpdateLog();
    }

    public void AddFindEthelBasementObjective()
    {
        findEthelVisible = true;
        searchBasementVisible = true;
        SetDescription(basementDescription);
        UpdateLog();
    }

    public void RemoveEthelPassageObjective()
    {
        investigateEthelPassageVisible = false;
        UpdateLog();
    }

    public void AddFindBasementExitObjective()
    {
        findEthelVisible = true;
        findBasementExitVisible = true;
        RefreshBasementIdeaPointProgress(false);
        UpdateLog();
    }

    public void AddFindBasementHiddenDoorObjective()
    {
        findEthelVisible = true;
        findBasementExitVisible = false;
        findBasementHiddenDoorVisible = true;
        UpdateLog();
    }

    public void RemoveFindBasementHiddenDoorObjective()
    {
        findBasementHiddenDoorVisible = false;
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

    public void RegisterSessionWitnessInterview(SmartNPC witness)
    {
        if (witness == null || !interviewedSessionWitnessIds.Add(witness.GetInstanceID()))
            return;

        interviewedSessionWitnesses = Mathf.Min(
            interviewedSessionWitnesses + 1,
            requiredSessionWitnesses);
        UpdateLog(false);
    }

    public void BeginUpperFloorEvidenceObjective()
    {
        sessionWitnessesVisible = false;
        upperFloorEvidenceVisible = true;
        UpdateLog();
    }

    public void AddAskSelmaAboutHenrySpiritObjective()
    {
        askSelmaAboutHenrySpiritVisible = true;
        UpdateLog();
    }

    public void RemoveAskSelmaAboutHenrySpiritObjective()
    {
        askSelmaAboutHenrySpiritVisible = false;
        UpdateLog();
    }

    public void AddAskArthurAboutFoundItemsObjective()
    {
        askArthurAboutFoundItemsVisible = !AreArthurFoundItemQuestionsComplete();
        UpdateLog();
    }

    public void RegisterArthurRingQuestion()
    {
        askedArthurAboutRing = true;
        RefreshArthurFoundItemsObjective();
    }

    public void RegisterArthurPaperQuestion()
    {
        askedArthurAboutPaper = true;
        RefreshArthurFoundItemsObjective();
    }

    private void RefreshArthurFoundItemsObjective()
    {
        if (!askArthurAboutFoundItemsVisible)
            askArthurAboutFoundItemsVisible = true;

        if (AreArthurFoundItemQuestionsComplete())
            askArthurAboutFoundItemsVisible = false;

        UpdateLog();
    }

    private bool AreArthurFoundItemQuestionsComplete()
    {
        return askedArthurAboutRing && askedArthurAboutPaper;
    }

    public void RegisterUpperFloorEvidence()
    {
        collectedUpperFloorEvidence = Mathf.Min(
            collectedUpperFloorEvidence + 1,
            requiredUpperFloorEvidence);

        bool allEvidenceCollected = collectedUpperFloorEvidence >= requiredUpperFloorEvidence;
        if (allEvidenceCollected)
            SetDescription(lettersDescription);

        UpdateLog(false);
    }

    public void RemoveUpperFloorEvidenceObjective()
    {
        upperFloorEvidenceVisible = false;
        UpdateLog();
    }

    public void BeginBasementEvidenceObjective()
    {
        upperFloorEvidenceVisible = false;
        basementEvidenceVisible = true;
        UpdateLog();
    }

    public void RegisterBasementEvidence(string evidenceId)
    {
        if (string.IsNullOrWhiteSpace(evidenceId) ||
            !collectedBasementEvidenceIds.Add(evidenceId))
        {
            return;
        }

        collectedBasementEvidence = Mathf.Min(
            collectedBasementEvidence + 1,
            requiredBasementEvidence);
        UpdateLog(false);
    }

    public void CompleteFindEthelObjective()
    {
        findEthelVisible = true;
        findEthelCompleted = true;
        UpdateLog();
    }

    public void RemoveFindEthelObjective()
    {
        findEthelVisible = false;
        findWayUpstairsVisible = false;
        searchUpperFloorVisible = false;
        findEthelHiddenDoorVisible = false;
        investigateEthelPassageVisible = false;
        searchBasementVisible = false;
        findBasementExitVisible = false;
        findBasementHiddenDoorVisible = false;
        UpdateLog();
    }

    public void ReplaceFindEthelWithSessionConfrontation()
    {
        findEthelVisible = false;
        confrontSessionVisible = true;
        UpdateLog();
    }

    public void StartConfrontResidentsObjective()
    {
        findEthelVisible = false;
        findWayUpstairsVisible = false;
        searchUpperFloorVisible = false;
        findEthelHiddenDoorVisible = false;
        investigateEthelPassageVisible = false;
        searchBasementVisible = false;
        findBasementExitVisible = false;
        findBasementHiddenDoorVisible = false;
        confrontSessionVisible = true;
        followEthelVisible = false;
        UpdateLog();
    }

    public void AddFollowEthelObjective()
    {
        confrontSessionVisible = true;
        followEthelVisible = true;
        UpdateLog();
    }

    public void SetEthelFirstConversationDescription()
    {
        SetDescription(ethelFirstConversationDescription);
        UpdateLog();
    }

    public void SetControlUnitDescription()
    {
        SetDescription(controlUnitDescription);
        UpdateLog();
    }

    public void SetBasementIdeaPointPuzzleDescription()
    {
        SetDescription(basementIdeaPointPuzzleDescription);
        UpdateLog();
    }

    public void SetEthelSecondConversationDescription()
    {
        SetDescription(ethelSecondConversationDescription);
        UpdateLog();
    }

    public void UpdateLog(bool animateQuestLog = true)
    {
        string questLog = GetQuestLogText();

        OnLogUpdated?.Invoke(questLog);

        if (HasQuestLogRows())
        {
            RefreshQuestLogRows(animateQuestLog);
            return;
        }

        if (questLogText == null)
            return;

        if (!animateQuestLog || !isActiveAndEnabled)
        {
            StopQuestLogFade();
            questLogText.text = questLog;
            SetQuestLogTextAlpha(1f);
            return;
        }

        StopQuestLogFade();
        questLogFadeCoroutine = StartCoroutine(FadeQuestLog(questLog));
    }

    private void ConfigureQuestLogText()
    {
        if (questLogText != null)
            questLogTextAlpha = questLogText.color.a;
    }

    private void ConfigureQuestLogRows()
    {
        if (!HasQuestLogRows())
            return;

        if (questLogContent == null)
            questLogContent = questLogObjectiveRow.transform.parent;

        questLogTitleRow.gameObject.SetActive(true);
        questLogObjectiveRow.gameObject.SetActive(false);
        questLogSubObjectiveRow.gameObject.SetActive(false);

        if (questLogText != null && questLogText != questLogTitleRow &&
            questLogText != questLogObjectiveRow && questLogText != questLogSubObjectiveRow)
        {
            questLogText.gameObject.SetActive(false);
        }
    }

    private bool HasQuestLogRows()
    {
        return questLogTitleRow != null && questLogObjectiveRow != null &&
               questLogSubObjectiveRow != null;
    }

    private void RefreshQuestLogRows(bool animate)
    {
        List<QuestLogRowData> rows = BuildQuestLogRows();
        questLogTitleRow.text = title;

        if (questLogRowsTransitionCoroutine != null)
        {
            hasPendingQuestLogRowsRefresh = true;
            pendingQuestLogRowsAnimate |= animate;
            return;
        }

        if (!animate)
        {
            ApplyQuestLogRows(rows, false);
            return;
        }

        questLogRowsTransitionCoroutine = StartCoroutine(TransitionQuestLogRows(rows));
    }

    private IEnumerator TransitionQuestLogRows(List<QuestLogRowData> rows)
    {
        List<QuestLogRowView> rowsToFadeOut = new();
        foreach (QuestLogRowView row in activeQuestRows)
        {
            QuestLogRowData data = FindRowData(rows, row.Key);
            if (data == null || row.Text != data.Text)
                rowsToFadeOut.Add(row);
        }

        yield return FadeRows(rowsToFadeOut, 1f, 0f);
        ApplyQuestLogRows(rows, true);

        List<QuestLogRowView> rowsToFadeIn = new();
        foreach (QuestLogRowView row in activeQuestRows)
        {
            QuestLogRowData data = FindRowData(rows, row.Key);
            if (data != null && row.CanvasGroup.alpha <= 0f)
                rowsToFadeIn.Add(row);
        }

        yield return FadeRows(rowsToFadeIn, 0f, 1f);
        questLogRowsTransitionCoroutine = null;

        if (!hasPendingQuestLogRowsRefresh)
            yield break;

        bool animatePendingUpdate = pendingQuestLogRowsAnimate;
        hasPendingQuestLogRowsRefresh = false;
        pendingQuestLogRowsAnimate = false;
        RefreshQuestLogRows(animatePendingUpdate);
    }

    private void ApplyQuestLogRows(List<QuestLogRowData> rows, bool prepareFadeIn)
    {
        for (int index = activeQuestRows.Count - 1; index >= 0; index--)
        {
            QuestLogRowView row = activeQuestRows[index];
            QuestLogRowData data = FindRowData(rows, row.Key);
            if (data == null)
            {
                row.Label.gameObject.SetActive(false);
                Destroy(row.Label.gameObject);
                activeQuestRows.RemoveAt(index);
                continue;
            }

            if (row.Text != data.Text)
            {
                row.Text = data.Text;
                row.Label.text = data.Text;
                SetRowAlpha(row, prepareFadeIn ? 0f : 1f);
            }

            row.IsProgressCounter = data.IsProgressCounter;
        }

        for (int index = 0; index < rows.Count; index++)
        {
            QuestLogRowData data = rows[index];
            QuestLogRowView row = FindRow(data.Key);
            if (row == null)
            {
                row = CreateQuestLogRow(data, prepareFadeIn ? 0f : 1f);
                activeQuestRows.Add(row);
            }

            row.Label.transform.SetSiblingIndex(index + GetRowStartSiblingIndex());
        }

        if (questLogContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private List<QuestLogRowData> BuildQuestLogRows()
    {
        List<QuestLogRowData> rows = new();
        Dictionary<string, int> keyOccurrences = new();
        string[] lines = GetObjectivesText().Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string text = line.Trim();
            bool isSubObjective = line.StartsWith("    - ");
            bool isProgressCounter = TryGetProgressText(text, out string keyText);
            string typeKey = isSubObjective ? "sub:" : "objective:";
            string baseKey = typeKey + (isProgressCounter ? keyText : text)
                .Replace("<s>", string.Empty)
                .Replace("</s>", string.Empty);

            if (!keyOccurrences.TryGetValue(baseKey, out int occurrence))
                occurrence = 0;

            keyOccurrences[baseKey] = occurrence + 1;
            rows.Add(new QuestLogRowData
            {
                Key = $"{baseKey}:{occurrence}",
                Text = text,
                IsSubObjective = isSubObjective,
                IsProgressCounter = isProgressCounter
            });
        }

        return rows;
    }

    private static bool TryGetProgressText(string text, out string textWithoutProgress)
    {
        textWithoutProgress = text;
        int slashIndex = text.LastIndexOf('/');
        if (slashIndex <= 0 || slashIndex >= text.Length - 1)
            return false;

        int denominatorStart = slashIndex + 1;
        for (int index = denominatorStart; index < text.Length; index++)
        {
            if (!char.IsDigit(text[index]))
                return false;
        }

        int numeratorStart = slashIndex - 1;
        while (numeratorStart >= 0 && char.IsDigit(text[numeratorStart]))
            numeratorStart--;

        if (numeratorStart < 0 || numeratorStart == slashIndex - 1 || text[numeratorStart] != ' ')
            return false;

        textWithoutProgress = text.Substring(0, numeratorStart);
        return true;
    }

    private QuestLogRowView CreateQuestLogRow(QuestLogRowData data, float alpha)
    {
        TextMeshProUGUI template = data.IsSubObjective ? questLogSubObjectiveRow : questLogObjectiveRow;
        TextMeshProUGUI label = Instantiate(template, questLogContent);

        CanvasGroup canvasGroup = label.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = label.gameObject.AddComponent<CanvasGroup>();

        QuestLogRowView row = new()
        {
            Key = data.Key,
            Text = data.Text,
            IsProgressCounter = data.IsProgressCounter,
            Label = label,
            CanvasGroup = canvasGroup
        };

        SetRowAlpha(row, alpha);
        label.text = data.Text;
        label.gameObject.SetActive(true);

        return row;
    }

    private QuestLogRowView FindRow(string key)
    {
        foreach (QuestLogRowView row in activeQuestRows)
        {
            if (row.Key == key)
                return row;
        }

        return null;
    }

    private static QuestLogRowData FindRowData(List<QuestLogRowData> rows, string key)
    {
        foreach (QuestLogRowData row in rows)
        {
            if (row.Key == key)
                return row;
        }

        return null;
    }

    private int GetRowStartSiblingIndex()
    {
        return questLogTitleRow.transform.parent == questLogContent ? 1 : 0;
    }

    private IEnumerator FadeRows(List<QuestLogRowView> rows, float from, float to)
    {
        if (rows.Count == 0)
            yield break;

        float duration = GetQuestLogFadeDuration();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            foreach (QuestLogRowView row in rows)
                SetRowAlpha(row, alpha);

            yield return null;
        }

        foreach (QuestLogRowView row in rows)
            SetRowAlpha(row, to);
    }

    private float GetQuestLogFadeDuration()
    {
        return questLogFadeDuration * (debugSlowMotionEnabled ? debugSlowMotionFadeMultiplier : 1f);
    }

    private static void SetRowAlpha(QuestLogRowView row, float alpha)
    {
        if (row.CanvasGroup != null)
            row.CanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void StopQuestLogFade()
    {
        if (questLogFadeCoroutine != null)
        {
            StopCoroutine(questLogFadeCoroutine);
            questLogFadeCoroutine = null;
        }
    }

    private IEnumerator FadeQuestLog(string updatedQuestLog)
    {
        yield return FadeQuestLogTextAlpha(1f, 0f);
        questLogText.text = updatedQuestLog;
        yield return FadeQuestLogTextAlpha(0f, 1f);
        questLogFadeCoroutine = null;
    }

    private IEnumerator FadeQuestLogTextAlpha(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < questLogFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetQuestLogTextAlpha(Mathf.Lerp(from, to, elapsed / questLogFadeDuration));
            yield return null;
        }

        SetQuestLogTextAlpha(to);
    }

    private void SetQuestLogTextAlpha(float alpha)
    {
        if (questLogText == null)
            return;

        Color color = questLogText.color;
        color.a = questLogTextAlpha * Mathf.Clamp01(alpha);
        questLogText.color = color;
    }

    private void ConfigureJournalUpdateNotice()
    {
        if (noteUpdateLogText == null)
            return;

        noteUpdateTextAlpha = noteUpdateLogText.color.a;
        SetJournalUpdateNoticeAlpha(0f);
        noteUpdateLogText.gameObject.SetActive(false);
    }

    private void ShowJournalUpdateNotice()
    {
        ShowJournalUpdateNotice(storyDescriptionUpdateNotice);
    }

    public void ShowJournalUpdateNotice(string message, bool playAudio = true)
    {
        if (noteUpdateLogText == null)
            return;

        noteUpdateLogText.gameObject.SetActive(true);

        if (!string.IsNullOrWhiteSpace(message))
            noteUpdateLogText.text = message;

        if (noteUpdateFadeCoroutine != null)
            StopCoroutine(noteUpdateFadeCoroutine);

        SetJournalUpdateNoticeAlpha(0f);
        noteUpdateFadeCoroutine = StartCoroutine(FadeJournalUpdateNotice(playAudio));
    }

    private IEnumerator FadeJournalUpdateNotice(bool playAudio)
    {
        if (noteUpdateStartDelay > 0f)
            yield return new WaitForSecondsRealtime(noteUpdateStartDelay);

        if (playAudio && storyDescriptionAudioSource != null && storyDescriptionChangedClip != null)
            storyDescriptionAudioSource.PlayOneShot(storyDescriptionChangedClip);

        yield return FadeJournalUpdateNoticeAlpha(0f, 1f);

        if (noteUpdateVisibleDuration > 0f)
            yield return new WaitForSecondsRealtime(noteUpdateVisibleDuration);

        yield return FadeJournalUpdateNoticeAlpha(1f, 0f);
        noteUpdateFadeCoroutine = null;

        if (noteUpdateLogText != null)
            noteUpdateLogText.gameObject.SetActive(false);
    }

    private IEnumerator FadeJournalUpdateNoticeAlpha(float from, float to)
    {
        if (noteUpdateFadeDuration <= 0f)
        {
            SetJournalUpdateNoticeAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < noteUpdateFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetJournalUpdateNoticeAlpha(Mathf.Lerp(from, to, elapsed / noteUpdateFadeDuration));
            yield return null;
        }

        SetJournalUpdateNoticeAlpha(to);
    }

    private void SetJournalUpdateNoticeAlpha(float alpha)
    {
        if (noteUpdateLogText == null)
            return;

        Color color = noteUpdateLogText.color;
        color.a = noteUpdateTextAlpha * Mathf.Clamp01(alpha);
        noteUpdateLogText.color = color;
    }

    private void SetDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || description == value)
            return;

        description = value;

        ShowJournalUpdateNotice();
    }

    public string GetFormattedLog()
    {
        return GetQuestLogText();
    }

    public string GetQuestLogText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<size=120%><u>{title}</u></size>");

        builder.Append(GetObjectivesText());

        return builder.ToString();
    }

    public string GetObjectivesText()
    {
        StringBuilder builder = new StringBuilder();

        if (crimeSceneVisible)
            AppendCrimeSceneObjectives(builder);

        if (findEthelVisible)
        {
            string currentFindEthelText = findWayUpstairsVisible ? findEthelUpstairsText : findEthelText;
            AppendObjective(builder, currentFindEthelText, true, findEthelCompleted);

            if (findWayUpstairsVisible)
                AppendNestedObjective(builder, findWayUpstairsText, false);

            if (searchUpperFloorVisible)
                AppendNestedObjective(builder, searchUpperFloorText, false);

            if (findEthelHiddenDoorVisible)
                AppendNestedObjective(builder, findEthelHiddenDoorText, false);

            if (investigateEthelPassageVisible)
                AppendNestedObjective(builder, investigateEthelPassageText, false);

            if (searchBasementVisible)
                AppendNestedObjective(builder, searchBasementText, false);

            if (findBasementExitVisible)
            {
                AppendNestedObjective(
                    builder,
                    $"{findBasementExitText} {discoveredBasementIdeaPoints}/{requiredBasementIdeaPoints}",
                    false);
            }

            if (findBasementHiddenDoorVisible)
                AppendNestedObjective(builder, findBasementHiddenDoorText, false);
        }

        if (confrontSessionVisible)
        {
            AppendObjective(builder, confrontSessionText, false, false);

            if (followEthelVisible)
                AppendNestedObjective(builder, followEthelText, false);
        }

        // The Lady Edith connections are a separate story thread and always stay last.
        if (connectionsVisible)
        {
            AppendObjective(builder, connectionsText, true, connectionsCompleted);
            if (sessionWitnessesVisible)
            {
                AppendNestedObjective(
                    builder,
                    $"{interviewSessionWitnessesText} {interviewedSessionWitnesses}/{requiredSessionWitnesses}",
                    false);
            }

            if (askSelmaAboutHenrySpiritVisible)
                AppendNestedObjective(builder, askSelmaAboutHenrySpiritText, false);

            if (askArthurAboutFoundItemsVisible)
            {
                int askedItemsCount = (askedArthurAboutRing ? 1 : 0) +
                                      (askedArthurAboutPaper ? 1 : 0);
                AppendNestedObjective(
                    builder,
                    $"{askArthurAboutFoundItemsText} {askedItemsCount}/2",
                    false);
            }

            if (upperFloorEvidenceVisible)
            {
                AppendNestedObjective(
                    builder,
                    $"{searchUpperFloorEvidenceText} {collectedUpperFloorEvidence}/{requiredUpperFloorEvidence}",
                    false);
            }

            if (basementEvidenceVisible)
            {
                AppendNestedObjective(
                    builder,
                    $"{searchBasementEvidenceText} {collectedBasementEvidence}/{requiredBasementEvidence}",
                    false);
            }
        }

        return builder.ToString();
    }

    private void AppendCrimeSceneObjectives(StringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(examineCrimeSceneText))
            return;

        builder.AppendLine($" • {examineCrimeSceneText}");

        if (crimeSceneCompleted)
        {
            if (!ideaPointPuzzleSolved)
                AppendNestedObjective(builder, findSecretPassageText, false);
            else if (secretDoorOpeningPuzzleSolved)
            {
                if (!secretPassageExplored)
                    AppendNestedObjective(builder, investigateSecretPassageText, false);
            }
            else
            {
                AppendNestedObjective(builder, findSecretDoorOpeningMethodText, false);

                if (tableMechanismObjectiveStarted && !tableMechanismCompleted)
                {
                    AppendNestedObjective(
                        builder,
                        $"{completeTableMechanismText} {GetCollectedTableMechanismElementCount()}/3",
                        false);
                }
            }

            return;
        }

        AppendNestedObjective(
            builder,
            $"{examineEdithBodyText} {collectedEdithBodyClues}/{requiredEdithBodyClues}",
            false);
        AppendNestedObjective(
            builder,
            $"{examineRoomText} {discoveredRoomIdeaPoints}/{GetRoomIdeaPointTarget()}",
            false);
    }

    private static void AppendNestedObjective(StringBuilder builder, string text, bool completed)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        builder.AppendLine(completed ? $"    - <s>{text}</s>" : $"    - {text}");
    }

    private void SubscribeToRoomIdeaPoints()
    {
        if (roomIdeaPoints == null)
            return;

        foreach (DetectiveIdeaPoint point in roomIdeaPoints)
        {
            if (point != null)
                point.OnDiscovered += HandleRoomIdeaPointDiscovered;
        }
    }

    private void UnsubscribeFromRoomIdeaPoints()
    {
        if (roomIdeaPoints == null)
            return;

        foreach (DetectiveIdeaPoint point in roomIdeaPoints)
        {
            if (point != null)
                point.OnDiscovered -= HandleRoomIdeaPointDiscovered;
        }
    }

    private void HandleRoomIdeaPointDiscovered(DetectiveIdeaPoint _)
    {
        RefreshRoomIdeaPointProgress();
    }

    private void SubscribeToBasementIdeaPoints()
    {
        if (basementIdeaPoints == null)
            return;

        foreach (DetectiveIdeaPoint point in basementIdeaPoints)
        {
            if (point != null)
                point.OnDiscovered += HandleBasementIdeaPointDiscovered;
        }
    }

    private void UnsubscribeFromBasementIdeaPoints()
    {
        if (basementIdeaPoints == null)
            return;

        foreach (DetectiveIdeaPoint point in basementIdeaPoints)
        {
            if (point != null)
                point.OnDiscovered -= HandleBasementIdeaPointDiscovered;
        }
    }

    private void HandleBasementIdeaPointDiscovered(DetectiveIdeaPoint _)
    {
        RefreshBasementIdeaPointProgress();
    }

    private void RefreshRoomIdeaPointProgress(bool animateOnCompletion = true)
    {
        int discovered = 0;

        if (roomIdeaPoints != null)
        {
            foreach (DetectiveIdeaPoint point in roomIdeaPoints)
            {
                if (point != null && point.IsDiscovered)
                    discovered++;
            }
        }

        discoveredRoomIdeaPoints = Mathf.Clamp(discovered, 0, GetRoomIdeaPointTarget());
        RefreshCrimeSceneProgress(animateOnCompletion);
    }

    private int GetRoomIdeaPointTarget()
    {
        return roomIdeaPoints != null && roomIdeaPoints.Length > 0 ? roomIdeaPoints.Length : 5;
    }

    private void RefreshBasementIdeaPointProgress(bool animateOnCompletion = true)
    {
        int discovered = 0;

        if (basementIdeaPoints != null)
        {
            foreach (DetectiveIdeaPoint point in basementIdeaPoints)
            {
                if (point != null && point.IsDiscovered)
                    discovered++;
            }
        }

        bool wasCompleted = basementIdeaPointSearchCompleted;
        discoveredBasementIdeaPoints = Mathf.Clamp(discovered, 0, requiredBasementIdeaPoints);
        basementIdeaPointSearchCompleted = discoveredBasementIdeaPoints >= requiredBasementIdeaPoints;
        if (basementIdeaPointSearchCompleted)
            findBasementExitVisible = false;

        UpdateLog(animateOnCompletion && !wasCompleted && basementIdeaPointSearchCompleted);
    }

    private void RefreshCrimeSceneProgress(bool animateOnCompletion = true)
    {
        bool wasCompleted = crimeSceneCompleted;
        bool allBodyCluesCollected = collectedEdithBodyClues >= requiredEdithBodyClues;
        bool allRoomIdeaPointsDiscovered = discoveredRoomIdeaPoints >= GetRoomIdeaPointTarget();

        if (allBodyCluesCollected && allRoomIdeaPointsDiscovered)
        {
            crimeSceneCompleted = true;
        }

        UpdateLog(animateOnCompletion && !wasCompleted && crimeSceneCompleted);
    }

    private void SyncTableMechanismProgressWithInventory()
    {
        if (InventoryManager.Instance == null || InventoryManager.Instance.items == null)
            return;

        if (InventoryManager.Instance.items.Contains(ItemType.Zielona))
            greenTableMechanismElementCollected = true;
        if (InventoryManager.Instance.items.Contains(ItemType.Czerwona))
            redTableMechanismElementCollected = true;
        if (InventoryManager.Instance.items.Contains(ItemType.Niebieska))
            blueTableMechanismElementCollected = true;

    }

    private int GetCollectedTableMechanismElementCount()
    {
        int count = 0;

        if (greenTableMechanismElementCollected)
            count++;
        if (redTableMechanismElementCollected)
            count++;
        if (blueTableMechanismElementCollected)
            count++;

        return count;
    }

    private static void AppendObjective(StringBuilder builder, string text, bool canComplete, bool completed)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (canComplete && completed)
            builder.AppendLine($" • <s>{text}</s>");
        else
            builder.AppendLine($" • {text}");
    }

    // ---------------------------------------------------------------
    // SYSTEM ZAPISU - postep celow w panelu zadan
    // ---------------------------------------------------------------

    // Pakuje caly postep panelu do jednego obiektu (dla SaveLoadManager).
    public CluesLogSaveData GetSaveState()
    {
        CluesLogSaveData d = new CluesLogSaveData();

        d.crimeSceneCompleted = crimeSceneCompleted;
        d.crimeSceneVisible = crimeSceneVisible;
        d.ideaPointPuzzleSolved = ideaPointPuzzleSolved;
        d.tableMechanismObjectiveStarted = tableMechanismObjectiveStarted;
        d.tableMechanismCompleted = tableMechanismCompleted;
        d.secretDoorOpeningPuzzleSolved = secretDoorOpeningPuzzleSolved;
        d.secretPassageExplored = secretPassageExplored;
        d.greenTableMechanismElementCollected = greenTableMechanismElementCollected;
        d.redTableMechanismElementCollected = redTableMechanismElementCollected;
        d.blueTableMechanismElementCollected = blueTableMechanismElementCollected;
        d.basementIdeaPointSearchCompleted = basementIdeaPointSearchCompleted;
        d.connectionsCompleted = connectionsCompleted;
        d.connectionsVisible = connectionsVisible;
        d.sessionWitnessesVisible = sessionWitnessesVisible;
        d.askSelmaAboutHenrySpiritVisible = askSelmaAboutHenrySpiritVisible;
        d.askArthurAboutFoundItemsVisible = askArthurAboutFoundItemsVisible;
        d.askedArthurAboutRing = askedArthurAboutRing;
        d.askedArthurAboutPaper = askedArthurAboutPaper;
        d.upperFloorEvidenceVisible = upperFloorEvidenceVisible;
        d.basementEvidenceVisible = basementEvidenceVisible;
        d.findEthelVisible = findEthelVisible;
        d.findEthelCompleted = findEthelCompleted;
        d.findWayUpstairsVisible = findWayUpstairsVisible;
        d.searchUpperFloorVisible = searchUpperFloorVisible;
        d.findEthelHiddenDoorVisible = findEthelHiddenDoorVisible;
        d.investigateEthelPassageVisible = investigateEthelPassageVisible;
        d.searchBasementVisible = searchBasementVisible;
        d.findBasementExitVisible = findBasementExitVisible;
        d.findBasementHiddenDoorVisible = findBasementHiddenDoorVisible;
        d.confrontSessionVisible = confrontSessionVisible;
        d.followEthelVisible = followEthelVisible;
        d.collectedEdithBodyClues = collectedEdithBodyClues;
        d.discoveredRoomIdeaPoints = discoveredRoomIdeaPoints;
        d.discoveredBasementIdeaPoints = discoveredBasementIdeaPoints;
        d.interviewedSessionWitnesses = interviewedSessionWitnesses;
        d.collectedUpperFloorEvidence = collectedUpperFloorEvidence;
        d.collectedBasementEvidence = collectedBasementEvidence;

        d.interviewedSessionWitnessIds = new List<int>(interviewedSessionWitnessIds);
        d.collectedBasementEvidenceIds = new List<string>(collectedBasementEvidenceIds);

        return d;
    }

    // Przywraca postep panelu z zapisu i odswieza tekst na ekranie.
    public void RestoreSaveState(CluesLogSaveData d)
    {
        if (d == null)
        {
            Debug.LogWarning("CluesLog: brak danych panelu zadan w zapisie - zostawiam stan startowy.");
            return;
        }

        crimeSceneCompleted = d.crimeSceneCompleted;
        crimeSceneVisible = d.crimeSceneVisible;
        ideaPointPuzzleSolved = d.ideaPointPuzzleSolved;
        tableMechanismObjectiveStarted = d.tableMechanismObjectiveStarted;
        tableMechanismCompleted = d.tableMechanismCompleted;
        secretDoorOpeningPuzzleSolved = d.secretDoorOpeningPuzzleSolved;
        secretPassageExplored = d.secretPassageExplored;
        greenTableMechanismElementCollected = d.greenTableMechanismElementCollected;
        redTableMechanismElementCollected = d.redTableMechanismElementCollected;
        blueTableMechanismElementCollected = d.blueTableMechanismElementCollected;
        basementIdeaPointSearchCompleted = d.basementIdeaPointSearchCompleted;
        connectionsCompleted = d.connectionsCompleted;
        connectionsVisible = d.connectionsVisible;
        sessionWitnessesVisible = d.sessionWitnessesVisible;
        askSelmaAboutHenrySpiritVisible = d.askSelmaAboutHenrySpiritVisible;
        askArthurAboutFoundItemsVisible = d.askArthurAboutFoundItemsVisible;
        askedArthurAboutRing = d.askedArthurAboutRing;
        askedArthurAboutPaper = d.askedArthurAboutPaper;
        upperFloorEvidenceVisible = d.upperFloorEvidenceVisible;
        basementEvidenceVisible = d.basementEvidenceVisible;
        findEthelVisible = d.findEthelVisible;
        findEthelCompleted = d.findEthelCompleted;
        findWayUpstairsVisible = d.findWayUpstairsVisible;
        searchUpperFloorVisible = d.searchUpperFloorVisible;
        findEthelHiddenDoorVisible = d.findEthelHiddenDoorVisible;
        investigateEthelPassageVisible = d.investigateEthelPassageVisible;
        searchBasementVisible = d.searchBasementVisible;
        findBasementExitVisible = d.findBasementExitVisible;
        findBasementHiddenDoorVisible = d.findBasementHiddenDoorVisible;
        confrontSessionVisible = d.confrontSessionVisible;
        followEthelVisible = d.followEthelVisible;
        collectedEdithBodyClues = d.collectedEdithBodyClues;
        discoveredRoomIdeaPoints = d.discoveredRoomIdeaPoints;
        discoveredBasementIdeaPoints = d.discoveredBasementIdeaPoints;
        interviewedSessionWitnesses = d.interviewedSessionWitnesses;
        collectedUpperFloorEvidence = d.collectedUpperFloorEvidence;
        collectedBasementEvidence = d.collectedBasementEvidence;

        interviewedSessionWitnessIds.Clear();
        if (d.interviewedSessionWitnessIds != null)
        {
            foreach (int id in d.interviewedSessionWitnessIds)
                interviewedSessionWitnessIds.Add(id);
        }

        collectedBasementEvidenceIds.Clear();
        if (d.collectedBasementEvidenceIds != null)
        {
            foreach (string id in d.collectedBasementEvidenceIds)
                collectedBasementEvidenceIds.Add(id);
        }

        Debug.Log("CluesLog: przywrocono postep panelu zadan (cialo Edith: " + collectedEdithBodyClues +
                  ", pomieszczenia: " + discoveredRoomIdeaPoints +
                  ", swiadkowie: " + interviewedSessionWitnesses + ").");

        // Pasek zadan wlacza normalnie dialog startowy. Po wczytaniu zapisu
        // ten dialog sie nie odtwarza, wiec pokazujemy panel wprost.
        SetQuestLogVisible(true);

        UpdateLog(false);
    }
}
