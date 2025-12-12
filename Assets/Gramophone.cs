using UnityEngine;
using UnityEngine.InputSystem;

public class Gramophone : MonoBehaviour
{
    public float interactionPointOffset = 1f;
    private Interactable interactable;
    private PlayerInput playerInput;

    private Animator gramophoneAnimator;
    public bool isPlaying = false;
    private void Start()
    {
        interactable = GetComponent<Interactable>();
        gramophoneAnimator = GetComponent<Animator>();

    }
    public void Interact(PlayerController player)
    {
        MovePlayerToInteractionPoint(player);
        playerInput = player.GetComponent<PlayerInput>();
    }

    public void PerformInteraction()
    {
        Debug.Log("Perform Interaction " + gameObject.name);

        PlayGram();
    }

    private void PlayGram()
    {
        if (isPlaying) return;

        gramophoneAnimator.SetTrigger("Play");
        isPlaying = true;
        PlayerTopText.Instance.ShotTopText("Wyglπda na to, øe duchy znowu nawiedzajπ dom", "Porozmawiajmy z Madame Selma");


    }

    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Vector3 npcPos = transform.position;
        Vector3 playerPos = player.transform.position;

        // kierunek od gracza do NPC (na p≥aszczyünie)
        Vector3 dirToNpc = npcPos - playerPos;
        dirToNpc.y = 0f;

        float dist = dirToNpc.magnitude;
        if (dist < 0.001f)
            dirToNpc = transform.forward;      // awaryjnie
        else
            dirToNpc /= dist;                  // normalize

        float stopDistance = interactionPointOffset;

        // punkt: "na linii do NPC, ale w odleg≥oúci stopDistance od NPC"
        Vector3 targetPosition = npcPos - dirToNpc * stopDistance;
        targetPosition.y = playerPos.y;

        // jeúli punkt jest zajÍty, sprÛbuj bokiem wzglÍdem kierunku podejúcia
        if (!CheckPositionEmpty(targetPosition, 0.5f, player.gameObject))
        {
            Vector3 right = Vector3.Cross(Vector3.up, dirToNpc);  // prostopadle do kierunku podejúcia

            Vector3 t1 = targetPosition + right * 1f;
            Vector3 t2 = targetPosition - right * 1f;

            if (CheckPositionEmpty(t1, 0.5f, player.gameObject)) targetPosition = t1;
            else if (CheckPositionEmpty(t2, 0.5f, player.gameObject)) targetPosition = t2;
            // else zostaje oryginalny target
        }

        player.currentInteractable = interactable;
        player.targetPosition = targetPosition;

        // jeúli juø jest wystarczajπco blisko, nie kaø mu iúÊ
        player.isWalking = (dist > stopDistance + 0.05f);
        Debug.Log("Done");
    }
    private bool CheckPositionEmpty(Vector3 position, float radius, GameObject ignoreObject = null)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius);

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;

            if (ignoreObject != null && hit.gameObject == ignoreObject) continue;

            if (hit.GetComponent<PlayerController>() != null)
            {
                return false;
            }
        }

        return true;
    }
}
