using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ClueManager : MonoBehaviour
{
    public static ClueManager Instance;

    public Transform sherlock;
    public Transform watson;
    public TextMeshPro SherlockText;
    public TextMeshPro WatsonText;

    [Header("UI Settings")]
    public GameObject clueCardPrefab;
    public ConclusionCard conclusionCard;
    public TextMeshProUGUI notificationText;
    public GameObject notificationCluePrefab;
    public GameObject notificationConclusionPrefab;
    public GameObject notificationQuestConclusionPrefab;
    public Transform container;

    [Header("Data Lists")]
    public List<Clues_SO> collectedClues = new List<Clues_SO>();
    public List<Conclusions_SO> collectedConclusions = new List<Conclusions_SO>();
    public List<QuestConclusions_SO> collectedQuestConclusions = new List<QuestConclusions_SO>();
    public List<Quest_SO> completedQuests = new List<Quest_SO>();
    public List<Quest_SO> activeQuests = new List<Quest_SO>();

    [Header("All Resources")]
    public List<Conclusions_SO> allConclusions;
    public List<QuestConclusions_SO> allQuestConclusions;
    public List<Quest_SO> allQuests;

    public List<GameObject> nextQuestFirstInteractions = new List<GameObject>();

    public MainQuest_SO currentMainQuest;

    private Coroutine rotateCoroutine;
    private Queue<string> messageQueue = new Queue<string>();
    private bool isDisplaying = false;

    private HashSet<Clues_SO> pendingClues = new HashSet<Clues_SO>();
    private HashSet<Conclusions_SO> pendingConclusions = new HashSet<Conclusions_SO>();
    private HashSet<QuestConclusions_SO> pendingQuestConclusions = new HashSet<QuestConclusions_SO>();

    public bool isLockpicking = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (notificationText != null) notificationText.text = "";
    }

    public void AddClue(Clues_SO newClue, Vector3 cluePosition)
    {
        if (newClue == null)
        {
            Debug.LogError("Tried to add a null clue.");
            return;
        }

        if (collectedClues.Contains(newClue) || pendingClues.Contains(newClue))
        {
            return;
        }

        pendingClues.Add(newClue);
        StartCoroutine(ShowTextAndAddClue(newClue));
    }
    private IEnumerator ShowTextAndAddClue(Clues_SO clue)
    {


        if (SherlockText != null)
        {
            //Debug.Log("ShowTextAndAddClue");
            SherlockText.text = clue.sherlockText;
        }
        else
        {
            Debug.LogError($"SherlockText = {SherlockText}");
            pendingClues.Remove(clue);
            yield break;
        }



        if (!string.IsNullOrEmpty(clue.watsonText) && watson != null)
        {
            if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
            rotateCoroutine = StartCoroutine(RotateWatsonTowardSherlockCoroutine());
        }

        yield return new WaitForSeconds(3f);
        SherlockText.text = "";
        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(clue.watsonText) && WatsonText != null)
        {
            WatsonText.text = clue.watsonText;
            yield return new WaitForSeconds(3f);
            WatsonText.text = "";
            yield return new WaitForSeconds(0.3f);
        }

        ShowNotification(clue.type, clue.displayName, clue.shortDescription, clue);
    }

    private IEnumerator RotateWatsonTowardSherlockCoroutine()
    {
        while (true)
        {
            Vector3 direction = sherlock.position - watson.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f) yield break;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            watson.rotation = Quaternion.RotateTowards(watson.rotation, lookRotation, 180f * Time.deltaTime);

            if (Quaternion.Angle(watson.rotation, lookRotation) < 1f)
            {
                watson.rotation = lookRotation;
                yield break;
            }

            yield return null;
        }
    }

    public void AddConclusion(Conclusions_SO newConclusion)
    {
        if (collectedConclusions.Contains(newConclusion) || pendingConclusions.Contains(newConclusion))
        {
            return;
        }

        pendingConclusions.Add(newConclusion);
        conclusionCard.ShowConclusion(newConclusion);
        ShowNotification(newConclusion.type, newConclusion.displayName, newConclusion.shortDescription, null, newConclusion);
    }

    public void AddQuestConclusion(QuestConclusions_SO newQuestConclusion)
    {
        if (collectedQuestConclusions.Contains(newQuestConclusion) || pendingQuestConclusions.Contains(newQuestConclusion))
        {
            return;
        }

        pendingQuestConclusions.Add(newQuestConclusion);
        conclusionCard.ShowQuestConclusion(newQuestConclusion);
        ShowNotification(newQuestConclusion.type, newQuestConclusion.displayName, newQuestConclusion.shortDescription, null, null, newQuestConclusion);
    }

    public void ShowNotification(string typeText, string titleText, string descriptionText, Clues_SO clue = null, Conclusions_SO conclusion = null, QuestConclusions_SO questConclusion = null)
    {
        GameObject prefab = GetNotificationPrefab(clue, conclusion, questConclusion);
        if (prefab == null) return;

        GameObject newCard = Instantiate(prefab, container);
        InvestigationCard cardScript = newCard.GetComponent<InvestigationCard>();

        if (cardScript != null)
        {
            cardScript.cardType.text = typeText;
            cardScript.title.text = titleText;
            cardScript.description.text = descriptionText;
            cardScript.enabled = false;
            cardScript.cardPanel.SetActive(true);

            CanvasGroup cg = cardScript.cardPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                StartCoroutine(FadeInCard(cg));
            }
        }

        StartCoroutine(CheckConclusionAndDestroy(newCard, 5f, clue, conclusion, questConclusion));
    }

    private GameObject GetNotificationPrefab(Clues_SO clue, Conclusions_SO conclusion, QuestConclusions_SO questConclusion)
    {
        if (clue != null) return notificationCluePrefab;
        if (conclusion != null) return notificationConclusionPrefab;
        if (questConclusion != null) return notificationQuestConclusionPrefab;
        return null;
    }

    private IEnumerator FadeInCard(CanvasGroup cg)
    {
        while (cg.alpha < 1)
        {
            cg.alpha += Time.deltaTime * 3f;
            yield return null;
        }
    }

    private IEnumerator CheckConclusionAndDestroy(GameObject card, float delay, Clues_SO clue, Conclusions_SO conclusion, QuestConclusions_SO questConclusion)
    {
        yield return new WaitForSeconds(delay);

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            while (cg.alpha > 0)
            {
                cg.alpha -= Time.deltaTime * 2f;
                yield return null;
            }
        }

        ProcessDataAfterNotification(clue, conclusion, questConclusion);

        if (CluesLog.Instance != null) CluesLog.Instance.UpdateLog();
        Destroy(card);
    }

    private void ProcessDataAfterNotification(Clues_SO clue, Conclusions_SO conclusion, QuestConclusions_SO questConclusion)
    {
        if (clue != null)
        {
            pendingClues.Remove(clue);

            if (!collectedClues.Contains(clue))
            {
                collectedClues.Add(clue);
            }

            CheckForConclusions();
        }

        if (conclusion != null)
        {
            pendingConclusions.Remove(conclusion);

            if (!collectedConclusions.Contains(conclusion))
            {
                collectedConclusions.Add(conclusion);
            }

            CheckForQuestConclusions();
        }

        if (questConclusion != null)
        {
            pendingQuestConclusions.Remove(questConclusion);

            if (!collectedQuestConclusions.Contains(questConclusion))
            {
                collectedQuestConclusions.Add(questConclusion);
            }

            CheckForQuestCompetions();
        }
    }

    private void CheckForConclusions()
    {
        foreach (var conclusion in allConclusions)
        {
            if (collectedConclusions.Contains(conclusion)) continue;

            bool allFound = true;
            foreach (var req in conclusion.requiredClues)
            {
                if (!collectedClues.Contains(req))
                {
                    allFound = false;
                    break;
                }
            }

            if (allFound) AddConclusion(conclusion);
        }
    }

    private void CheckForQuestConclusions()
    {
        foreach (var questConclusion in allQuestConclusions)
        {
            if (collectedQuestConclusions.Contains(questConclusion)) continue;

            bool allFound = true;
            foreach (var req in questConclusion.requiredConclusions)
            {
                if (!collectedConclusions.Contains(req))
                {
                    allFound = false;
                    break;
                }
            }

            if (allFound) AddQuestConclusion(questConclusion);
        }
    }

    private void CheckForQuestCompetions()
    {
        List<Quest_SO> questsToRemove = new List<Quest_SO>();

        foreach (var quest in activeQuests)
        {
            bool allFound = true;
            foreach (var req in quest.requiredQuestConclusions)
            {
                if (!collectedQuestConclusions.Contains(req))
                {
                    allFound = false;
                    break;
                }
            }

            if (allFound) questsToRemove.Add(quest);
        }

        foreach (var finishedQuest in questsToRemove)
        {
            CompleteAndSwitchQuest(finishedQuest);
        }
    }

    private void CompleteAndSwitchQuest(Quest_SO finishedQuest)
    {
        Debug.Log("QUEST UKOÑCZONY: " + finishedQuest.displayName);

        activeQuests.Remove(finishedQuest);
        if (!completedQuests.Contains(finishedQuest)) completedQuests.Add(finishedQuest);

        if (finishedQuest.nextQuest != null)
        {
            var nextQuest = finishedQuest.nextQuest;

            activeQuests.Add(nextQuest);
            Debug.Log("NOWY QUEST ROZPOCZÊTY: " + nextQuest.displayName);

            StartNextQuestFirstInteractions(nextQuest);
        }

        if (CluesLog.Instance != null) CluesLog.Instance.UpdateLog();
    }

    private void StartNextQuestFirstInteractions(Quest_SO nextQuest)
    {
        if (nextQuest == null)
        {
            Debug.LogError(nextQuest);
            Debug.LogError(nextQuest.FirstInteractions);
            return;
        }

        foreach (GameObject prefab in nextQuestFirstInteractions)
        {
            if (prefab != null)
            {
                GameObject spawnedInteraction = Instantiate(prefab);
            }
        }
    }

    public List<Clues_SO> GetCollectedClues() => collectedClues;
    public List<Conclusions_SO> GetCollectedConclusions() => collectedConclusions;
    public List<QuestConclusions_SO> GetQuestCollectedConclusions() => collectedQuestConclusions;



    //FIX

}