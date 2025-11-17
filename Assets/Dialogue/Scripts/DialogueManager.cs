using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;

// UWAGA: Ten skrypt wymaga zainstalowanego Firebase SDK for Unity (Firestore)
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Wymagane Komponenty")]
    public DialogueUI dialogueUI;
    public PlayerContext playerContext;
    public NotebookManager notebookManager;
    // public QuestManager questManager; // Opcjonalnie: do zaimplementowania zadañ

    // Aktualnie ³adowane drzewko dialogowe i wêze³
    private DialogueTree currentTree;
    private DialogueNode currentNode;
    private int currentLineIndex;

    private List<KeywordData> activeKeywords = new List<KeywordData>();
    private bool isAwaitingPlayerInput = false;

    // Zmienne do obs³ugi Firebase/Firestore
    private FirebaseFirestore db;
    private string userId;
    private string appId;
    private bool isFirebaseReady = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Inicjalizacja FireStore odbywa siê w Start() lub przez FirebaseInitializer
        appId = typeof(__app_id) != null ? __app_id.ToString() : "default-app-id";
    }

    private void Start()
    {
        // Ustawienie referencji dla bezpieczeñstwa, jeœli nie s¹ ustawione w Inspectorze
        if (playerContext == null) playerContext = FindObjectOfType<PlayerContext>();
        if (notebookManager == null) notebookManager = FindObjectOfType<NotebookManager>();

        // Pod³¹czenie akcji z UI
        if (dialogueUI != null)
        {
            dialogueUI.OnOptionSelectedAction = OnOptionSelected;
        }

        // Uruchomienie inicjalizacji Firebase
        if (Application.isPlaying)
        {
            FirebaseInitializer.InitializeFirebase(this, (firestore, uid) =>
            {
                db = firestore;
                userId = uid;
                isFirebaseReady = true;
                Debug.Log($"Firebase i Firestore zainicjowane dla UID: {userId}");
            });
        }
    }

    // G£ÓWNA FUNKCJA: Rozpoczyna nowy dialog z NPC
    public void StartDialogue(DialogueTree tree)
    {
        if (!isFirebaseReady)
        {
            Debug.LogError("Firebase nie jest gotowe. Nie mo¿na rozpocz¹æ dialogu.");
            return;
        }

        // Zapewnienie, ¿e nie ma aktywnego dialogu
        if (currentNode != null)
        {
            Debug.LogWarning("Poprzedni dialog jest nadal aktywny. Koñczê go przed nowym startem.");
            EndDialogue();
        }

        currentTree = tree;
        currentNode = tree.StartNode;
        currentLineIndex = 0;
        activeKeywords.Clear();
        isAwaitingPlayerInput = false;

        // Zapisujemy, ¿e aktualna postaæ rozpoczê³a rozmowê (wa¿ne dla Interrogation)
        if (currentTree.InterrogationDialogue != null && currentNode.ShouldTriggerInterrogationCheck)
        {
            // W tym miejscu NIE zapisujemy jeszcze, tylko upewniamy siê, ¿e jesteœmy gotowi.
            // Zapis nastêpuje w EndDialogue() po zakoñczeniu rozmowy.
        }

        DisplayCurrentNode();
    }

    // Wywo³ywane przez gracza (np. klikniêcie na dymek dialogowy lub spacjê)
    public void HandleNextInput()
    {
        if (isAwaitingPlayerInput || currentNode == null) return;

        // Jeœli aktualny index jest mniejszy ni¿ liczba linii w Node, przejdŸ do nastêpnej
        if (currentLineIndex < currentNode.Lines.Count - 1)
        {
            currentLineIndex++;
            DisplayCurrentNode();
        }
        // Jeœli osi¹gnêliœmy ostatni¹ liniê, przygotuj opcje
        else if (currentNode.Options.Count > 0 || activeKeywords.Count > 0)
        {
            DisplayOptions();
        }
        // Jeœli nie ma wiêcej linii ani opcji, zakoñcz
        else
        {
            EndDialogue();
        }
    }

    // --- Nawigacja w drzewku ---

    private void DisplayCurrentNode()
    {
        // 1. Ustaw flagê, ¿e czekamy na akcjê gracza (klikniêcie, aby przejœæ dalej)
        isAwaitingPlayerInput = true;

        DialogueLine lineToShow = null;

        // Iteruj od aktualnego indeksu, aby znaleŸæ pierwsz¹ liniê spe³niaj¹c¹ warunki gracza
        // (W tej prostej implementacji, zak³adamy, ¿e tylko KeywordData ma warunki)
        if (currentLineIndex < currentNode.Lines.Count)
        {
            lineToShow = currentNode.Lines[currentLineIndex];
        }

        if (lineToShow == null)
        {
            EndDialogue();
            return;
        }

        // 2. Przetwarzanie i podœwietlanie s³ów kluczowych
        string lineText = lineToShow.LineText;
        activeKeywords.Clear();

        foreach (var keyword in lineToShow.Keywords)
        {
            if (CheckConditions(keyword.Conditions))
            {
                activeKeywords.Add(keyword);

                // POPRAWIONA LOGIKA PODŒWIETLANIA: 
                // Zast¹pienie faktycznego s³owa formatem [KEYWORD]
                // W skrypcie DialogueUI.cs tag [KEYWORD] zostanie zamieniony na <color>
                lineText = lineText.Replace(keyword.Keyword, $"[KEYWORD]{keyword.Keyword}[/KEYWORD]");
            }
        }

        // 3. Wyœwietl liniê i zwolnij kontrolê do gracza
        dialogueUI.SetDialogueText(lineToShow, lineText);
        isAwaitingPlayerInput = false;
    }

    // Wyœwietla dostêpne opcje wyboru gracza (w tym opcje s³ów kluczowych)
    private void DisplayOptions()
    {
        isAwaitingPlayerInput = true;

        // Filtruj opcje statyczne na podstawie warunków
        List<DialogueOption> availableOptions = currentNode.Options
            .Where(opt => CheckConditions(opt.Conditions))
            .ToList();

        // Wyœwietl wszystkie opcje (statyczne + dynamiczne s³owa kluczowe)
        dialogueUI.DisplayOptions(availableOptions, activeKeywords);
    }

    // Obs³uguje wybór gracza
    private void OnOptionSelected(DialogueOption selectedOption)
    {
        // 1. Sprawdzenie, czy to dynamiczna opcja s³owa kluczowego
        if (selectedOption.NextNodeID.StartsWith("KEYWORD:"))
        {
            HandleKeywordAsk(selectedOption);
            return;
        }

        // 2. Obs³uga statycznej opcji

        // TODO: Wykonaj akcje (selectedOption.Actions)
        if (selectedOption.Actions != null)
        {
            Debug.Log($"Wykonanie akcji: {string.Join(", ", selectedOption.Actions)}");
        }

        // PrzejdŸ do nastêpnego wêz³a
        DialogueNode nextNode = currentTree.GetNodeByID(selectedOption.NextNodeID);
        if (nextNode != null)
        {
            currentNode = nextNode;
            currentLineIndex = 0;
            DisplayCurrentNode();
        }
        else
        {
            EndDialogue();
        }
    }

    // Obs³uga mechaniki s³owa kluczowego (dodanie notatki, nowa linia NPC)
    private void HandleKeywordAsk(DialogueOption keywordOption)
    {
        string keywordText = keywordOption.NextNodeID.Substring("KEYWORD:".Length);
        KeywordData data = activeKeywords.FirstOrDefault(k => k.Keyword == keywordText);

        if (data != null)
        {
            // 1. Dodaj notatkê do Notatnika
            if (data.NoteOnAsk != null)
            {
                notebookManager.AddNote(data.NoteOnAsk);
            }

            // 2. Utwórz tymczasow¹ liniê odpowiedzi od NPC (i opcjê powrotu)
            DialogueNode keywordResponseNode = new DialogueNode
            {
                NodeID = "TEMP_KEYWORD_RESPONSE",
                Lines = new List<DialogueLine>
                {
                    new DialogueLine
                    {
                        Speaker = CharacterType.NPC,
                        LineText = data.NPCResponse,
                        Keywords = new List<KeywordData>()
                    }
                },
                Options = new List<DialogueOption>
                {
                    // Wracamy do oryginalnego wêz³a po odpowiedzi NPC
                    new DialogueOption { OptionText = "Kontynuuj rozmowê.", NextNodeID = currentNode.NodeID }
                }
            };

            currentNode = keywordResponseNode;
            currentLineIndex = 0;
            DisplayCurrentNode();
        }
        else
        {
            DisplayOptions();
        }
    }

    // --- Walidacja Warunków ---
    private bool CheckConditions(List<Condition> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true;

        foreach (var condition in conditions)
        {
            bool isSatisfied = false;
            switch (condition.Type)
            {
                case ConditionType.IsSherlock:
                    isSatisfied = playerContext.IsSherlockActive() == condition.RequiredValue;
                    break;
                case ConditionType.IsWatson:
                    isSatisfied = playerContext.IsWatsonActive() == condition.RequiredValue;
                    break;
                case ConditionType.RequireNote:
                    isSatisfied = notebookManager.HasNote(condition.TargetID) == condition.RequiredValue;
                    break;
                case ConditionType.RequireQuestStatus:
                    // Wymaga implementacji QuestManagera, obecnie zawsze zwraca true
                    isSatisfied = true;
                    break;
            }

            if (!isSatisfied)
            {
                return false;
            }
        }
        return true;
    }

    // Zakoñczenie dialogu i sprawdzenie interakcji wewnêtrznej
    private async void EndDialogue()
    {
        dialogueUI.ShowPanel(false);
        isAwaitingPlayerInput = false;

        // SprawdŸ, czy musimy œledziæ konwersacjê
        if (currentNode != null && currentNode.ShouldTriggerInterrogationCheck && currentTree.InterrogationDialogue != null)
        {
            // Zapisz, ¿e aktualna postaæ rozmawia³a
            await MarkCharacterSpoken(currentTree.DialogueID, playerContext.GetActivePlayer());

            // SprawdŸ, czy nadszed³ czas na rozmowê wewnêtrzn¹ (Sherlock & Watson)
            if (await CanStartInterrogation(currentTree.DialogueID))
            {
                Debug.Log("Obie postacie rozmawia³y. Uruchamiam dialog wewnêtrzny.");
                // Resetuj flagi, aby mo¿na by³o ponownie przes³uchaæ NPC (lub dla innego NPC)
                await ResetSpokenFlags(currentTree.DialogueID);
                StartDialogue(currentTree.InterrogationDialogue);
            }
        }

        currentNode = null;
        currentTree = null;
    }

    // --- OBS£UGA FIREBASE / FIRETORE DLA ŒLEDZENIA KONWERSACJI ---

    private string GetInterrogationDocPath(string dialogueId)
    {
        // U¿ywamy œcie¿ki publicznej, aby stan konwersacji by³ dostêpny globalnie dla tej samej gry
        return $"artifacts/{appId}/public/data/interrogations/{dialogueId}";
    }

    private async Task MarkCharacterSpoken(string dialogueId, CurrentPlayer player)
    {
        if (db == null || string.IsNullOrEmpty(dialogueId)) return;

        DocumentReference docRef = db.Document(GetInterrogationDocPath(dialogueId));
        Dictionary<string, object> updates = new Dictionary<string, object>();

        if (player == CurrentPlayer.Sherlock)
        {
            updates["sherlockSpoke"] = true;
        }
        else if (player == CurrentPlayer.Watson)
        {
            updates["watsonSpoke"] = true;
        }

        await docRef.SetAsync(updates, SetOptions.MergeAll);
    }

    private async Task<bool> CanStartInterrogation(string dialogueId)
    {
        if (db == null || string.IsNullOrEmpty(dialogueId)) return false;

        DocumentReference docRef = db.Document(GetInterrogationDocPath(dialogueId));
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            // U¿ycie GetValue<T?> jest bezpieczniejsze przy pobieraniu opcjonalnych pól
            bool sherlockSpoke = snapshot.GetValue<bool?>("sherlockSpoke") ?? false;
            bool watsonSpoke = snapshot.GetValue<bool?>("watsonSpoke") ?? false;

            return sherlockSpoke && watsonSpoke;
        }
        return false;
    }

    private async Task ResetSpokenFlags(string dialogueId)
    {
        if (db == null || string.IsNullOrEmpty(dialogueId)) return;

        DocumentReference docRef = db.Document(GetInterrogationDocPath(dialogueId));
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "sherlockSpoke", false },
            { "watsonSpoke", false }
        };
        await docRef.SetAsync(updates, SetOptions.MergeAll);
    }
}