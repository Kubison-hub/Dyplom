using System.Collections;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.Video;


public class lvl2_Int_SelmaDoor : Lvl3InteractionDialogueBase
{
    
    public GameObject door;
    public GameObject room;
    public GameObject blackBoard;
    
    public bool performed = false;

    private Coroutine openCoroutine;
    private Coroutine fadeCoroutine;

    private Quaternion openRotation;
    private bool isOpen = false;

    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    [SerializeField] private CameraController cameraController;
    [SerializeField] private string unlockedCameraPresetName = "Medium";


    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("LockPick")]
    [SerializeField] private LockPickMinigameController minigamePrefab;
    [SerializeField] private Transform minigameTransform;
    [SerializeField] private Camera playerCamera;
    private LockPickMinigameController currentMinigame;
    [SerializeField] private LockPickAudioController audioController;

    private Interactable interactable;
    public bool setActiveOnStart = false;
    private bool firsInteraction = true;
    [SerializeField] private Material newMaterial;

    [SerializeField] private AudioSource doorclosedAudio;

    [Header("First Interaction Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] firstInteractionDialogue;

    [Header("Lockpick Tutorial Popup")]
    [SerializeField] private bool showLockpickTutorialPopup = true;
    [SerializeField] private string lockpickTutorialTitle = "Otwieranie Zamków";
    [SerializeField, TextArea] private string lockpickTutorialText =
        "Sherlock potrafi otwierać zamki, może to wymagać cierpliwości...";
    [SerializeField] private VideoClip lockpickTutorialVideoClip;
    private bool waitingForFirstInteractionDialogue;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => firstInteractionDialogue;
    private void Start()
    {
        openRotation = Quaternion.Euler(openEuler);
        interactable = GetComponent<Interactable>();
        
        transform.parent = door.transform;
        if (!setActiveOnStart)
        {
            room.SetActive(false);
        }
        
        blackBoard.SetActive(true);
        
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");

        if (firsInteraction)
        {
            if (doorclosedAudio != null)
                doorclosedAudio.Play();

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
            cameraController.SetZoomState(CameraZoomState.Narrow);
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
        room.SetActive(true);


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

    private IEnumerator FadeAlpha(Material material, float targetAlpha, float duration)
    {
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        if (!material.HasProperty(colorProperty))
        {
            Debug.LogError("Materiał nie ma _Color ani _BaseColor");
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            material.SetColor(colorProperty, color);

            yield return null;
        }

        color.a = targetAlpha;
        material.SetColor(colorProperty, color);
        blackBoard.SetActive(false);
        

        fadeCoroutine = null;
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
            HandleClosed,6 ,5
        );

    }
    private void HandleUnlocked()
    {
        audioController.PlayUnlock();
        if (cameraController != null &&
            (string.IsNullOrWhiteSpace(unlockedCameraPresetName) ||
             !cameraController.SetZoomPreset(unlockedCameraPresetName)))
            cameraController.ReturnToPreviousZoomState();
        ClueManager.Instance.isLockpicking = false;

        Debug.Log("Door unlocked");

        interactable.interactiveShader = null;

        if (!isOpen && openCoroutine == null)
        {
            openCoroutine = StartCoroutine(OpenDoor());
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        Renderer renderer = blackBoard.GetComponent<Renderer>();
        renderer.material = newMaterial;


        fadeCoroutine = StartCoroutine(FadeAlpha(renderer.material, 0f, fadeDuration));

        interactable.isInteractableActive = false;

        if (currentMinigame != null)
            Destroy(currentMinigame.gameObject);

    }

    private void HandleClosed()
    {
        ClueManager.Instance.isLockpicking = false;
        audioController.PlayReset();
        cameraController.ReturnToPreviousZoomState();

        if (currentMinigame != null)
            Destroy(currentMinigame.gameObject);

        currentMinigame = null;
    }

    //FIX
}
