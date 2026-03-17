using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Piano : MonoBehaviour
{
    
    public Transform interactionPoint;

    [SerializeField] private Transform pianoLid;
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

    public bool firstPlay = true;
    public float volume = 1f;

    public AudioSource hit;
    public AudioSource playMusic;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    private void Update()
    {
        if (isAnimating && pianoLid != null)
        {
            pianoLid.localRotation = Quaternion.Slerp(pianoLid.localRotation, targetAngle, animationSpeed * Time.deltaTime);

            if (Quaternion.Angle(pianoLid.localRotation, targetAngle) < 0.5f)
            {
                pianoLid.localRotation = targetAngle;
                isAnimating = false;
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
    }

    private void UsePiano(PlayerController player)
    {
        if (!isLidOpen) OpenCover();
        else DoRandomAction(player);
    }

    private void OpenCover()
    {
        if (Quaternion.Angle(pianoLid.localRotation, openAngle) < 1f) return;

        animationSpeed = 5;
        targetAngle = openAngle;
        isAnimating = true;
        isLidOpen = true;
    }

    private void CloseCover()
    {
        if (Quaternion.Angle(pianoLid.localRotation, closeAngle) < 1f) return;

        animationSpeed = 5;
        targetAngle = closeAngle;
        isAnimating = true;
        isLidOpen = false;
    }

    private void DoRandomAction(PlayerController player)
    {
        int action;

        if (player.playerCharacter == PlayerCharacter.Sherlock)
        {

            if (firstPlay)
            {
                action = 2;
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
