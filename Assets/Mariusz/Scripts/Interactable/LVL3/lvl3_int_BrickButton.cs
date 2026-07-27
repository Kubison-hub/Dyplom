using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_BrickButton : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    [SerializeField] private lvl3_int_SecretLeverWall secretLeverWall;

    [Header("Press")]
    [SerializeField] private Transform button;
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, 0f, -0.08f);
    [SerializeField] private float pressSpeed = 0.35f;

    private Vector3 startLocalPosition;
    private Vector3 pressedLocalPosition;
    private Coroutine pressCoroutine;

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

        if (button == null)
        {
            button = transform;
        }

        startLocalPosition = button.localPosition;
        pressedLocalPosition = startLocalPosition + pressedLocalOffset;
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        ShowTopText("Aha!", "Cos pstryknelo.");

        if (secretLeverWall != null)
        {
            secretLeverWall.OpenSecret();
        }
        else
        {
            Debug.LogWarning($"{name}: Secret lever wall reference is missing.");
        }

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
        }

        if (pressCoroutine == null)
        {
            pressCoroutine = StartCoroutine(PressButton());
        }

        player.currentInteractable = null;
    }

    private IEnumerator PressButton()
    {
        while (Vector3.Distance(button.localPosition, pressedLocalPosition) > 0.005f)
        {
            button.localPosition = Vector3.MoveTowards(
                button.localPosition,
                pressedLocalPosition,
                pressSpeed * Time.deltaTime
            );

            yield return null;
        }

        button.localPosition = pressedLocalPosition;
        pressCoroutine = null;
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
            interactable.SetInteractionType(InteractionType.lvl3_int_BrickButton);
        }
    }
}
