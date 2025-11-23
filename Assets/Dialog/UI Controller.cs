using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DialogueUIController : MonoBehaviour, IPointerClickHandler
{
    public static DialogueUIController Instance;

    [Header("UI References - Dialogue")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    [Header("UI References - Notebook")]
    public GameObject notebookCanvas;
    public Transform notebookContent;
    public GameObject notebookEntryPrefab; // Prefab z tekstem Tytu³ + Opis

    [Header("Notification")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;

    private DialogueNode currentNode;
    private NPCDefinition currentNPC;
    private bool isDialogueActive = false;

    void Awake()
    {
        Instance = this;
        dialogueCanvas.SetActive(false);
        notebookCanvas.SetActive(false);
        notificationPanel.SetActive(false);
    }

    void Update()
    {
        // Otwieranie notatnika (jeœli nie ma dialogu)
        if (Input.GetKeyDown(KeyCode.N) && !isDialogueActive)
        {
            ToggleNotebook();
        }
    }

    // --- SYSTEM DIALOGOWY ---

    public void StartDialogue(NPCDefinition npc)
    {
        currentNPC = npc;
        isDialogueActive = true;
        dialogueCanvas.SetActive(true);

        CharacterType myChar = DetectiveManager.Instance.currentCharacter;

        // 1. SprawdŸ czy jest dostêpna specjalna interakcja (DEDUKCJA)
        if (DetectiveManager.Instance.CheckCrossInteraction(npc.npcID) && npc.interactionNode != null)
        {
            DisplayNode(npc.interactionNode);
        }
        else
        {
            // 2. Jeœli nie, ³aduj standardowy dialog dla postaci
            if (myChar == CharacterType.Sherlock) DisplayNode(npc.startNodeSherlock);
            else DisplayNode(npc.startNodeWatson);
        }
    }

    void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        speakerNameText.text = node.speakerName;
        dialogueText.text = node.dialogueText; // Tutaj tekst mo¿e zawieraæ <link="id">s³owo</link>

        if (node.portrait != null) portraitImage.sprite = node.portrait;
        else portraitImage.gameObject.SetActive(false);

        // Generowanie przycisków
        foreach (Transform child in choicesContainer) Destroy(child.gameObject);

        if (node.isEndNode || node.choices.Count == 0)
        {
            CreateChoiceButton("Zakoñcz rozmowê", null);
            // Zapisz fakt rozmowy
            DetectiveManager.Instance.MarkDialogueComplete(currentNPC.npcID, DetectiveManager.Instance.currentCharacter);
        }
        else
        {
            foreach (var choice in node.choices)
            {
                // Filtrowanie opcji:
                // 1. Czy postaæ pasuje?
                bool charOk = (choice.requiredCharacter == CharacterType.Both || choice.requiredCharacter == DetectiveManager.Instance.currentCharacter);
                // 2. Czy mamy wymagany dowód?
                bool clueOk = DetectiveManager.Instance.HasClue(choice.requiredClue);

                if (charOk && clueOk)
                {
                    CreateChoiceButton(choice.choiceText, choice.nextNode);
                }
            }
        }
    }

    void CreateChoiceButton(string text, DialogueNode nextNode)
    {
        GameObject btn = Instantiate(choiceButtonPrefab, choicesContainer);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = text;
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (nextNode == null) EndDialogue();
            else DisplayNode(nextNode);
        });
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        dialogueCanvas.SetActive(false);
    }

    // --- KLIKANIE W S£OWA KLUCZOWE (LINKI) ---
    // Wymaga, aby ten skrypt by³ na obiekcie, który odbiera raycasty lub
    // (lepiej) u¿yjemy tricku z Camera.main w OnPointerClick na samym komponencie TextMeshPro, 
    // ale tutaj zrobimy to globalnie dla uproszczenia, zak³adaj¹c ¿e ten skrypt obs³uguje input.

    // UWAGA: Aby to dzia³a³o, na komponencie TextMeshProUGUI (dialogueText) musisz dodaæ skrypt,
    // który przekazuje klikniêcie tutaj, LUB po prostu w Update sprawdzaæ myszkê.
    // Zróbmy wersjê prostsz¹: Sprawdzamy klikniêcie w Update dla konkretnie dialogueText.

    public void OnPointerClick(PointerEventData eventData)
    {
        // Ta metoda zadzia³a, jeœli skrypt jest na obiekcie z Raycast Target (np. Panelu Dialogu)
        // Ale musimy sprawdziæ, czy kliknêliœmy w link w tekœcie.

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(dialogueText, Input.mousePosition, null); // null if camera is overlay

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = dialogueText.textInfo.linkInfo[linkIndex];
            string clueID = linkInfo.GetLinkID();

            // Logika klikniêcia
            DetectiveManager.Instance.UnlockClue(clueID);
        }
    }

    // --- NOTATNIK ---

    public void ToggleNotebook()
    {
        bool state = !notebookCanvas.activeSelf;
        notebookCanvas.SetActive(state);

        if (state) RefreshNotebook();
    }

    void RefreshNotebook()
    {
        foreach (Transform child in notebookContent) Destroy(child.gameObject);

        var clues = DetectiveManager.Instance.GetUnlockedCluesList();

        if (clues.Count == 0)
        {
            // Opcjonalnie: Pusty tekst
        }

        foreach (var clue in clues)
        {
            GameObject entry = Instantiate(notebookEntryPrefab, notebookContent);
            // Zak³adam, ¿e prefab ma 2 komponenty tekstowe
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 1) texts[0].text = clue.title; // Tytu³
            if (texts.Length >= 2) texts[1].text = clue.description; // Opis
        }
    }

    // --- POWIADOMIENIA ---
    public void ShowNotification(string msg)
    {
        notificationPanel.SetActive(true);
        notificationText.text = msg;
        Invoke("HideNotification", 3f);
    }

    void HideNotification()
    {
        notificationPanel.SetActive(false);
    }
}