using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    public NPCDialogue npcDialogue;

    [Header("Trigger Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 3f;

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (player == null || npcDialogue == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(interactKey))
        {
            // ✅ Corrected call
            DialogueManager.Instance.StartDialogue(npcDialogue.rootNode, npcDialogue);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
