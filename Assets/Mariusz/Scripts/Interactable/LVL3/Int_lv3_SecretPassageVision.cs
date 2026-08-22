using UnityEngine;

/// <summary>
/// Shows a Vision Eye reconstruction of a closed passage until the real passage is opened.
/// </summary>
public class Int_lv3_SecretPassageVision : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private GameObject realDoorGameObject;
    [SerializeField] private GameObject visionDoor;
    [SerializeField] private Animator visionDoorAnimator;
    [SerializeField] private string openTrigger = "Open";

    [Header("Real Passage State")]
    [SerializeField] private Int_lv3_ClockSecretPassage realPassage;

    private bool visionWasActive;
    private bool completed;

    private void Awake()
    {
        if (visionDoorAnimator == null && visionDoor != null)
            visionDoorAnimator = visionDoor.GetComponent<Animator>();

        SetDoorStates(false);
    }

    private void OnEnable()
    {
        visionWasActive = false;
        completed = false;
    }

    private void Update()
    {
        if (completed)
            return;

        if (realPassage != null && realPassage.IsOpened)
        {
            CompleteVision();
            return;
        }

        bool visionActive = EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
        if (visionActive == visionWasActive)
            return;

        visionWasActive = visionActive;
        SetDoorStates(visionActive);

        if (visionActive && visionDoorAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            visionDoorAnimator.SetTrigger(openTrigger);
    }

    private void SetDoorStates(bool visionActive)
    {
        if (realDoorGameObject != null)
            realDoorGameObject.SetActive(!visionActive);

        if (visionDoor != null)
            visionDoor.SetActive(visionActive);
    }

    private void CompleteVision()
    {
        completed = true;

        if (realDoorGameObject != null)
            realDoorGameObject.SetActive(true);

        if (visionDoor != null)
            visionDoor.SetActive(false);

        gameObject.SetActive(false);
    }
}
