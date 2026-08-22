using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public class Int_SetupLastPoints : MonoBehaviour
{
    [Serializable]
    private class NpcLastPoint
    {
        [SerializeField] private Transform npc;
        [SerializeField] private Transform lastPoint;

        public void Teleport(float navMeshSampleRadius, UnityEngine.Object context)
        {
            if (npc == null || lastPoint == null)
                return;

            if (!npc.gameObject.activeSelf)
                npc.gameObject.SetActive(true);

            Vector3 destination = lastPoint.position;
            if (NavMesh.SamplePosition(lastPoint.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                destination = hit.position;

            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>() ?? npc.GetComponentInChildren<NavMeshAgent>(true);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.Warp(destination);
                agent.ResetPath();
            }
            else
            {
                npc.position = destination;
            }

            npc.rotation = lastPoint.rotation;
        }
    }

    [Header("NPC Last Points")]
    [SerializeField] private NpcLastPoint selma = new NpcLastPoint();
    [SerializeField] private NpcLastPoint henry = new NpcLastPoint();
    [SerializeField] private NpcLastPoint george = new NpcLastPoint();
    [SerializeField] private NpcLastPoint violet = new NpcLastPoint();
    [SerializeField] private NpcLastPoint arthur = new NpcLastPoint();
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;

    [Header("After Setup")]
    [Tooltip("Objects enabled once all NPCs have been placed at their Last Points.")]
    [SerializeField] private GameObject[] gameObjectsToActivate;
    [SerializeField] private bool deactivateInteractionAfterSetup = true;

    private Interactable interactable;
    private bool hasBeenSetUp;

    private void Reset() => SetupInteractable();
    private void OnValidate() => SetupInteractable();

    private void Awake()
    {
        SetupInteractable();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (hasBeenSetUp)
            return;

        hasBeenSetUp = true;
        selma.Teleport(navMeshSampleRadius, this);
        henry.Teleport(navMeshSampleRadius, this);
        george.Teleport(navMeshSampleRadius, this);
        violet.Teleport(navMeshSampleRadius, this);
        arthur.Teleport(navMeshSampleRadius, this);

        foreach (GameObject target in gameObjectsToActivate)
        {
            if (target != null)
                target.SetActive(true);
        }

        if (player != null)
            player.currentInteractable = null;

        if (deactivateInteractionAfterSetup && interactable != null)
            interactable.isInteractableActive = false;
    }

    private void SetupInteractable()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_SetupLastPoints);
    }
}
