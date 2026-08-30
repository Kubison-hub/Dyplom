using UnityEngine;
using UnityEngine.AI;

public class Int_lv4_EthelEntrance : Lvl3InteractionDialogueBase
{
    [Header("State")]
    [SerializeField] private Int_lv4_Hatch hatch;

    [Header("Sherlock Start Position")]
    [SerializeField] private Transform sherlockEthelStartPoz;
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;
    [SerializeField, Min(0.05f)] private float rotationLockDuration = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] noReturnDialogue;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => noReturnDialogue;

    private bool isTeleporting;

    public void PerformInteraction(PlayerController player)
    {
        if (player == null)
            return;

        bool isWatsonInRoom = hatch != null && hatch.IsWatsonInRoom;
        if (player.playerCharacter == PlayerCharacter.Sherlock && !isWatsonInRoom)
        {
            if (!isTeleporting)
                StartCoroutine(TeleportSherlockAfterInteraction(player));
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
            agent.isStopped = true;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.Warp(targetPosition);
            agent.ResetPath();
            agent.nextPosition = targetPosition;
        }
        else
        {
            sherlock.transform.position = targetPosition;
        }

        sherlock.transform.rotation = sherlockEthelStartPoz.rotation;
    }

    private System.Collections.IEnumerator TeleportSherlockAfterInteraction(PlayerController sherlock)
    {
        isTeleporting = true;

        // Allow PlayerController to finish the current interaction coroutine before
        // clearing its old interaction point and moving it to the hidden room.
        yield return new WaitForEndOfFrame();

        sherlock.CancelInteractionForTeleport();
        TeleportSherlock(sherlock);
        sherlock.ResumeAfterTeleport();
        if (sherlockEthelStartPoz != null)
            sherlock.LockRotationAfterTeleport(sherlockEthelStartPoz.rotation, rotationLockDuration);

        isTeleporting = false;
    }
}
