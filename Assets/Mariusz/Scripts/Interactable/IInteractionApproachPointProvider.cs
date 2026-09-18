using UnityEngine;

public interface IInteractionApproachPointProvider
{
    Transform GetInteractionApproachPoint(PlayerController player, Transform defaultPoint);
}
