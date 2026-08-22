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
    [SerializeField] private GameObject activateOnFirstVioletGate;
    [SerializeField] private Transform intPointWatson;

    [Header("Violet Present Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] violetPresentDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Lepiej by\u0142oby pomyszkowa\u0107 tu w samotno\u015bci.",
            duration = 3f
        }
    };
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;

    [Header("Debug")]
    [SerializeField] private bool debugIsVioletInRoom;
    [SerializeField] private bool debugIsLibraryObserved;
    [SerializeField] private bool debugVioletGateRedirected;

    private Vector3 baseLocalEulerAngles;
    private Collider interactionCollider;
    private bool firstInteraction = true;
    private bool isSpinning;
    private bool isWalkingToWaitPoint;
    private bool firstVioletGateTriggered;

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
        if (firstInteraction)
            return false;

        debugVioletGateRedirected = IsVioletInRoom();
        if (!debugVioletGateRedirected)
            return false;

        if (player == null)
            return true;

        player.currentInteractable = null;

        if (waitInteractionPoint == null)
        {
            isWalkingToWaitPoint = true;
            StartCoroutine(PlayVioletGateDialogue());
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

        violetDialog?.RefreshLibraryAwareness();
        debugIsVioletInRoom = violetDialog != null && violetDialog.isVioletInRoom;
        debugIsLibraryObserved = violetDialog != null && violetDialog.IsLibraryObserved;
        return debugIsVioletInRoom || debugIsLibraryObserved;
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
            yield return PlayVioletGateDialogue();

    }

    private IEnumerator PlayVioletGateDialogue()
    {
        if (violetPresentDialogue != null)
        {
            foreach (Lvl3DialogueLine line in violetPresentDialogue)
            {
                string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
                string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
                PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);

                AudioSource voiceSource = line.speaker == Lvl3DialogueSpeaker.Sherlock
                    ? sherlockVoiceSource
                    : watsonVoiceSource;
                if (voiceSource != null && line.voiceClip != null)
                {
                    voiceSource.Stop();
                    voiceSource.PlayOneShot(line.voiceClip);
                }

                yield return new WaitForSeconds(line.duration > 0f ? line.duration : 3f);
                PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
            }
        }

        ActivateFirstVioletGateObject();
        MoveWatsonToGatePoint();
        isWalkingToWaitPoint = false;
    }

    private void ActivateFirstVioletGateObject()
    {
        if (firstVioletGateTriggered)
            return;

        firstVioletGateTriggered = true;
        if (activateOnFirstVioletGate != null)
            activateOnFirstVioletGate.SetActive(true);
    }

    private void MoveWatsonToGatePoint()
    {
        if (intPointWatson == null || SwitchCharacter.Instance == null ||
            SwitchCharacter.Instance.players == null || SwitchCharacter.Instance.players.Length < 2 ||
            SwitchCharacter.Instance.players[1] == null)
            return;

        PlayerController watson = SwitchCharacter.Instance.players[1].GetComponent<PlayerController>();
        watson?.MoveToPoint(intPointWatson.position);
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
