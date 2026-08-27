using System.Collections;
using UnityEngine;

public class lvl2_Int_Mirror : Lvl3InteractionDialogueBase
{
    [Header("Door")]
    [SerializeField] private GameObject door;
    [SerializeField] private GameObject spline1;
    [Header("Unused Lever")]
    [Tooltip("Former lever object. It remains disabled because this door now opens with the key.")]
    [SerializeField] private GameObject doorSwitcher;
    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    [SerializeField, Min(0.1f)] private float openSpeed = 120f;
    [SerializeField] private AudioSource doorOpenAudioSource;
    [SerializeField] private AudioClip doorOpenAudio;
    [SerializeField] private AudioSource lockedDoorAudioSource;
    [SerializeField] private AudioClip lockedDoorAudio;

    [Header("Required Key")]
    [SerializeField] private lvl2_Int_Book book;
    [SerializeField] private Lvl3DialogueLine[] missingKeyDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Potrzebujê specjalnego kluczyka.",
            duration = 3f
        }
    };

    public bool performed;
    public bool isOpen;

    private Interactable interactable;
    private Coroutine openCoroutine;
    private Quaternion openRotation;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => missingKeyDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        openRotation = Quaternion.Euler(openEuler);

        if (doorSwitcher != null)
            doorSwitcher.SetActive(false);

        if (door != null)
            transform.parent = door.transform;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isOpen || openCoroutine != null)
            return;

        if (book == null || !book.keyFounded)
        {
            if (lockedDoorAudioSource != null && lockedDoorAudio != null)
                lockedDoorAudioSource.PlayOneShot(lockedDoorAudio);

            PlayDialogue(player, missingKeyDialogue);
            return;
        }

        performed = true;
        if (player != null)
            player.currentInteractable = null;

        openCoroutine = StartCoroutine(OpenDoor());
    }

    private IEnumerator OpenDoor()
    {
        isOpen = true;

        if (doorOpenAudioSource != null && doorOpenAudio != null)
            doorOpenAudioSource.PlayOneShot(doorOpenAudio);

        if (spline1 != null)
            spline1.SetActive(false);

        if (door == null)
        {
            FinishInteraction();
            yield break;
        }

        while (Quaternion.Angle(door.transform.localRotation, openRotation) > 0.5f)
        {
            door.transform.localRotation = Quaternion.RotateTowards(
                door.transform.localRotation,
                openRotation,
                openSpeed * Time.deltaTime);
            yield return null;
        }

        door.transform.localRotation = openRotation;
        FinishInteraction();
    }

    private void FinishInteraction()
    {
        openCoroutine = null;

        if (interactable == null)
            return;

        interactable.isInteractableActive = false;
        interactable.allowQuestionFXWhenInactive = false;
        interactable.SetQuestionFXEagleVisionState(false);

        if (interactable.interactiveShader != null)
            interactable.interactiveShader.SetActive(false);
    }
}
