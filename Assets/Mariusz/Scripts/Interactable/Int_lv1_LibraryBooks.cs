using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_LibraryBooks : MonoBehaviour, IVioletRoomInteractionGate
{
    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private float horizontalAxis = 116.8f;
    [SerializeField] private float orbitSpeed = 5f;

    [Header("Narration")]
    [SerializeField] private float topTextDelay = 0.5f;
    [SerializeField, TextArea] private string topText = "Jak szuka\u0107, to tylko tutaj...";

    [Header("Violet Gate")]
    [SerializeField] private Int_VioletDialog violetDialog;
    [SerializeField] private Transform waitInteractionPoint;
    [SerializeField, TextArea] private string violetPresentText =
        "Lepiej by\u0142oby pomyszkowa\u0107 tu w samotno\u015bci.";

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;
    private bool isWalkingToWaitPoint;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;

        cameraController?.SetZoomState(CameraZoomState.Medium);
        cameraController?.OrbitHorizontalAxisTo(horizontalAxis, orbitSpeed);
        StartCoroutine(ShowTopTextAfterDelay());

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

        if (player != null)
            player.currentInteractable = null;
    }

    public bool RedirectWhenVioletIsInRoom(PlayerController player)
    {
        if (!IsVioletInRoom())
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

    private IEnumerator ShowTopTextAfterDelay()
    {
        yield return new WaitForSeconds(topTextDelay);

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");
    }
}
