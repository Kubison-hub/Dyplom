using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_BasementDoor : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    public bool canOpen = false;

    [Header("Door")]
    [SerializeField] private Transform door;
    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private AudioSource openAudio;

    [Header("Text")]
    [SerializeField] private string firstInteractionTitle = "Musi istnieć sposób, aby otworzyć te drzwi.";
    [SerializeField] private string firstInteractionDescription = "Ślady małej Ethel prowadzą wgłąb ciemnej piwnicy.";
    [SerializeField] private string lockedTitle = "Zamknięte.";
    [SerializeField] private string lockedDescription = "Mechanizm musi być ukryty gdzieś w pobliżu.";
    [SerializeField] private string openedTitle = "Udało się!";
    [SerializeField] private string openedDescription = "";

    private Quaternion openRotation;
    private Coroutine openCoroutine;
    private bool firstInteraction = true;
    private bool isOpen = false;

    private void Reset()
    {
        SetupInteractable();
    }

    private void OnValidate()
    {
        SetupInteractable();
    }

    private void Start()
    {
        SetupInteractable();

        if (door == null)
        {
            door = transform;
        }

        openRotation = Quaternion.Euler(openEuler);
    }

    public void PerformInteraction(PlayerController player)
    {
        player.currentInteractable = null;

        if (isOpen)
            return;

        performed = true;

        if (firstInteraction)
        {
            firstInteraction = false;
            ShowTopText(firstInteractionTitle, firstInteractionDescription);

            if (interactable != null)
            {
                interactable.AddClue(0);
            }

            return;
        }

        if (!canOpen)
        {
            ShowTopText(lockedTitle, lockedDescription);
            return;
        }

        ShowTopText(openedTitle, openedDescription);

        if (openCoroutine == null)
        {
            openCoroutine = StartCoroutine(OpenDoor());
        }
    }

    public void OpenDoorFromMechanism()
    {
        if (isOpen)
            return;

        ShowTopText(openedTitle, openedDescription);

        if (openCoroutine == null)
        {
            openCoroutine = StartCoroutine(OpenDoor());
        }
    }

    private IEnumerator OpenDoor()
    {
        isOpen = true;

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.interactiveShader = null;
        }

        if (openAudio != null)
        {
            openAudio.Play();
        }

        while (Quaternion.Angle(door.localRotation, openRotation) > 0.5f)
        {
            door.localRotation = Quaternion.RotateTowards(
                door.localRotation,
                openRotation,
                openSpeed * Time.deltaTime
            );

            yield return null;
        }

        door.localRotation = openRotation;
        openCoroutine = null;
    }

    private void ShowTopText(string title, string description)
    {
        if (PlayerTopText.Instance != null)
        {
            PlayerTopText.Instance.ShowTopText(title, description);
        }
        else
        {
            Debug.Log($"{title} {description}");
        }
    }

    private void SetupInteractable()
    {
        lvl3_LayerUtility.SetOutlinedObjectsLayer(gameObject);

        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.lvl3_int_BasementDoor);
        }
    }
}
