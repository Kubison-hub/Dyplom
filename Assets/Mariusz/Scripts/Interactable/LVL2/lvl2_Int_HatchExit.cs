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
    [Tooltip("Disabled immediately when this hatch interaction begins.")]
    [SerializeField] private GameObject[] deactivateOnInteractionStart;
    [Tooltip("Activated immediately before teleporting the player into the hidden room.")]
    [SerializeField] private GameObject[] activateBeforeTeleport;
    [Tooltip("Activated immediately before teleporting the player into the hidden room.")]
    [SerializeField] private GameObject[] blackboardsToActivateBeforeTeleport;

    [Header("Level 1 Return Music")]
    [SerializeField] private GameMusicManager gameMusicManager;
    [SerializeField] private AudioClip levelOneReturnMusic;
    [Tooltip("When disabled, the playlist resumes after this track finishes.")]
    [SerializeField] private bool loopLevelOneReturnMusic;

    [Header("Hidden Passage Blackboard Restore")]
    [Tooltip("The Level 1 hidden-passage blackboard that must be visible after the first successful hatch exit.")]
    [SerializeField] private GameObject hiddenPassageBlackboard;
    [Tooltip("Optional replacement material applied to the hidden-passage blackboard on the first successful hatch exit.")]
    [SerializeField] private Material hiddenPassageBlackboardMaterial;

    [SerializeField] private int requiredLetters = 3;
    private int lettersCollected = 0;
    private bool hiddenPassageBlackboardRestored;
    private bool levelOneReturnMusicPlayed;

    public void AddLetter()
    {
        lettersCollected++;
        CluesLog.Instance?.RegisterUpperFloorEvidence();

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

        DeactivateObjectsAtInteractionStart();

        TryExit(player);

        
        player.currentInteractable = null;

        
        
    }

    private void DeactivateObjectsAtInteractionStart()
    {
        if (deactivateOnInteractionStart == null)
            return;

        foreach (GameObject gameObjectToDeactivate in deactivateOnInteractionStart)
        {
            if (gameObjectToDeactivate != null)
                gameObjectToDeactivate.SetActive(false);
        }
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
        CluesLog.Instance?.RemoveUpperFloorEvidenceObjective();

        if (levelOneRoot != null)
            levelOneRoot.SetActive(true);

        SetObjectsActiveBeforeTeleport(activateBeforeTeleport);
        SetBlackboardsActiveBeforeTeleport();
        RestoreHiddenPassageBlackboard();
        TeleportToHiddenRoom(player);
        PlayLevelOneReturnMusic();
        
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

    private void PlayLevelOneReturnMusic()
    {
        if (levelOneReturnMusicPlayed || levelOneReturnMusic == null)
            return;

        if (gameMusicManager == null)
            gameMusicManager = FindFirstObjectByType<GameMusicManager>();

        if (gameMusicManager == null)
            return;

        levelOneReturnMusicPlayed = true;
        gameMusicManager.ChangeMusic(levelOneReturnMusic, loopLevelOneReturnMusic);
    }

    private void SetBlackboardsActiveBeforeTeleport()
    {
        SetObjectsActiveBeforeTeleport(blackboardsToActivateBeforeTeleport);
    }

    private void RestoreHiddenPassageBlackboard()
    {
        if (hiddenPassageBlackboardRestored || hiddenPassageBlackboard == null)
            return;

        hiddenPassageBlackboardRestored = true;
        hiddenPassageBlackboard.SetActive(true);

        foreach (Renderer renderer in hiddenPassageBlackboard.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (hiddenPassageBlackboardMaterial != null)
                renderer.material = hiddenPassageBlackboardMaterial;

            foreach (Material material in renderer.materials)
            {
                if (material == null)
                    continue;

                RestoreMaterialAlpha(material, "_BaseColor");
                RestoreMaterialAlpha(material, "_Color");
            }
        }
    }

    private static void RestoreMaterialAlpha(Material material, string propertyName)
    {
        if (!material.HasProperty(propertyName))
            return;

        Color color = material.GetColor(propertyName);
        color.a = 1f;
        material.SetColor(propertyName, color);
    }

    private static void SetObjectsActiveBeforeTeleport(GameObject[] objectsToActivate)
    {
        if (objectsToActivate == null)
            return;

        foreach (GameObject target in objectsToActivate)
        {
            if (target != null)
                target.SetActive(true);
        }
    }

    private IEnumerator AddText()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(text, string.Empty);

        yield return new WaitForSeconds(3);
        interactable.isInteractableActive = true;

    }

    

}
