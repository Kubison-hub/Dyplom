using UnityEngine;
using UnityEngine.InputSystem;

public class Piano : MonoBehaviour
{
    public float interactionPointOffset = 1f;
    
    [SerializeField] private Transform pianoLid;
    [SerializeField] private AudioClip pianoMusicAudio;
    [SerializeField] private AudioClip hitLid;

   
    [SerializeField] private float animationSpeed = 10f;

    private Interactable interactable;
    private PlayerInput playerInput;

    public bool isLidOpen = false;
    public bool isAnimating = false;

    private Quaternion openAngle = Quaternion.Euler(145, 0, 0);
    private Quaternion closeAngle = Quaternion.Euler(0, 0, 0);
    private Quaternion targetAngle;

    public bool firstPlay = true;

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
        MovePlayerToInteractionPoint(player);
        playerInput = player.GetComponent<PlayerInput>();
    }

    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Vector3 forward = transform.forward;
        Vector3 basePoint = transform.position - forward * interactionPointOffset;
        basePoint.y = player.transform.position.y;

        Vector3 targetPosition = basePoint;

        if (!CheckPositionEmpty(basePoint, 0.5f, player.gameObject))
        {
            targetPosition += transform.right * 1f;
        }

        player.currentInteractable = interactable;

        player.targetPosition = targetPosition;
        player.isWalking = true;
    }

    private bool CheckPositionEmpty(Vector3 position, float radius, GameObject ignoreObject = null)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius);

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;
            if (ignoreObject != null && hit.gameObject == ignoreObject) continue;

            if (hit.GetComponent<PlayerController>() != null)
            {
                return false;
            }
        }

        return true;
    }

    public void PerformInteraction(PlayerController player)
    {
        Debug.Log("Perform Interaction " + gameObject.name);

        UsePiano(player);
    }

    private void UsePiano(PlayerController player)
    {

        if (!isLidOpen)
        {
            OpenCover();
        }
        else
        {
            DoRandomAction(player);
        }
    }

    private void OpenCover()
    {
        if (Quaternion.Angle(pianoLid.rotation, openAngle) < 1f) return;

        animationSpeed = 5;
        targetAngle = openAngle;
        isAnimating = true;
        isLidOpen = true;
    }

    private void CloseCover()
    {
        if (Quaternion.Angle(pianoLid.rotation, closeAngle) < 1f) return;

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
            AudioSource.PlayClipAtPoint(hitLid, transform.position);
    }

    private void PlayMusic()
    {
        if (pianoMusicAudio != null)
            AudioSource.PlayClipAtPoint(pianoMusicAudio, transform.position);
    }
}
