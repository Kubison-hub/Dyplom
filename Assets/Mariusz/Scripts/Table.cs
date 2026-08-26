using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Table : MonoBehaviour
{
    public float interactionPointOffset = 1f;
    private Interactable interactable;
    private PlayerInput playerInput;

    [SerializeField] private Transform tableTop;
    [SerializeField] private float rotationDuration = 1.0f;
    private bool isRotating = false;
    
    public bool questDone = false;
    public GameObject key;
    public DoorSwitcher wallSwitcher;
    public GameObject dresser;
    
    public Animator dresserAnimator;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        dresserAnimator = dresser.GetComponent<Animator>();
       
    }
    public void Interact(PlayerController player)
    {
        MovePlayerToInteractionPoint(player);
        playerInput = player.GetComponent<PlayerInput>();
    }

    public void PerformInteraction()
    {
        Debug.Log("Perform Interaction " + gameObject.name);

        UseTable();
    }

    private void UseTable()
    {
        if (questDone) return;

        if (!InventoryManager.Instance.keyInInv)
        {
            PlayerTopText.Instance.ShowTopText("Interesuj¹ce Watsonie, wygl¹da to na jakiœ mechanizm", 
                "czegoœ tu brakuje, Holmes");
        }
        else
        {
            PlayerTopText.Instance.ShowTopText("Figurka pasuje idealnie", "");
            key.SetActive(true);
            
        }


        if (!isRotating)
            StartCoroutine(RotateTableCoroutine());
    }

    private IEnumerator RotateTableCoroutine()
    {
        isRotating = true;
        Debug.Log("StartRotation");

        Quaternion baseRot = tableTop.localRotation;
        float timer = 0f;

        while (timer < rotationDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / rotationDuration);

            float angle = 360f * t; // aktualny k¹t
            tableTop.localRotation = baseRot * Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        if (InventoryManager.Instance.keyInInv)
        {
            dresserAnimator.SetTrigger("Push");
            PlayerTopText.Instance.ShowTopText("", "Sherlock, komoda ustêpuje miejsca do œciany!");
            wallSwitcher.canOpen = true;
            questDone = true;
            InventoryManager.Instance.TryRemoveItem(ItemType.Czerwona);
        }
        

        tableTop.localRotation = baseRot;
        isRotating = false;
    }
    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Vector3 npcPos = transform.position;
        Vector3 playerPos = player.transform.position;

        // kierunek od gracza do NPC (na p³aszczyŸnie)
        Vector3 dirToNpc = npcPos - playerPos;
        dirToNpc.y = 0f;

        float dist = dirToNpc.magnitude;
        if (dist < 0.001f)
            dirToNpc = transform.forward;      // awaryjnie
        else
            dirToNpc /= dist;                  // normalize

        float stopDistance = interactionPointOffset;

        // punkt: "na linii do NPC, ale w odleg³oœci stopDistance od NPC"
        Vector3 targetPosition = npcPos - dirToNpc * stopDistance;
        targetPosition.y = playerPos.y;

        // jeœli punkt jest zajêty, spróbuj bokiem wzglêdem kierunku podejœcia
        if (!CheckPositionEmpty(targetPosition, 0.5f, player.gameObject))
        {
            Vector3 right = Vector3.Cross(Vector3.up, dirToNpc);  // prostopadle do kierunku podejœcia

            Vector3 t1 = targetPosition + right * 1f;
            Vector3 t2 = targetPosition - right * 1f;

            if (CheckPositionEmpty(t1, 0.5f, player.gameObject)) targetPosition = t1;
            else if (CheckPositionEmpty(t2, 0.5f, player.gameObject)) targetPosition = t2;
            // else zostaje oryginalny target
        }

        player.currentInteractable = interactable;
        player.targetPosition = targetPosition;

        // jeœli ju¿ jest wystarczaj¹co blisko, nie ka¿ mu iœæ
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
