using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lvl2_Int_Mirror : Lvl3InteractionDialogueBase
{

   

    public GameObject door;
    public GameObject spline1;
    public GameObject spline2;


    public bool performed = false;

    private Coroutine openCoroutine;
    private Coroutine fadeCoroutine;

    private Quaternion openRotation;
    public bool isOpen = false;

    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);

    [SerializeField] private float openSpeed = 120f;

    public GameObject doorSwitcher;
    [SerializeField] private CameraController cameraController;
    public bool canOpen = false;

    private Interactable interactable;
    private bool firsInteraction = true;

    [Header("LockPick")]
    [SerializeField] private LockPickMinigameController minigamePrefab;
    [SerializeField] private Transform minigameTransform;
    [SerializeField] private Camera playerCamera;
    private LockPickMinigameController currentMinigame;
    [SerializeField] private LockPickAudioController audioController;

    [SerializeField] private lvl2_Int_Book book;
    [SerializeField] private AudioSource cabinetCloseAudio;

    [Header("First Interaction Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] firstInteractionDialogue;

    [Header("Lockpick Tutorial Popup")]
    [SerializeField] private bool showLockpickTutorialPopup = true;
    [SerializeField] private string lockpickTutorialTitle = "Otwieranie Zamków";
    [SerializeField, TextArea] private string lockpickTutorialText =
        "Sherlock potrafi otwierać zamki, może to wymagać cierpliwości...";
    [SerializeField] private UnityEngine.Video.VideoClip lockpickTutorialVideoClip;

    private bool waitingForFirstInteractionDialogue;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => firstInteractionDialogue;

    private void Start()
    {
        openRotation = Quaternion.Euler(openEuler);
        interactable = GetComponent<Interactable>();

        transform.parent = door.transform;
        doorSwitcher.SetActive(false);
       
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        //Debug.Log(interactable.name + ", interaction Performed");

        if (firsInteraction)
        {
            if (cabinetCloseAudio != null)
                cabinetCloseAudio.Play();

            interactable.isInteractableActive = false;
            firsInteraction = false;
            player.currentInteractable = null;
            cameraController?.SetZoomState(CameraZoomState.Narrow);

            if (firstInteractionDialogue != null && firstInteractionDialogue.Length > 0)
            {
                waitingForFirstInteractionDialogue = true;
                PlayDialogue(player, firstInteractionDialogue);
            }
            else
            {
                StartCoroutine(ShowLockpickTutorialSequence());
            }
        }
        else
        {
            
            LockPick();
            player.currentInteractable = null;
        }

        player.currentInteractable = null;




        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private IEnumerator OpenDoor()
    {
        isOpen = true;

        doorSwitcher.SetActive(true);


        spline1.gameObject.SetActive(false);

        if (!book.keyFounded)
        {
            spline2.gameObject.SetActive(true);
        }
        



        while (Quaternion.Angle(door.transform.localRotation, openRotation) > 0.5f)
        {
            door.transform.localRotation = Quaternion.RotateTowards(
                door.transform.localRotation,
                openRotation,
                openSpeed * Time.deltaTime
            );

            yield return null;
        }

        door.transform.localRotation = openRotation;
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (!waitingForFirstInteractionDialogue || lines != firstInteractionDialogue)
            return;

        waitingForFirstInteractionDialogue = false;
        StartCoroutine(ShowLockpickTutorialSequence());
    }

    private IEnumerator ShowLockpickTutorialSequence()
    {
        TutorialTimeline tutorialTimeline = TutorialTimeline.Instance;
        if (showLockpickTutorialPopup && tutorialTimeline != null)
        {
            bool tutorialWasShown = tutorialTimeline.TryShowLockpickTutorialPopup(
                lockpickTutorialTitle,
                lockpickTutorialText,
                lockpickTutorialVideoClip);

            if (tutorialWasShown)
            {
                yield return null;
                while (tutorialTimeline.BlocksWorldInput)
                    yield return null;
            }
        }

        interactable.isInteractableActive = true;
    }


    public void LockPick()
    {
        
        cameraController.SetZoomState(CameraZoomState.Narrow);

        if (currentMinigame != null)
            return;

        ClueManager.Instance.isLockpicking = true;

        currentMinigame = Instantiate(
            minigamePrefab,
            minigameTransform.position,
            minigameTransform.rotation
        );

        currentMinigame.Open(
            playerCamera,
            audioController,
            HandleUnlocked,
            HandleClosed,5, 4
        );

    }
    private void HandleUnlocked()
    {
        audioController.PlayUnlock();
        cameraController.ReturnToPreviousZoomState();
        ClueManager.Instance.isLockpicking = false;
        Debug.Log("Door unlocked");

        openCoroutine = StartCoroutine(OpenDoor());
        interactable.isInteractableActive = false;
        

        interactable.interactiveShader = null;

        interactable.isInteractableActive = false;


        if (currentMinigame != null)
            Destroy(currentMinigame.gameObject);
    }

    private void HandleClosed()
    {
        audioController.PlayReset();
        cameraController.ReturnToPreviousZoomState();
        ClueManager.Instance.isLockpicking = false;
        if (currentMinigame != null)
            Destroy(currentMinigame.gameObject);

        currentMinigame = null;
    }

    //FIX



}
