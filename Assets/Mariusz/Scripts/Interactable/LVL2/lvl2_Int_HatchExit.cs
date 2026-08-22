using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class lvl2_Int_HatchExit : MonoBehaviour
{
    private Interactable interactable;

    [Header("Top Text")]
    [TextArea] public string text = "Najpierw powinienem dokładniej przeszukać piętro";
    

    public bool performed = false;

    [Header("Hidden Room Teleport")]
    [SerializeField] private Transform hiddenRoomStartPosition;

    [Header("Level Transition")]
    [Tooltip("Root GameObject of Level 1, disabled when the player uses the stairs to reach Level 2.")]
    [SerializeField] private GameObject levelOneRoot;

    [SerializeField] private int requiredLetters = 3;
    private int lettersCollected = 0;

    public void AddLetter()
    {
        lettersCollected++;
        
    }

    public bool HasAllLetters()
    {
        return lettersCollected >= requiredLetters;
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");


        TryExit(player);

        
        player.currentInteractable = null;

        
        
    }

    public void TryExit(PlayerController player)
    {
        if (!HasAllLetters())
        {
            StartCoroutine(AddText());
            Debug.Log("Brakuje listów");
            
            return;
        }
        Debug.Log("Wszystkie listy zebrane. OPUSZCZAM LEVEL.");

        if (levelOneRoot != null)
            levelOneRoot.SetActive(true);

        TeleportToHiddenRoom(player);
        
    }

    private void TeleportToHiddenRoom(PlayerController player)
    {
        if (player == null || hiddenRoomStartPosition == null)
            return;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(hiddenRoomStartPosition.position);
            agent.ResetPath();
        }
        else
        {
            player.transform.position = hiddenRoomStartPosition.position;
        }

        player.transform.rotation = hiddenRoomStartPosition.rotation;
        player.currentInteractable = null;
    }
    private IEnumerator AddText()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(text, string.Empty);

        yield return new WaitForSeconds(3);
        interactable.isInteractableActive = true;

    }

    

}
