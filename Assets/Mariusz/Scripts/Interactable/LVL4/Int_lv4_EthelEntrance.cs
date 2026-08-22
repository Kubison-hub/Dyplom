using UnityEngine;
using UnityEngine.AI;

public class Int_lv4_EthelEntrance : Lvl3InteractionDialogueBase
{
    [Header("State")]
    [SerializeField] private Int_lv4_Hatch hatch;

    [Header("Sherlock Start Position")]
    [SerializeField] private Transform sherlockEthelStartPoz;
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;

    [Header("Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] noReturnDialogue;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => noReturnDialogue;

    public void PerformInteraction(PlayerController player)
    {
        if (player == null)
            return;

        bool isWatsonInRoom = hatch != null && hatch.IsWatsonInRoom;
        if (player.playerCharacter == PlayerCharacter.Sherlock && !isWatsonInRoom)
        {
            TeleportSherlock(player);
            return;
        }

        // The dialogue line should use Speaker: Sherlock, even when Watson clicks.
        PlayDialogue(player, noReturnDialogue);
    }

    private void TeleportSherlock(PlayerController sherlock)
    {
        if (sherlockEthelStartPoz == null)
        {
            Debug.LogWarning($"{name}: Sherlock Ethel Start Poz is not assigned.", this);
            return;
        }

        Vector3 targetPosition = sherlockEthelStartPoz.position;
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            targetPosition = hit.position;
        else
            Debug.LogWarning($"{name}: Sherlock Ethel Start Poz is outside NavMesh.", sherlockEthelStartPoz);

        NavMeshAgent agent = sherlock.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(targetPosition);
            agent.ResetPath();
        }
        else
        {
            sherlock.transform.position = targetPosition;
        }

        sherlock.transform.rotation = sherlockEthelStartPoz.rotation;
    }
}
