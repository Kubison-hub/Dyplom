using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueNode rootNode;

    [Header("Ranges")]
    [Tooltip("When inside this radius the exclamation (!) appears above NPC.")]
    public float exclamationRange = 5f;

    [Tooltip("When inside this radius player can press interact key to start dialog.")]
    public float interactRange = 3f;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Optional: auto start when entering interactRange")]
    public bool autoStartOnEnter = false;

    [Header("Visuals")]
    [Tooltip("World-space '!' object. You can create a small sprite/3D model and put it as a child, assign it here.")]
    public GameObject exclamationSign;

    private Transform player;
    private bool playerInInteractRange = false;

    private void Start()
    {
        player = Camera.main ? Camera.main.transform : null; // fallback; usually player transform will be set by manager
        if (exclamationSign != null) exclamationSign.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Exclamation management
        if (exclamationSign != null)
            exclamationSign.SetActive(dist <= exclamationRange);

        // Interact range
        bool insideInteract = dist <= interactRange;
        if (insideInteract && !playerInInteractRange)
        {
            playerInInteractRange = true;
            if (autoStartOnEnter)
            {
                StartDialogue();
            }
        }
        else if (!insideInteract && playerInInteractRange)
        {
            playerInInteractRange = false;
        }

        // Manual interact
        if (playerInInteractRange && Input.GetKeyDown(interactKey))
        {
            StartDialogue();
        }
    }

    private void StartDialogue()
    {
        if (rootNode == null) return;
        DialogueManager.Instance.StartDialogue(rootNode, this);
    }

    // Optional: helper for editor: draw gizmos to visualize ranges
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, exclamationRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
