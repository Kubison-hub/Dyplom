using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BasementIdeaPointPuzzle : MonoBehaviour
{
    public static BasementIdeaPointPuzzle Instance { get; private set; }

    [Header("Puzzle")]
    [SerializeField] private DetectiveSequencePuzzle sequencePuzzle;
    [SerializeField] private DetectiveIdeaPoint[] requiredIdeaPoints;
    [SerializeField] private bool configureConnectionsOnStart = true;

    [Header("Solved Vision")]
    [SerializeField] private GameObject secretPassageVision;

    [Header("Sequence Start")]
    [SerializeField] private PlayerController sherlock;
    [SerializeField] private Transform puzzlePosition;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1f;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.1f;
    [SerializeField, Min(0.1f)] private float rotationSpeed = 300f;
    [Header("Watson Sequence Position")]
    [SerializeField] private PlayerController watson;
    [SerializeField] private Transform watsonPuzzlePosition;

    [Header("Eagle Vision")]
    [SerializeField, Min(0.05f)] private float forcedVisionRefreshDuration = 0.25f;

    private bool sequenceStarted;
    private bool forceVision;
    private Coroutine startSequenceCoroutine;
    private Coroutine watsonMoveCoroutine;

    public bool KeepsEagleVisionActive => forceVision && !IsPuzzleSolved();
    public bool IsSolved => IsPuzzleSolved();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    private void Start()
    {
        if (configureConnectionsOnStart)
            ConfigureBasementConnections();

        SubscribeToIdeaPoints();

        if (sequencePuzzle != null)
            sequencePuzzle.onSolved.AddListener(HandlePuzzleSolved);

        if (DetectiveIdeaManager.Instance != null)
        {
            DetectiveIdeaManager.Instance.BeginPuzzleSession(sequencePuzzle, true, false);
            DetectiveIdeaManager.Instance.OnEmptyVisionClick += TryExitForcedVision;
        }

        TryStartSequenceWhenReady();
    }

    private void Update()
    {
        if (forceVision && !IsPuzzleSolved())
            EagleVisionSystem.Instance?.HoldVisionFor(forcedVisionRefreshDuration);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromIdeaPoints();

        if (DetectiveIdeaManager.Instance != null)
            DetectiveIdeaManager.Instance.OnEmptyVisionClick -= TryExitForcedVision;

        if (sequencePuzzle != null)
            sequencePuzzle.onSolved.RemoveListener(HandlePuzzleSolved);
    }

    private void SubscribeToIdeaPoints()
    {
        if (requiredIdeaPoints == null)
            return;

        foreach (DetectiveIdeaPoint point in requiredIdeaPoints)
        {
            if (point != null)
                point.OnDiscovered += HandleIdeaPointDiscovered;
        }
    }

    private void UnsubscribeFromIdeaPoints()
    {
        if (requiredIdeaPoints == null)
            return;

        foreach (DetectiveIdeaPoint point in requiredIdeaPoints)
        {
            if (point != null)
                point.OnDiscovered -= HandleIdeaPointDiscovered;
        }
    }

    private void HandleIdeaPointDiscovered(DetectiveIdeaPoint point)
    {
        TryStartSequenceWhenReady();
    }

    private void TryStartSequenceWhenReady()
    {
        if (sequenceStarted || !AreAllIdeaPointsDiscovered())
            return;

        sequenceStarted = true;
        startSequenceCoroutine = StartCoroutine(MoveSherlockAndStartSequence());
    }

    private bool AreAllIdeaPointsDiscovered()
    {
        if (requiredIdeaPoints == null || requiredIdeaPoints.Length == 0)
            return false;

        foreach (DetectiveIdeaPoint point in requiredIdeaPoints)
        {
            if (point == null || !point.IsDiscovered)
                return false;
        }

        return true;
    }

    private IEnumerator MoveSherlockAndStartSequence()
    {
        if (sherlock == null || puzzlePosition == null || sherlock.navMeshAgent == null)
        {
            Debug.LogWarning("BasementIdeaPointPuzzle: Assign Sherlock, his NavMeshAgent and Puzzle Position.", this);
            EnableConnectionsAndVision();
            yield break;
        }

        if (SwitchCharacter.Instance != null && SwitchCharacter.Instance.canSwitch)
            SwitchCharacter.Instance.SetActivePlayer(0);

        NavMeshAgent agent = sherlock.navMeshAgent;
        sherlock.LockMovement();

        if (!agent.isOnNavMesh ||
            !NavMesh.SamplePosition(puzzlePosition.position, out NavMeshHit destination, navMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning("BasementIdeaPointPuzzle: Puzzle Position is not on the NavMesh.", puzzlePosition);
            sherlock.UnlockMovement();
            EnableConnectionsAndVision();
            yield break;
        }

        StartWatsonMoveToPuzzlePosition();

        agent.isStopped = false;
        agent.SetDestination(destination.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.hasPath && agent.remainingDistance > Mathf.Max(arrivalDistance, agent.stoppingDistance))
                yield return null;
        }
        else
        {
            Debug.LogWarning("BasementIdeaPointPuzzle: Sherlock cannot reach Puzzle Position.", puzzlePosition);
        }

        agent.ResetPath();
        agent.updateRotation = false;

        Quaternion startRotation = sherlock.transform.rotation;
        Quaternion targetRotation = puzzlePosition.rotation;
        while (Quaternion.Angle(sherlock.transform.rotation, targetRotation) > 0.2f)
        {
            sherlock.transform.rotation = Quaternion.RotateTowards(
                sherlock.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
            yield return null;
        }

        sherlock.transform.rotation = targetRotation;
        agent.updateRotation = true;

        while (watsonMoveCoroutine != null)
            yield return null;

        sherlock.UnlockMovement();
        EnableConnectionsAndVision();
    }

    private void StartWatsonMoveToPuzzlePosition()
    {
        if (watsonMoveCoroutine != null || watsonPuzzlePosition == null)
            return;

        watson = ResolveWatson();
        if (watson == null)
        {
            Debug.LogWarning("BasementIdeaPointPuzzle: Assign Watson or add him to SwitchCharacter players.", this);
            return;
        }

        watsonMoveCoroutine = StartCoroutine(MoveWatsonToPuzzlePosition());
    }

    private IEnumerator MoveWatsonToPuzzlePosition()
    {
        NavMeshAgent agent = watson != null ? watson.navMeshAgent : null;
        if (agent == null || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(watsonPuzzlePosition.position, out NavMeshHit destination, navMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning("BasementIdeaPointPuzzle: Watson Puzzle Position is not on the NavMesh.", watsonPuzzlePosition);
            watsonMoveCoroutine = null;
            yield break;
        }

        agent.updateRotation = true;
        agent.isStopped = false;
        agent.SetDestination(destination.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.hasPath && agent.remainingDistance > Mathf.Max(arrivalDistance, agent.stoppingDistance))
                yield return null;
        }
        else
        {
            Debug.LogWarning("BasementIdeaPointPuzzle: Watson cannot reach Watson Puzzle Position.", watsonPuzzlePosition);
        }

        agent.ResetPath();
        watsonMoveCoroutine = null;
    }

    private PlayerController ResolveWatson()
    {
        if (watson != null)
            return watson;

        if (SwitchCharacter.Instance == null || SwitchCharacter.Instance.players == null)
            return null;

        foreach (var playerInput in SwitchCharacter.Instance.players)
        {
            if (playerInput == null)
                continue;

            PlayerController player = playerInput.GetComponent<PlayerController>();
            if (player != null && player.playerCharacter == PlayerCharacter.Watson)
                return player;
        }

        return null;
    }

    private void EnableConnectionsAndVision()
    {
        DetectiveIdeaManager.Instance?.SetConnectionsEnabled(true);
        DetectiveIdeaManager.Instance?.SetPuzzleCompletionEnabled(true);
        forceVision = true;
        startSequenceCoroutine = null;
    }

    private void HandlePuzzleSolved()
    {
        forceVision = false;
        CluesLog.Instance?.AddFindBasementHiddenDoorObjective();
        CluesLog.Instance?.SetBasementIdeaPointPuzzleDescription();

        if (secretPassageVision != null)
            secretPassageVision.SetActive(true);

        Debug.Log("BASEMENT_PUZZLE_COMPLETE");
    }

    private bool TryExitForcedVision()
    {
        if (!forceVision || IsPuzzleSolved())
            return false;

        forceVision = false;
        return true;
    }

    private bool IsPuzzleSolved()
    {
        return sequencePuzzle != null && sequencePuzzle.IsSolved;
    }

    [ContextMenu("Configure Basement Connections")]
    public void ConfigureBasementConnections()
    {
        if (sequencePuzzle == null)
            sequencePuzzle = GetComponent<DetectiveSequencePuzzle>();

        DetectiveIdeaPoint oldClock = FindPoint("OldClock");
        DetectiveIdeaPoint brickClock = FindPoint("BrickClock");
        DetectiveIdeaPoint mannequin = FindPoint("Manequine");
        DetectiveIdeaPoint trapDoor = FindPoint("TrapDoor");
        DetectiveIdeaPoint secretDoor = FindPoint("SecretDoor");

        if (sequencePuzzle == null || oldClock == null || brickClock == null || mannequin == null ||
            trapDoor == null || secretDoor == null)
        {
            Debug.LogWarning(
                "BasementIdeaPointPuzzle: Assign points with IDs containing OldClock, BrickClock, Manequine, TrapDoor and SecretDoor.",
                this);
            return;
        }

        sequencePuzzle.correctSequence = new List<DetectiveSequencePuzzle.IdeaConnectionStep>
        {
            new DetectiveSequencePuzzle.IdeaConnectionStep { from = oldClock, to = brickClock },
            new DetectiveSequencePuzzle.IdeaConnectionStep { from = brickClock, to = mannequin },
            new DetectiveSequencePuzzle.IdeaConnectionStep { from = mannequin, to = secretDoor }
        };

        // Every direction has an observation. Only the three sequence steps advance the puzzle.
        ConfigureConnection(oldClock, brickClock,
            "Stary zegar uruchamia ten w ścianie.",
            "Dwa zegary milcza razem, Watsonie.", canConnect: true);
        ConfigureConnection(brickClock, oldClock,
            "Sygnał biegnie od starego zegara.",
            "Odpowiedz kryje sie w starszym z zegarow.");

        ConfigureConnection(oldClock, mannequin,
            "Między zegarem a manekinem jest ogniwo.",
            "Manekin nie poruszy sie od samego wspomnienia czasu.");
        ConfigureConnection(mannequin, oldClock,
            "Manekin nie steruje starym zegarem.",
            "Stary zegar wciaz pilnuje tej tajemnicy.");

        ConfigureConnection(oldClock, trapDoor,
            "Zegar uruchomił pułapkę pośrednio.",
            "Czas i pulapka maja wspolny, ponury rytm.");
        ConfigureConnection(trapDoor, oldClock,
            "Pułapka zadziałała po uderzeniu zegara.",
            "To nie byl zwykly dzwon zegara.");

        ConfigureConnection(oldClock, secretDoor,
            "Sam zegar nie otwiera przejścia.",
            "Wyjscie nie otworzy sie bez wlasciwego momentu.");
        ConfigureConnection(secretDoor, oldClock,
            "Przejście potrzebuje kolejnego sygnału.",
            "Sciana pamieta, kiedy zegar jeszcze dzialal.");

        ConfigureConnection(brickClock, mannequin,
            "Zegar w ścianie steruje manekinem.",
            "Wskazowki i ruch manekina naleza do jednego mechanizmu.",
            canConnect: true,
            dependencyFirst: oldClock,
            dependencySecond: brickClock,
            lockedDescription: "Najpierw połącz oba zegary.");
        ConfigureConnection(mannequin, brickClock,
            "To zegar steruje manekinem.",
            "Kamienna tarcza nadal przyciaga jego uwage.");

        ConfigureConnection(brickClock, trapDoor,
            "Zegar może też sterować pułapką.",
            "Zegar w murze nadaje pulapce jej rytm.");
        ConfigureConnection(trapDoor, brickClock,
            "Pułapka reaguje na mechanizm zegara.",
            "Kamienny zegar i pulapka pracuja razem.");

        ConfigureConnection(brickClock, secretDoor,
            "Między zegarem a przejściem jest manekin.",
            "Zegar w murze nadal strzeze przejscia.");
        ConfigureConnection(secretDoor, brickClock,
            "Zegar nie otwiera przejścia bezpośrednio.",
            "Sciana oczekuje znaku od kamiennego mechanizmu.");

        ConfigureConnection(mannequin, trapDoor,
            "Ruch manekina zbiegł się z pułapką.",
            "Taniec i pulapka poruszaja sie jednym rytmem.");
        ConfigureConnection(trapDoor, mannequin,
            "Pułapka i manekin działają razem.",
            "Ten osobliwy straznik nie tanczy dla zabawy.");

        ConfigureConnection(mannequin, secretDoor,
            "Manekin uruchamia ukryte przejście.",
            "Manekin i sekretne przejscie sa czescia tej samej konstrukcji.",
            canConnect: true,
            dependencyFirst: brickClock,
            dependencySecond: mannequin,
            lockedDescription: "Najpierw ustal, co porusza manekinem.");
        ConfigureConnection(secretDoor, mannequin,
            "To manekin uruchamia przejście.",
            "Sciana czeka, az manekin ponownie wykona swoj ruch.");

        ConfigureConnection(trapDoor, secretDoor,
            "Pułapka i przejście mają wspólny mechanizm.",
            "Jedno zamyka droge, drugie moze ja oddac.");
        ConfigureConnection(secretDoor, trapDoor,
            "Przejście i pułapka działają razem.",
            "Wyjscie i pulapka wciaz pozostaja ze soba zwiazane.");
    }

    private DetectiveIdeaPoint FindPoint(string nameFragment)
    {
        if (requiredIdeaPoints == null)
            return null;

        foreach (DetectiveIdeaPoint point in requiredIdeaPoints)
        {
            if (point == null)
                continue;

            if ((point.ideaId ?? string.Empty).IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                point.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                return point;
        }

        return null;
    }

    private static void ConfigureConnection(
        DetectiveIdeaPoint from,
        DetectiveIdeaPoint to,
        string firstDescription,
        string unusedRepeatDescription,
        bool canConnect = false,
        DetectiveIdeaPoint dependencyFirst = null,
        DetectiveIdeaPoint dependencySecond = null,
        string lockedDescription = null)
    {
        DetectiveIdeaPoint.IdeaConnection connection = from.FindConnectionTo(to);
        if (connection == null)
        {
            connection = new DetectiveIdeaPoint.IdeaConnection { target = to };
            from.connections.Add(connection);
        }

        connection.target = to;
        connection.canConnect = canConnect;
        connection.canConnectReverse = false;
        connection.firstTitle = "Sherlock";
        connection.firstDescription = firstDescription;
        connection.lockedTitle = "Sherlock";
        connection.lockedDescription = string.IsNullOrWhiteSpace(lockedDescription)
            ? firstDescription
            : lockedDescription;
        if (connection.requiredConnections == null)
            connection.requiredConnections = new List<DetectiveIdeaPoint.ConnectionDependency>();

        connection.requiredConnections.Clear();

        if (dependencyFirst == null || dependencySecond == null)
            return;

        connection.requiredConnections.Add(new DetectiveIdeaPoint.ConnectionDependency
        {
            first = dependencyFirst,
            second = dependencySecond
        });
    }
}
