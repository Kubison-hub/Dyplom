using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_LibraryBook : MonoBehaviour, IVioletRoomInteractionGate
{
    private Interactable interactable;
    public bool performed;

    [Header("Audio")]
    [SerializeField] private AudioSource useAudio;
    [SerializeField, Min(0f)] private float audioCooldown = 0.45f;
    private float nextAudioTime;

    [Header("Violet Gate")]
    [SerializeField] private Int_VioletDialog violetDialog;
    [SerializeField] private Transform waitInteractionPoint;
    [SerializeField, TextArea] private string violetPresentText =
        "Lepiej by\u0142oby pomyszkowa\u0107 tu w samotno\u015bci.";

    private bool isWalkingToWaitPoint;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (useAudio == null)
            useAudio = GetComponent<AudioSource>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;

        if (useAudio != null && Time.time >= nextAudioTime)
        {
            useAudio.Play();
            nextAudioTime = Time.time + audioCooldown;
        }

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText("137, otwarta książka na 137 stronie.", "");

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

    private System.Collections.IEnumerator ShowVioletTextAtWaitPoint(PlayerController player)
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
