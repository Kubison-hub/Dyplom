using System.Collections;
using UnityEngine;

public class Int_Globus : MonoBehaviour, IVioletRoomInteractionGate
{
    private Interactable interactable;

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;

    [Header("Spin")]
    [SerializeField] private Transform globeRotationTarget;
    [SerializeField, Min(0.01f)] private float spinDuration = 1.5f;
    [SerializeField] private float totalSpinDegrees = -810f;
    [SerializeField] private AnimationCurve spinCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3.5f),
        new Keyframe(1f, 1f, 0f, 0f));
    [SerializeField] private int_Globus_Button globeButton;
    [SerializeField] private AudioSource spinAudio;

    [Header("Violet Gate")]
    [SerializeField] private Int_VioletDialog violetDialog;
    [SerializeField] private Transform waitInteractionPoint;
    [SerializeField, TextArea] private string violetPresentText =
        "Lepiej by\u0142oby pomyszkowa\u0107 tu w samotno\u015bci.";

    private Vector3 baseLocalEulerAngles;
    private Collider interactionCollider;
    private bool firstInteraction = true;
    private bool isSpinning;
    private bool isWalkingToWaitPoint;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();

        if (globeRotationTarget == null)
            globeRotationTarget = transform;

        baseLocalEulerAngles = globeRotationTarget.localEulerAngles;

        if (globeButton != null)
            globeButton.SetButtonAvailable(false);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isSpinning)
            return;

        if (player != null)
            player.currentInteractable = null;

        if (firstInteraction)
        {
            firstInteraction = false;

            if (cameraController != null)
                cameraController.SetZoomState(CameraZoomState.Narrow);

            if (interactable != null)
                interactable.isInteractableActive = true;

            StartCoroutine(ShowMechanismText());
            return;
        }


        StartCoroutine(SpinGlobe());
    }

    public bool RedirectWhenVioletIsInRoom(PlayerController player)
    {
        // Sherlock may always inspect the mechanism once. Violet only blocks the actual spin.
        if (firstInteraction || !IsVioletInRoom())
            return false;

        if (player == null)
            return true;

        player.currentInteractable = null;

        if (waitInteractionPoint == null)
        {
            ShowVioletPresentText();
            return true;
        }

        if (!isWalkingToWaitPoint)
        {
            isWalkingToWaitPoint = true;
            player.MoveToPoint(waitInteractionPoint.position);
            StartCoroutine(ShowVioletTextAtWaitPoint(player));
        }

        return true;
    }

    private bool IsVioletInRoom()
    {
        if (violetDialog == null)
            violetDialog = Int_VioletDialog.Instance;

        return violetDialog != null && violetDialog.isVioletInRoom;
    }

    private IEnumerator ShowVioletTextAtWaitPoint(PlayerController player)
    {
        while (player != null && player.navMeshAgent != null)
        {
            bool arrived = !player.navMeshAgent.pathPending &&
                           player.navMeshAgent.remainingDistance <= player.navMeshAgent.stoppingDistance + 0.05f &&
                           (!player.navMeshAgent.hasPath || player.navMeshAgent.velocity.sqrMagnitude <= 0.01f);
            if (arrived)
                break;

            yield return null;
        }

        if (player != null)
            ShowVioletPresentText();

        isWalkingToWaitPoint = false;
    }

    private void ShowVioletPresentText()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(violetPresentText, "");
    }

    private IEnumerator ShowMechanismText()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText("W tym globusie jest jakis mechanizm.", "");

        yield return new WaitForSeconds(2f);

        if (interactable != null)
            interactable.isInteractableActive = true;
    }

    private IEnumerator SpinGlobe()
    {
        isSpinning = true;

        if (spinAudio != null)
            spinAudio.Play();

        if (interactable != null)
            interactable.isInteractableActive = false;

        float startSpinDegrees = globeRotationTarget.localEulerAngles.y;
        float targetSpinDegrees = startSpinDegrees + totalSpinDegrees;
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / spinDuration);
            float curveProgress = spinCurve != null ? spinCurve.Evaluate(progress) : progress;
            float currentAngle = Mathf.Lerp(startSpinDegrees, targetSpinDegrees, curveProgress);

            globeRotationTarget.localEulerAngles = new Vector3(
                baseLocalEulerAngles.x,
                baseLocalEulerAngles.y + currentAngle,
                baseLocalEulerAngles.z);
            yield return null;
        }

        globeRotationTarget.localEulerAngles = new Vector3(
            baseLocalEulerAngles.x,
            baseLocalEulerAngles.y + targetSpinDegrees,
            baseLocalEulerAngles.z);
        isSpinning = false;

        if (interactable != null)
            interactable.isInteractableActive = true;

        if (globeButton != null)
            globeButton.SetButtonAvailable(true);
    }

    public void DeactivateGlobeInteraction()
    {
        isSpinning = false;

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }
}
