using System.Collections;
using UnityEngine;

public class lvl2_Int_StairsExit : Lvl3InteractionDialogueBase
{
    private Interactable interactable;

    [Header("Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine[] missingLettersDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Nie ma sensu na razie wracaæ na dó³",
            duration = 3f
        }
    };

    [SerializeField] private Lvl3DialogueLine[] hiddenDoorNotFoundDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Korci mnie, aby sprawdziæ, czy jest tu gdzieœ drugie wyjœcie",
            duration = 3f
        }
    };

    [SerializeField] private Lvl3DialogueLine[] hiddenDoorFoundDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Powinienem sprawdziæ, dok¹d prowadzi tajne przejœcie z pokoju Ethel",
            duration = 3f
        }
    };

    [SerializeField] private Lvl3DialogueLine[] allLettersDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Chyba mam wszystko, czego tutaj potrzebujê",
            duration = 3f
        }
    };

    [SerializeField] private Lvl3DialogueLine[] ethelNotFoundDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Muszê jeszcze znaleŸæ ma³¹ Ethel",
            duration = 3f
        }
    };

    public bool performed = false;
    public bool ethelFounded = false;

    [SerializeField] private int requiredLetters = 3;
    private int lettersCollected = 0;

    public bool hiddenDoorDiscovered = false;
    public bool canExitByStairs = false;

    public GameObject level_2;
    public GameObject level_1;
    public Transform level1StartPoint;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => missingLettersDialogue;

    public void AddLetter()
    {
        lettersCollected++;
        if (HasAllLetters())
            StartCoroutine(ShowAllLettersDialogue());
    }

    public bool HasAllLetters()
    {
        return lettersCollected >= requiredLetters;
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");
        TryExit(player);

        if (player != null)
            player.currentInteractable = null;
    }

    public void TryExit(PlayerController player)
    {
        if (canExitByStairs)
        {
            StartCoroutine(GoDownStairs(player));
            return;
        }

        if (!HasAllLetters())
        {
            PlayDialogue(player, missingLettersDialogue);
            Debug.Log("Brakuje listów");
            return;
        }

        PlayDialogue(player, hiddenDoorDiscovered
            ? hiddenDoorFoundDialogue
            : hiddenDoorNotFoundDialogue);
    }

    private IEnumerator ShowAllLettersDialogue()
    {
        yield return new WaitForSeconds(3f);
        PlayDialogue(null, allLettersDialogue);

        if (!ethelFounded)
        {
            yield return new WaitForSeconds(GetDialogueDuration(allLettersDialogue));
            PlayDialogue(null, ethelNotFoundDialogue);
        }
    }

    private static float GetDialogueDuration(Lvl3DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return 0f;

        float totalDuration = 0f;
        foreach (Lvl3DialogueLine line in lines)
            totalDuration += line.duration > 0f ? line.duration : 3f;

        return totalDuration;
    }

    private IEnumerator GoDownStairs(PlayerController player)
    {
        if (player == null)
            yield break;

        player.navMeshAgent.ResetPath();
        if (level_1 != null)
            level_1.SetActive(true);

        yield return null;
        player.navMeshAgent.ResetPath();

        if (level1StartPoint != null)
        {
            player.navMeshAgent.Warp(level1StartPoint.position);
            player.transform.rotation = level1StartPoint.rotation;
        }

        yield return null;
        if (level_2 != null)
            level_2.SetActive(false);

        player.currentInteractable = null;
    }
}