using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Piano : MonoBehaviour
{
    
    public Transform interactionPoint;

    [SerializeField] private Transform[] pianoLids;
    [SerializeField] private AudioClip pianoMusicAudio;
    [SerializeField] private AudioClip hitLid;

    [SerializeField] private float animationSpeed = 10f;

    private Interactable interactable;
    private PlayerInput playerInput;

    public bool isLidOpen = false;
    public bool isAnimating = false;

    private Quaternion openAngle = Quaternion.Euler(0, 90, -150);
    private Quaternion closeAngle = Quaternion.Euler(0, 90, 0);
    private Quaternion targetAngle;

    public bool firstPlay = false;
    public float volume = 1f;

    public AudioSource hit;
    public AudioSource playMusic;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    private void Update()
    {
        if (isAnimating)
        {
            foreach (var pianoLid in pianoLids)
            {
                pianoLid.localRotation = Quaternion.Slerp(pianoLid.localRotation, targetAngle, animationSpeed * Time.deltaTime);

                if (Quaternion.Angle(pianoLid.localRotation, targetAngle) < 0.5f)
                {
                    pianoLid.localRotation = targetAngle;
                    isAnimating = false;
                }
            }

            
        }
    }

    public void Interact(PlayerController player)
    {
        player.currentInteractable = interactable;
        player.currentInteractionPoint = interactionPoint;

        player.MoveToInteractable();    
    } 

    public void PerformInteraction(PlayerController player)
    {
        Debug.Log("Perform Interaction " + gameObject.name);
        UsePiano(player);
        player.currentInteractable = null;
    }

    private void UsePiano(PlayerController player)
    {
        if (!isLidOpen) OpenCover();
        else DoRandomAction(player);
    }

    private void OpenCover()
    {
        foreach (var pianoLid in pianoLids)
        {
            if (Quaternion.Angle(pianoLid.localRotation, openAngle) < 1f) return;

            animationSpeed = 5;
            targetAngle = openAngle;
            isAnimating = true;
            isLidOpen = true;
        }

        
    }

    private void CloseCover()
    {
        foreach (var pianoLid in pianoLids)
        {
            if (Quaternion.Angle(pianoLid.localRotation, closeAngle) < 1f) return;

            animationSpeed = 5;
            targetAngle = closeAngle;
            isAnimating = true;
            isLidOpen = false;
        }

            
    }

    private void DoRandomAction(PlayerController player)
    {
        int action;

        if (player.playerCharacter == PlayerCharacter.Sherlock)
        {

            if (firstPlay)
            {
                action = 2;
                ChangeCameraPresetForFirstPlay(player);
                firstPlay = false;
            }
            else action = 3;
        }
           
        else
            action = Random.Range(1, 2);

        switch (action)
        {
            case 1:
                CloseCover();
                break;
            case 2:
                PlayMusic();
                break;
            case 3:
                HitCover();
                break;
        }
    }

    private static void ChangeCameraPresetForFirstPlay(PlayerController player)
    {
        CameraController activeCamera = null;
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null && switchCharacter.playersCamera != null)
        {
            int activeIndex = switchCharacter.activePlayerIndex;
            if (activeIndex >= 0 && activeIndex < switchCharacter.playersCamera.Length &&
                switchCharacter.playersCamera[activeIndex] != null)
            {
                activeCamera = switchCharacter.playersCamera[activeIndex]
                    .GetComponent<CameraController>();
                if (activeCamera == null)
                {
                    activeCamera = switchCharacter.playersCamera[activeIndex]
                        .GetComponentInChildren<CameraController>(true);
                }
            }
        }

        if (activeCamera == null && player != null)
        {
            activeCamera = player.GetComponent<CameraController>();
            if (activeCamera == null)
                activeCamera = player.GetComponentInChildren<CameraController>(true);
        }

        activeCamera?.SetZoomInOneStep();
    }

    private void HitCover()
    {
        animationSpeed = 30;
        targetAngle = closeAngle;
        isAnimating = true;
        isLidOpen = false;

        if (hitLid != null)
            playMusic.Stop();
            hit.Play();
    }

    private void PlayMusic()
    {
        if (pianoMusicAudio != null)
            playMusic.Play();
    }
}
