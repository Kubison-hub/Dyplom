using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_SecretLever : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    public bool canInteract = false;

    [Header("Door")]
    [SerializeField] private lvl3_int_BasementDoor doorToOpen;

    [Header("Lever")]
    [SerializeField] private Transform lever;
    [SerializeField] private Vector3 pulledEuler = new Vector3(-45f, 0f, 0f);
    [SerializeField] private float pullSpeed = 160f;
    [SerializeField] private float playerRotateSpeed = 240f;
    [SerializeField] private AudioSource leverAudio;

    private Coroutine pullCoroutine;
    private Coroutine playerRotateCoroutine;
    private bool isPulled = false;

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

        if (lever == null)
        {
            lever = transform;
        }

        SetCanInteract(canInteract);
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;

        if (interactable != null)
        {
            interactable.isInteractableActive = value;
        }
    }

    public void PerformInteraction(PlayerController player)
    {
        player.currentInteractable = null;

        if (!canInteract)
        {
            ShowTopText("Ani drgnie.", "Najpierw trzeba odslonic mechanizm.");
            return;
        }

        if (isPulled)
            return;

        performed = true;
        GetComponent<Interactable>()?.MarkCompleted();
        ShowTopText("Mechanizm ruszyl.", "Drzwi sie otwieraja.");

        if (playerRotateCoroutine == null)
        {
            playerRotateCoroutine = StartCoroutine(RotatePlayer(player.transform));
        }

        if (doorToOpen != null)
        {
            doorToOpen.OpenDoorFromMechanism();
        }
        else
        {
            Debug.LogWarning($"{name}: Door to open reference is missing.");
        }

        if (pullCoroutine == null)
        {
            pullCoroutine = StartCoroutine(PullLever());
        }
    }

    private IEnumerator PullLever()
    {
        isPulled = true;
        SetCanInteract(false);

        if (leverAudio != null)
        {
            leverAudio.Play();
        }

        Quaternion targetRotation = Quaternion.Euler(pulledEuler);

        while (Quaternion.Angle(lever.localRotation, targetRotation) > 0.5f)
        {
            lever.localRotation = Quaternion.RotateTowards(
                lever.localRotation,
                targetRotation,
                pullSpeed * Time.deltaTime
            );

            yield return null;
        }

        lever.localRotation = targetRotation;
        pullCoroutine = null;
    }

    private IEnumerator RotatePlayer(Transform playerTransform)
    {
        Quaternion targetRotation = playerTransform.rotation * Quaternion.Euler(0f, 180f, 0f);

        while (Quaternion.Angle(playerTransform.rotation, targetRotation) > 0.5f)
        {
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                targetRotation,
                playerRotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        playerTransform.rotation = targetRotation;
        playerRotateCoroutine = null;
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
        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.lvl3_int_SecretLever);
        }
    }
}
