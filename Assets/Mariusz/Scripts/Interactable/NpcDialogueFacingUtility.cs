using System.Collections;
using UnityEngine;

public static class NpcDialogueFacingUtility
{
    public static IEnumerator FacePlayer(Transform npc, Transform player, float turnSpeed, float tolerance)
    {
        if (npc == null || player == null)
            yield break;

        Vector3 direction = player.position - npc.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        while (Quaternion.Angle(npc.rotation, targetRotation) > tolerance)
        {
            npc.rotation = Quaternion.RotateTowards(
                npc.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
            yield return null;
        }

        npc.rotation = targetRotation;
    }
}
