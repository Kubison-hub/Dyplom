using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public static class PlayerControllerTeleportExtensions
{
    /// <summary>
    /// Clears the movement coroutine and NavMesh path that brought a player to
    /// an interaction, so they cannot keep rotating toward its old location
    /// after a scripted teleport.
    /// </summary>
    public static void CancelInteractionForTeleport(this PlayerController player)
    {
        if (player == null)
            return;

        player.StopAllCoroutines();
        ClearPendingInteractionState(player);

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    public static void ResumeAfterTeleport(this PlayerController player)
    {
        if (player == null)
            return;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }
    }

    public static void LockRotationAfterTeleport(this PlayerController player, Quaternion rotation, float duration)
    {
        if (player == null)
            return;

        TeleportRotationLock rotationLock = player.GetComponent<TeleportRotationLock>();
        if (rotationLock == null)
            rotationLock = player.gameObject.AddComponent<TeleportRotationLock>();

        rotationLock.Begin(rotation, duration);
    }

    private static void ClearPendingInteractionState(PlayerController player)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        foreach (FieldInfo field in player.GetType().GetFields(flags))
        {
            string fieldName = field.Name.ToLowerInvariant();
            bool interactionState = fieldName.Contains("interaction") ||
                                    fieldName.Contains("interactable") ||
                                    fieldName.Contains("rotation") ||
                                    fieldName.Contains("moving");

            if (!interactionState)
                continue;

            if (field.FieldType == typeof(bool))
            {
                field.SetValue(player, false);
            }
            else if (field.FieldType == typeof(Interactable) ||
                     field.FieldType == typeof(Transform) ||
                     field.FieldType == typeof(GameObject))
            {
                field.SetValue(player, null);
            }
            else if (field.FieldType == typeof(Quaternion))
            {
                field.SetValue(player, player.transform.rotation);
            }
        }
    }
}

internal sealed class TeleportRotationLock : MonoBehaviour
{
    private Quaternion lockedRotation;
    private float unlockTime;

    public void Begin(Quaternion rotation, float duration)
    {
        lockedRotation = rotation;
        unlockTime = Time.time + Mathf.Max(0.05f, duration);
        transform.rotation = lockedRotation;
    }

    private void LateUpdate()
    {
        transform.rotation = lockedRotation;

        if (Time.time >= unlockTime)
            Destroy(this);
    }
}
