using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

/// <summary>
/// Sherlock-only two-step hidden-door interaction. The key is first revealed in
/// the lock and then pushed in to open the door.
/// </summary>
public class Int_lv4_HiddenDoor : Lvl3InteractionDialogueBase
{
    [Header("Required Key")]
    [Tooltip("Temporary testing override until the inventory is connected.")]
    [SerializeField] private bool hasKey;
    [Tooltip("The actual lv4_HiddenDoorKey object after it has been put into the inventory.")]
    [SerializeField] private Transform hiddenDoorKeyInventoryObject;
    [Tooltip("Inventory root that contains the key after it is collected.")]
    [SerializeField] private Transform inventoryRoot;
    [Tooltip("Fallback for quick tests when an inventory root is not assigned.")]
    [SerializeField] private bool keyObjectActiveMeansOwned = true;
    [Tooltip("When enabled, this door also accepts the chosen item from InventoryManager.")]
    [SerializeField] private bool useInventoryItemKey;
    [SerializeField] private ItemType requiredInventoryItem = ItemType.WoodBlockLevel1;

    [Header("Key In Door")]
    [SerializeField] private GameObject keyGameObject;
    [SerializeField] private Transform keyTransform;
    [SerializeField] private Interactable keyInteractable;
    [SerializeField] private Vector3 keyLocalPushOffset = new Vector3(0f, 0f, 0.1f);
    [SerializeField, Min(0.05f)] private float keyPushDuration = 0.35f;
    [SerializeField] private AudioSource keyAudioSource;
    [SerializeField] private AudioClip keyUseAudio;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip doorOpenAudio;
    [Tooltip("Room revealed immediately after the door receives its Open trigger.")]
    [SerializeField] private GameObject roomToActivateAfterOpening;
    [Header("Reparent Before Opening")]
    [Tooltip("Leave empty to reparent the Door Animator GameObject.")]
    [SerializeField] private Transform objectToReparentBeforeOpening;
    [SerializeField] private Transform newParentBeforeOpening;

    [Header("BlackBoard Reveal")]
    [SerializeField] private GameObject blackBoard;
    [Tooltip("Fade material, used like lvl2_Int_EthelWallButton before hiding the blackboard.")]
    [SerializeField] private Material blackBoardFadeMaterial;
    [SerializeField, Min(0f)] private float blackBoardFadeDelay = 0.1f;
    [SerializeField, Min(0.01f)] private float blackBoardFadeDuration = 3.5f;
    [SerializeField] private bool deactivateBlackBoardAfterFade = true;

    [Header("After Opening")]
    [SerializeField, Min(1f)] private float sherlockTurnSpeed = 180f;
    [SerializeField] private Transform watsonAfterOpenIntPoint;
    [SerializeField, Min(0.05f)] private float watsonArrivalDistance = 0.15f;
    [SerializeField, Min(1f)] private float watsonTurnSpeed = 180f;
    [SerializeField] private Int_lv4_Hatch hatchAfterOpening;

    [Header("Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] keyFoundDialogue;
    [SerializeField] private Lvl3DialogueLine[] missingKeyDialogue;
    [SerializeField] private Lvl3DialogueLine[] keyUseDialogue;

    [Header("Loupe Discovery")]
    [SerializeField, Min(0.1f)] private float loupeHoldDuration = 1.3f;
    [SerializeField] private Collider[] loupeDetectionColliders;
    [SerializeField] private LayerMask loupeDetectionMask = ~0;
    [SerializeField] private GameObject questionFX;
    [SerializeField] private string revealedQuestionFXLayer = "Hidden";
    [SerializeField] private Lvl3DialogueLine[] loupeRevealDialogue;

    [Header("Interaction")]
    [SerializeField] private Interactable interactable;

    private bool keyPresented;
    private bool opened;
    private bool loupeRevealed;
    private float loupeHoldTimer;
    private Vector3 keyInitialLocalPosition;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => keyFoundDialogue;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();

        if (keyTransform == null && keyGameObject != null)
            keyTransform = keyGameObject.transform;

        if (keyInteractable == null && keyGameObject != null)
            keyInteractable = keyGameObject.GetComponent<Interactable>();

        if (keyTransform != null)
            keyInitialLocalPosition = keyTransform.localPosition;

        if (keyGameObject != null)
            keyGameObject.SetActive(false);
    }

    private void Update()
    {
        if (loupeRevealed)
            return;

        bool sherlockIsActive = SwitchCharacter.Instance != null &&
                                SwitchCharacter.Instance.activePlayerIndex == 0;
        if (!sherlockIsActive || Keyboard.current == null ||
            !Keyboard.current.fKey.isPressed || !IsLoupeOverDoor())
        {
            loupeHoldTimer = 0f;
            return;
        }

        loupeHoldTimer += Time.deltaTime;
        if (loupeHoldTimer >= loupeHoldDuration)
            RevealWithLoupe();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player == null || opened || player.playerCharacter != PlayerCharacter.Sherlock)
            return;

        if (!PlayerHasKey())
        {
            PlayDialogue(player, missingKeyDialogue);
            return;
        }

        if (!keyPresented)
        {
            keyPresented = true;
            if (keyGameObject != null)
                keyGameObject.SetActive(true);

            PlayDialogue(player, keyFoundDialogue);
            return;
        }

        StartCoroutine(OpenDoor(player));
    }

    public void OpenWithInstalledWoodBlock(PlayerController player)
    {
        if (opened)
            return;

        hasKey = true;
        keyPresented = true;
        if (keyGameObject != null)
            keyGameObject.SetActive(true);

        StartCoroutine(OpenDoor(player));
    }

    public void SetHasKey(bool value)
    {
        hasKey = value;
    }

    private bool PlayerHasKey()
    {
        if (hasKey)
            return true;

        if (useInventoryItemKey && InventoryManager.Instance != null &&
            InventoryManager.Instance.items.Contains(requiredInventoryItem))
        {
            return true;
        }

        if (hiddenDoorKeyInventoryObject == null)
            return false;

        if (inventoryRoot != null)
            return hiddenDoorKeyInventoryObject.IsChildOf(inventoryRoot);

        return keyObjectActiveMeansOwned && hiddenDoorKeyInventoryObject.gameObject.activeInHierarchy;
    }

    private bool IsLoupeOverDoor()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null || Mouse.current == null)
            return false;

        Ray ray = activeCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, loupeDetectionMask, QueryTriggerInteraction.Collide))
            return false;

        if (loupeDetectionColliders != null && loupeDetectionColliders.Length > 0)
        {
            foreach (Collider detectionCollider in loupeDetectionColliders)
            {
                if (detectionCollider == hit.collider)
                    return true;
            }

            return false;
        }

        return hit.collider != null &&
               (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform));
    }

    private void RevealWithLoupe()
    {
        loupeRevealed = true;
        loupeHoldTimer = 0f;

        PlayerController sherlock = FindPlayer(PlayerCharacter.Sherlock);
        if (sherlock != null)
            PlayDialogue(sherlock, loupeRevealDialogue);

        if (questionFX != null)
        {
            int hiddenLayer = LayerMask.NameToLayer(revealedQuestionFXLayer);
            if (hiddenLayer < 0)
            {
                Debug.LogWarning($"{name}: layer '{revealedQuestionFXLayer}' does not exist.", this);
                return;
            }

            foreach (Transform current in questionFX.GetComponentsInChildren<Transform>(true))
                current.gameObject.layer = hiddenLayer;
        }
    }

    private IEnumerator OpenDoor(PlayerController player)
    {
        opened = true;
        GetComponent<Interactable>()?.MarkCompleted();
        ConsumeRequiredInventoryItem();
        PlayDialogue(player, keyUseDialogue);

        if (keyAudioSource != null && keyUseAudio != null)
            keyAudioSource.PlayOneShot(keyUseAudio);

        if (keyTransform != null)
        {
            Vector3 from = keyTransform.localPosition;
            Vector3 to = keyInitialLocalPosition + keyLocalPushOffset;
            float elapsed = 0f;

            while (elapsed < keyPushDuration)
            {
                elapsed += Time.deltaTime;
                keyTransform.localPosition = Vector3.Lerp(from, to, elapsed / keyPushDuration);
                yield return null;
            }

            keyTransform.localPosition = to;
        }

        if (doorAudioSource != null && doorOpenAudio != null)
            doorAudioSource.PlayOneShot(doorOpenAudio);

        ReparentDoorBeforeOpening();

        if (doorAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            doorAnimator.SetTrigger(openTrigger);

        if (roomToActivateAfterOpening != null)
            roomToActivateAfterOpening.SetActive(true);

        if (blackBoardFadeDelay > 0f)
            yield return new WaitForSeconds(blackBoardFadeDelay);

        yield return FadeBlackBoard();
        yield return MoveWatsonAfterOpening();

        if (SwitchCharacter.Instance != null)
            SwitchCharacter.Instance.canSwitch = true;

        if (hatchAfterOpening != null)
            hatchAfterOpening.SetWatsonInRoom(true);
        yield return RotateSherlockTowardWatson();

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            foreach (Collider interactionCollider in interactable.GetComponentsInChildren<Collider>(true))
                interactionCollider.enabled = false;
        }

        if (questionFX != null)
            questionFX.SetActive(false);

        // The key remains visible in the opened door, but cannot be clicked again.
        if (keyGameObject != null)
            keyGameObject.SetActive(true);

        if (keyInteractable != null)
        {
            keyInteractable.isInteractableActive = false;
            Collider keyCollider = keyInteractable.GetComponent<Collider>();
            if (keyCollider != null)
                keyCollider.enabled = false;
        }
    }

    private void ReparentDoorBeforeOpening()
    {
        if (newParentBeforeOpening == null)
            return;

        Transform target = objectToReparentBeforeOpening;
        if (target == null && doorAnimator != null)
            target = doorAnimator.transform;

        if (target != null && target.parent != newParentBeforeOpening)
            target.SetParent(newParentBeforeOpening, true);
    }

    private void ConsumeRequiredInventoryItem()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.TryRemoveItem(requiredInventoryItem);
    }

    private IEnumerator FadeBlackBoard()
    {
        if (blackBoard == null)
            yield break;

        Renderer[] renderers = blackBoard.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            blackBoard.SetActive(false);
            yield break;
        }

        if (blackBoardFadeMaterial != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.material = blackBoardFadeMaterial;
            }
        }

        float startAlpha = GetBlackBoardAlpha(renderers[0]);
        float elapsed = 0f;
        while (elapsed < blackBoardFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / blackBoardFadeDuration);
            SetBlackBoardAlpha(renderers, alpha);
            yield return null;
        }

        SetBlackBoardAlpha(renderers, 0f);
        if (deactivateBlackBoardAfterFade)
            blackBoard.SetActive(false);
    }

    private static void SetBlackBoardAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer currentRenderer in renderers)
        {
            if (currentRenderer == null || currentRenderer.material == null)
                continue;

            Material material = currentRenderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = alpha;
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                color.a = alpha;
                material.SetColor("_Color", color);
            }
        }
    }

    private static float GetBlackBoardAlpha(Renderer renderer)
    {
        if (renderer == null || renderer.material == null)
            return 1f;

        Material material = renderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        return material.HasProperty(colorProperty) ? material.GetColor(colorProperty).a : 1f;
    }

    private IEnumerator RotateSherlockTowardWatson()
    {
        PlayerController sherlock = FindPlayer(PlayerCharacter.Sherlock);
        PlayerController watson = FindPlayer(PlayerCharacter.Watson);

        if (sherlock == null || watson == null)
            yield break;

        Vector3 direction = watson.transform.position - sherlock.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        while (Quaternion.Angle(sherlock.transform.rotation, targetRotation) > 0.1f)
        {
            sherlock.transform.rotation = Quaternion.RotateTowards(
                sherlock.transform.rotation,
                targetRotation,
                sherlockTurnSpeed * Time.deltaTime);
            yield return null;
        }

        sherlock.transform.rotation = targetRotation;
    }

    private IEnumerator MoveWatsonAfterOpening()
    {
        if (watsonAfterOpenIntPoint == null)
            yield break;

        PlayerController watson = FindPlayer(PlayerCharacter.Watson);
        PlayerController sherlock = FindPlayer(PlayerCharacter.Sherlock);
        if (watson == null || sherlock == null)
            yield break;

        NavMeshAgent agent = watson.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
            yield break;

        if (!NavMesh.SamplePosition(watsonAfterOpenIntPoint.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Watson After Open Int Point is outside NavMesh.", watsonAfterOpenIntPoint);
            yield break;
        }

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(hit.position);

        while (agent.pathPending || agent.remainingDistance > watsonArrivalDistance)
            yield return null;

        agent.ResetPath();

        Vector3 direction = sherlock.transform.position - watson.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        while (Quaternion.Angle(watson.transform.rotation, targetRotation) > 0.1f)
        {
            watson.transform.rotation = Quaternion.RotateTowards(
                watson.transform.rotation,
                targetRotation,
                watsonTurnSpeed * Time.deltaTime);
            yield return null;
        }

        watson.transform.rotation = targetRotation;
    }

    private static PlayerController FindPlayer(PlayerCharacter character)
    {
        foreach (PlayerController candidate in FindObjectsOfType<PlayerController>())
        {
            if (candidate != null && candidate.playerCharacter == character)
                return candidate;
        }

        return null;
    }
}
