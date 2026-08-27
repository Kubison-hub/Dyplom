using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_Keyhole : Lvl3ClockworkInteraction
{
    [Header("Key State")]
    [Tooltip("Use for testing. Normally the keyhole checks ItemType.Wahadlo in InventoryManager.")]
    [SerializeField] private bool hasKeyDebug;

    [Header("Door")]
    [SerializeField] private Transform smallBrickDoor;
    [Tooltip("Target global Y position for the small brick door after using the key.")]
    [SerializeField] private float openedDoorWorldY = 1.1f;
    [SerializeField, Min(0.05f)] private float doorMoveDuration = 0.8f;
    [SerializeField] private AudioSource doorOpenAudioSource;

    [Header("Watson After Opening")]
    [SerializeField] private Transform watsonPosition;
    [SerializeField, Min(0.05f)] private float watsonArrivalDistance = 0.15f;
    [SerializeField, Min(0.1f)] private float watsonNavMeshSampleRadius = 1f;

    [Header("Inserted Key Visual")]
    [Tooltip("A key object kept inactive until the player uses the correct key.")]
    [SerializeField] private GameObject insertedKey;
    [Tooltip("Target local Z position for the visible key after it appears.")]
    [SerializeField] private float insertedKeyLocalZ = 0.46f;
    [SerializeField, Min(0.05f)] private float insertedKeyMoveDuration = 1.2f;

    [Header("Dialogue Audio")]
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [TextArea] [SerializeField] private string firstExaminationText =
        "Hmm, to mi wygląda na wejście na specjalny klucz.";
    [SerializeField] private AudioClip firstExaminationAudio;
    [TextArea] [SerializeField] private string missingKeyText = "Potrzebuję klucza.";
    [SerializeField] private AudioClip missingKeyAudio;

    [Header("Key Success Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] keySuccessDialogueLines;

    [Header("Next Interactions")]
    [Tooltip("Whole GameObjects enabled after the key opens the mechanism.")]
    public GameObject[] nextInteractionGameObjects;
    [Tooltip("Whole GameObjects disabled after the key opens the mechanism.")]
    public GameObject[] gameObjectsToDeactivateOnOpen;

    private bool hasBeenExamined;
    private bool opened;
    private bool isOpening;
    private Coroutine keySuccessDialogueCoroutine;

    protected override void Awake()
    {
        base.Awake();
        EnsureDefaultSuccessDialogue();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        EnsureDefaultSuccessDialogue();
    }

    public bool HasKey => hasKeyDebug ||
                          (InventoryManager.Instance != null &&
                           InventoryManager.Instance.items.Contains(ItemType.Wahadlo));

    public override bool CanPlayerUse(PlayerController player)
    {
        return base.CanPlayerUse(player);
    }

    public void SetKeyFound(bool found = true)
    {
        hasKeyDebug = found;
    }

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        if (isOpening || opened)
            return;

        if (!hasBeenExamined)
        {
            hasBeenExamined = true;
            ShowPlayerLine(player, firstExaminationText, firstExaminationAudio);
            return;
        }

        if (!HasKey)
        {
            ShowPlayerLine(player, missingKeyText, missingKeyAudio);
            return;
        }

        StartCoroutine(OpenWithKey(player));
    }

    private IEnumerator OpenWithKey(PlayerController player)
    {
        isOpening = true;
        InventoryManager.Instance?.TryRemoveItem(ItemType.Wahadlo);

        if (keySuccessDialogueCoroutine != null)
            StopCoroutine(keySuccessDialogueCoroutine);

        keySuccessDialogueCoroutine = StartCoroutine(PlayKeySuccessDialogue(player));
        doorOpenAudioSource?.Play();

        if (insertedKey != null)
        {
            insertedKey.SetActive(true);
            StartCoroutine(AnimateInsertedKey());
        }

        if (smallBrickDoor != null)
        {
            Vector3 startPosition = smallBrickDoor.position;
            Vector3 targetPosition = startPosition;
            targetPosition.y = openedDoorWorldY;

            float elapsed = 0f;
            while (elapsed < doorMoveDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / doorMoveDuration);
                smallBrickDoor.position = Vector3.Lerp(startPosition, targetPosition, progress);
                yield return null;
            }

            smallBrickDoor.position = targetPosition;
        }

        if (watsonPosition != null)
            StartCoroutine(MoveWatsonToPosition());

        foreach (GameObject nextInteraction in nextInteractionGameObjects)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }

        foreach (GameObject target in gameObjectsToDeactivateOnOpen)
        {
            if (target != null)
                target.SetActive(false);
        }

        opened = true;
        isOpening = false;

        if (Interactable != null)
            Interactable.isInteractableActive = false;
    }

    private IEnumerator MoveWatsonToPosition()
    {
        PlayerController watson = GetWatsonPlayer();
        if (watson == null || watson.navMeshAgent == null)
        {
            Debug.LogWarning($"{nameof(Int_lv3_Keyhole)}: Watson or his NavMeshAgent is missing.", this);
            yield break;
        }

        NavMeshAgent agent = watson.navMeshAgent;
        if (!agent.isOnNavMesh ||
            !NavMesh.SamplePosition(watsonPosition.position, out NavMeshHit destination,
                watsonNavMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{nameof(Int_lv3_Keyhole)}: Watson Position is not on the NavMesh.", watsonPosition);
            yield break;
        }

        agent.isStopped = false;
        agent.SetDestination(destination.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            agent.ResetPath();
            Debug.LogWarning($"{nameof(Int_lv3_Keyhole)}: Watson cannot reach Watson Position.", watsonPosition);
            yield break;
        }

        while (agent.hasPath &&
               agent.remainingDistance > Mathf.Max(watsonArrivalDistance, agent.stoppingDistance))
            yield return null;

        agent.ResetPath();
        watson.transform.rotation = watsonPosition.rotation;
    }

    private static PlayerController GetWatsonPlayer()
    {
        foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player != null &&
                (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB")))
                return player;
        }

        return null;
    }

    private IEnumerator AnimateInsertedKey()
    {
        Transform keyTransform = insertedKey.transform;
        Vector3 startPosition = keyTransform.localPosition;
        Vector3 targetPosition = startPosition;
        targetPosition.z = insertedKeyLocalZ;

        float elapsed = 0f;
        while (elapsed < insertedKeyMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / insertedKeyMoveDuration);
            keyTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        keyTransform.localPosition = targetPosition;
    }

    private void ShowPlayerLine(PlayerController player, string text, AudioClip clip)
    {
        ShowTopTextForPlayer(player, text);

        bool isSherlock = player != null &&
                          (player.playerCharacter == PlayerCharacter.Sherlock || player.CompareTag("PlayerA"));
        if (isSherlock && sherlockVoiceSource != null && clip != null)
            sherlockVoiceSource.PlayOneShot(clip);
    }

    private IEnumerator PlayKeySuccessDialogue(PlayerController player)
    {
        foreach (Lvl3DialogueLine line in keySuccessDialogueLines)
        {
            if (string.IsNullOrWhiteSpace(line.text))
                continue;

            bool isWatson = line.speaker == Lvl3DialogueSpeaker.Watson;
            string sherlockText = isWatson ? string.Empty : line.text;
            string watsonText = isWatson ? line.text : string.Empty;
            PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);

            AudioSource source = line.speaker == Lvl3DialogueSpeaker.Watson
                ? watsonVoiceSource
                : sherlockVoiceSource;
            if (source != null && line.voiceClip != null)
                source.PlayOneShot(line.voiceClip);

            float duration = line.duration > 0f
                ? line.duration
                : PlayerTopText.Instance != null ? PlayerTopText.Instance.textTime : 3f;
            yield return new WaitForSeconds(duration);
            PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
        }

        keySuccessDialogueCoroutine = null;
    }

    private void EnsureDefaultSuccessDialogue()
    {
        if (keySuccessDialogueLines != null && keySuccessDialogueLines.Length > 0)
            return;

        keySuccessDialogueLines = new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = "Udało się, klucz pasuje idealnie.",
                duration = 3f
            }
        };
    }
}
