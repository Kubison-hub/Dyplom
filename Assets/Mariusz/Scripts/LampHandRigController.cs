using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Smoothly enables a character's lamp-holding rig only while an active lamp is parented to its LampHolder.
/// </summary>
public class LampHandRigController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rig lampHandRig;
    [Tooltip("LampHolder belonging to the character driven by this rig.")]
    [SerializeField] private Transform lampHolder;

    [Header("Blend")]
    [Tooltip("Time in seconds for the rig weight to move from 0 to 1 or back.")]
    [SerializeField, Min(0.01f)] private float blendDuration = 0.5f;

    private void Reset()
    {
        if (lampHandRig == null)
            lampHandRig = GetComponent<Rig>();
    }

    private void Awake()
    {
        if (lampHandRig == null)
            lampHandRig = GetComponent<Rig>();
    }

    private void Update()
    {
        if (lampHandRig == null)
            return;

        float targetWeight = IsLampHeld() ? 1f : 0f;
        lampHandRig.weight = Mathf.MoveTowards(
            lampHandRig.weight,
            targetWeight,
            Time.deltaTime / blendDuration);
    }

    private bool IsLampHeld()
    {
        if (lampHolder == null)
            return false;

        foreach (Transform child in lampHolder)
        {
            if (child.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }
}
