public interface IInteractionApproachGate
{
    bool CanApproachInteraction(PlayerController player);
    void ShowApproachBlockedText(PlayerController player);
}
