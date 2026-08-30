using System.Collections;
using UnityEngine;

/// <summary>
/// A heavy hatch: Sherlock can only comment on it, while Watson opens it.
/// </summary>
public class Int_lv4_Hatch : Lvl3InteractionDialogueBase
{
    [Header("Hatch")]
    [SerializeField] private Transform hatchLid;
    [SerializeField] private float openEulerX = -30f;
    [SerializeField, Min(1f)] private float openSpeed = 180f;
    [SerializeField] private AudioSource hatchAudioSource;
    [SerializeField] private AudioClip hatchOpenAudio;
    [SerializeField] private GameObject[] hatchOpenFx;
    [SerializeField] private Animator watsonAnimator;
    [SerializeField] private string watsonOpenHatchTrigger = "OpenHatch";
    [SerializeField, Min(0f)] private float watsonOpenHatchDelay = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] sherlockFirstDialogue;
    [SerializeField] private Lvl3DialogueLine[] sherlockRepeatDialogue;
    [SerializeField] private Lvl3DialogueLine[] watsonInRoomDialogue;
    [SerializeField] private Lvl3DialogueLine[] watsonOpenDialogue;

    [Header("Watson Presence")]
    [SerializeField] private bool isWatsonInRoom;
    [SerializeField, Min(1f)] private float watsonReactionTurnSpeed = 180f;

    [Header("After Opening")]
    [SerializeField] private GameObject[] nextInteractionGameObjects;
    [SerializeField] private Interactable interactable;

    private bool opened;
    private bool sherlockHasExaminedHatch;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => sherlockFirstDialogue;

    public bool IsWatsonInRoom => isWatsonInRoom;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player == null)
            return;

        if (opened)
        {
            PlayDialogue(player, sherlockRepeatDialogue);
            return;
        }

        if (IsWatsonPlayer(player))
        {
            StartCoroutine(OpenHatch(player));
            return;
        }

        if (isWatsonInRoom)
        {
            PlayDialogue(player, watsonInRoomDialogue);
            StartCoroutine(RotateWatsonTowardHatch());
            return;
        }

        if (!sherlockHasExaminedHatch)
        {
            sherlockHasExaminedHatch = true;
            PlayDialogue(player, sherlockFirstDialogue);
            return;
        }

        PlayDialogue(player, sherlockRepeatDialogue);
    }

    public void SetWatsonInRoom(bool value)
    {
        isWatsonInRoom = value;

        if (value && interactable != null)
            interactable.rotateWatsonToInteraction = true;
    }

    private static bool IsWatsonPlayer(PlayerController player)
    {
        if (player == null)
            return false;

        if (player.playerCharacter == PlayerCharacter.Watson)
            return true;

        return SwitchCharacter.Instance != null &&
               SwitchCharacter.Instance.activePlayerIndex == 1;
    }

    private IEnumerator RotateWatsonTowardHatch()
    {
        PlayerController watson = null;
        foreach (PlayerController candidate in FindObjectsOfType<PlayerController>())
        {
            if (candidate != null && candidate.playerCharacter == PlayerCharacter.Watson)
            {
                watson = candidate;
                break;
            }
        }

        if (watson == null)
            yield break;

        Vector3 direction = transform.position - watson.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        while (Quaternion.Angle(watson.transform.rotation, targetRotation) > 0.1f)
        {
            watson.transform.rotation = Quaternion.RotateTowards(
                watson.transform.rotation,
                targetRotation,
                watsonReactionTurnSpeed * Time.deltaTime);
            yield return null;
        }

        watson.transform.rotation = targetRotation;
    }

    private IEnumerator OpenHatch(PlayerController watson)
    {
        opened = true;
        ActivateNextInteractions();
        PlayDialogue(watson, watsonOpenDialogue);

        Animator activeWatsonAnimator = watsonAnimator != null
            ? watsonAnimator
            : watson.GetComponentInChildren<Animator>();
        if (activeWatsonAnimator != null && !string.IsNullOrWhiteSpace(watsonOpenHatchTrigger))
            activeWatsonAnimator.SetTrigger(watsonOpenHatchTrigger);

        if (watsonOpenHatchDelay > 0f)
            yield return new WaitForSeconds(watsonOpenHatchDelay);

        if (hatchAudioSource != null && hatchOpenAudio != null)
            hatchAudioSource.PlayOneShot(hatchOpenAudio);

        foreach (GameObject fx in hatchOpenFx)
        {
            if (fx != null)
                fx.SetActive(true);
        }

        if (hatchLid != null)
        {
            Vector3 startEuler = hatchLid.localEulerAngles;
            Vector3 targetEuler = new Vector3(openEulerX, startEuler.y, startEuler.z);

            while (Quaternion.Angle(hatchLid.localRotation, Quaternion.Euler(targetEuler)) > 0.05f)
            {
                hatchLid.localRotation = Quaternion.RotateTowards(
                    hatchLid.localRotation,
                    Quaternion.Euler(targetEuler),
                    openSpeed * Time.deltaTime);
                yield return null;
            }

            hatchLid.localRotation = Quaternion.Euler(targetEuler);
        }

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            Collider hatchCollider = interactable.GetComponent<Collider>();
            if (hatchCollider != null)
                hatchCollider.enabled = false;
        }
    }

    private void ActivateNextInteractions()
    {
        foreach (GameObject nextObject in nextInteractionGameObjects)
        {
            if (nextObject != null)
                nextObject.SetActive(true);
        }
    }
}
