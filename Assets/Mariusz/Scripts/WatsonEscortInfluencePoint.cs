using UnityEngine;

[DisallowMultipleComponent]
public class WatsonEscortInfluencePoint : MonoBehaviour
{
    public enum InfluenceMode
    {
        LookAtPoint,
        FixedDirection
    }

    [Header("Influence")]
    [SerializeField] private InfluenceMode influenceMode = InfluenceMode.LookAtPoint;
    [SerializeField, Min(0.01f)] private float range = 3f;
    [SerializeField, Min(0f)] private float strength = 1f;
    [SerializeField, Min(0.01f)] private float falloff = 1f;
    [SerializeField] private bool isActive = true;

    public bool TryGetInfluence(Vector3 position, out Vector3 direction, out float weight)
    {
        direction = influenceMode == InfluenceMode.LookAtPoint
            ? transform.position - position
            : transform.forward;
        direction.y = 0f;
        weight = 0f;

        if (!isActive || direction.sqrMagnitude < 0.0001f)
            return false;

        float distance = Vector3.Distance(
            new Vector3(position.x, 0f, position.z),
            new Vector3(transform.position.x, 0f, transform.position.z));
        if (distance > range)
            return false;

        float normalizedDistance = 1f - Mathf.Clamp01(distance / range);
        weight = strength * Mathf.Pow(normalizedDistance, falloff);
        if (weight <= 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? new Color(0.3f, 0.9f, 0.65f, 0.24f) : new Color(0.45f, 0.45f, 0.45f, 0.16f);
        Gizmos.DrawWireSphere(transform.position, range);

        Vector3 arrowDirection = influenceMode == InfluenceMode.FixedDirection ? transform.forward : Vector3.up;
        Gizmos.color = isActive ? new Color(0.35f, 1f, 0.72f, 0.9f) : Color.gray;
        Gizmos.DrawRay(transform.position, arrowDirection.normalized * Mathf.Min(range * 0.35f, 1.2f));
        Gizmos.DrawSphere(transform.position, 0.08f);
    }
}
