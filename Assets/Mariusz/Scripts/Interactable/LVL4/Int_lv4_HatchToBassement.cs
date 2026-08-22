using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves Sherlock and Watson through an opened hatch onto basement NavMesh markers.
/// </summary>
public class Int_lv4_HatchToBassement : MonoBehaviour
{
    [Header("Basement Start Positions")]
    [SerializeField] private Transform sherlockBasementStartPoz;
    [SerializeField] private Transform watsonBasementStartPoz;
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;
    [SerializeField] private float transitionDelay = 0.1f;
    [SerializeField] private Interactable interactable;

    [Header("Level Transition")]
    [Tooltip("Root GameObject of Level 3, enabled before the characters are teleported into the basement.")]
    [SerializeField] private GameObject levelThreeRoot;
    [Tooltip("Root GameObject of Level 2, disabled after the characters reach Level 3.")]
    [SerializeField] private GameObject levelTwoRoot;

    private bool isTransitioning;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        StartCoroutine(MoveCharactersToBasement());
    }

    private IEnumerator MoveCharactersToBasement()
    {
        if (levelThreeRoot != null)
            levelThreeRoot.SetActive(true);

        if (transitionDelay > 0f)
            yield return new WaitForSeconds(transitionDelay);

        TeleportPlayer(PlayerCharacter.Sherlock, sherlockBasementStartPoz);
        TeleportPlayer(PlayerCharacter.Watson, watsonBasementStartPoz);

        if (levelTwoRoot != null)
            levelTwoRoot.SetActive(false);

        isTransitioning = false;
    }

    private void TeleportPlayer(PlayerCharacter character, Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning($"{name}: missing basement start position for {character}.", this);
            return;
        }

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController candidate in players)
        {
            if (candidate == null || candidate.playerCharacter != character)
                continue;

            Vector3 targetPosition = destination.position;
            if (NavMesh.SamplePosition(destination.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                targetPosition = hit.position;
            else
                Debug.LogWarning($"{name}: {character} basement marker is outside NavMesh.", destination);

            NavMeshAgent agent = candidate.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(targetPosition);
                agent.ResetPath();
            }
            else
            {
                candidate.transform.position = targetPosition;
            }

            candidate.transform.rotation = destination.rotation;
            return;
        }

        Debug.LogWarning($"{name}: could not find {character} PlayerController.", this);
    }
}
