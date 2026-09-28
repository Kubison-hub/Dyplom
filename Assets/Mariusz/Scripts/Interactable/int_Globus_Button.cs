using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class int_Globus_Button : MonoBehaviour, IVioletRoomInteractionGate
{
    private Interactable interactable;

    [Header("Result")]
    [SerializeField] private LockPickAudioController audioController;
    [SerializeField] private Animator globusAnimator;
    [SerializeField] private int_LibraryPainting painting;
    [SerializeField] private Int_Globus globe;
    [SerializeField] private GameObject[] activateOnPerformed;

    [Header("Violet Gate")]
    [SerializeField] private Int_VioletDialog violetDialog;
    [SerializeField] private Transform waitInteractionPoint;
    [SerializeField, TextArea] private string violetPresentText =
        "Lepiej by\u0142oby pomyszkowa\u0107 tu w samotno\u015bci.";

    public bool performed;
    private bool isWalkingToWaitPoint;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (globe == null)
            globe = FindFirstObjectByType<Int_Globus>();
    }

    public void SetButtonAvailable(bool available)
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();

        if (interactable != null && !performed)
            interactable.isInteractableActive = available;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        interactable?.MarkCompleted();

        if (audioController != null)
            audioController.PlayUnlock();

        if (globusAnimator != null)
            globusAnimator.SetTrigger("Rotate");

        if (painting != null)
            painting.PushPainting();

        if (globe == null)
            globe = FindFirstObjectByType<Int_Globus>();

        globe?.DeactivateGlobeInteraction();

        if (activateOnPerformed != null)
        {
            foreach (GameObject target in activateOnPerformed)
            {
                if (target != null)
                    target.SetActive(true);
            }
        }

        
        if (interactable != null)
        {
            interactable.interactiveShader = null;
            interactable.isInteractableActive = false;
        }

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
}
