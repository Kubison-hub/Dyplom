using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_SecretLeverWall : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;

    [Header("Secret")]
    [SerializeField] private Transform secretLid;
    [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 0f, -0.35f);
    [SerializeField] private float openSpeed = 0.8f;
    [SerializeField] private lvl3_int_SecretLever secretLever;
    [SerializeField] private AudioSource openAudio;

    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private Coroutine openCoroutine;
    private int interactionCount = 0;
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

        if (secretLid == null)
        {
            secretLid = transform;
        }

        closedLocalPosition = secretLid.localPosition;
        openLocalPosition = closedLocalPosition + openLocalOffset;

        if (secretLever != null)
        {
            secretLever.SetCanInteract(false);
        }
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        player.currentInteractable = null;

        interactionCount++;

        if (interactionCount == 1)
        {
            ShowTopText("Podejrzana sciana", "Ta sciana wyglada co najmniej podejrzanie.");
        }
        else
        {
            ShowTopText("Hmm", "");
        }
    }

    public void OpenSecret()
    {
        if (isOpen)
            return;

        if (openCoroutine == null)
        {
            openCoroutine = StartCoroutine(OpenSecretCoroutine());
        }
    }

    private IEnumerator OpenSecretCoroutine()
    {
        isOpen = true;
        interactable?.MarkCompleted();

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.interactiveShader = null;
        }

        if (openAudio != null)
        {
            openAudio.Play();
        }

        while (Vector3.Distance(secretLid.localPosition, openLocalPosition) > 0.01f)
        {
            secretLid.localPosition = Vector3.MoveTowards(
                secretLid.localPosition,
                openLocalPosition,
                openSpeed * Time.deltaTime
            );

            yield return null;
        }

        secretLid.localPosition = openLocalPosition;

        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider wallCollider in colliders)
        {
            wallCollider.enabled = false;
        }

        if (secretLever != null)
        {
            secretLever.SetCanInteract(true);
        }

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
        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.lvl3_int_SecretLeverWall);
        }
    }
}
