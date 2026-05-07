using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lvl2_Int_Mirror : MonoBehaviour
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

    public string text = "Zamkniête, powinienem móc to otworzyæ";

    [Header("LockPick")]
    [SerializeField] private LockPickMinigameController minigamePrefab;
    [SerializeField] private Transform minigameTransform;
    [SerializeField] private Camera playerCamera;
    private LockPickMinigameController currentMinigame;
    [SerializeField] private LockPickAudioController audioController;

    [SerializeField] private lvl2_Int_Book book;
    [SerializeField] private AudioSource cabinetCloseAudio;

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
            cabinetCloseAudio.Play();
            StartCoroutine(AddText());
            firsInteraction = false;
            player.currentInteractable = null;
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

    private IEnumerator AddText()
    {
        
        ClueManager.Instance.SherlockText.text = text;
        yield return new WaitForSeconds(3);
        ClueManager.Instance.SherlockText.text = "";
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
