using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(SphereCollider))]
public sealed class MirrorCameraZone : MonoBehaviour
{
    [SerializeField] private Transform sherlock;
    [SerializeField] private CameraController sherlockCamera;
    [SerializeField] private Transform mirrorSurface;
    [Tooltip("Use a dedicated Rig with a head Multi-Aim Constraint aimed at Sherlock's reflection.")]
    [SerializeField] private Rig mirrorHeadRig;
    [SerializeField, Range(0f, 1f)] private float headLookWeight = 0.35f;
    [SerializeField, Min(0.01f)] private float headBlendSpeed = 1.5f;

    [Header("Head Look Falloff")]
    [SerializeField, Range(-1f, 1f)] private float noLookBelowDot = 0.2f;
    [SerializeField, Range(-1f, 1f)] private float fullLookAboveDot = 0.75f;
    [SerializeField, Range(0f, 1f)] private float fullWeightRadiusFraction = 0.5f;

    private readonly HashSet<Collider> sherlockColliders = new HashSet<Collider>();
    private SphereCollider zoneSphere;

    private void Awake()
    {
        zoneSphere = GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sherlock == null || !other.transform.IsChildOf(sherlock))
            return;

        sherlockColliders.Add(other);
        sherlockCamera?.SetMirrorZoneActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!sherlockColliders.Remove(other) || sherlockColliders.Count > 0)
            return;

        sherlockCamera?.SetMirrorZoneActive(false);
    }

    private void Update()
    {
        if (mirrorHeadRig == null)
            return;

        float targetWeight = sherlockColliders.Count > 0 ? headLookWeight * GetLookFactor() : 0f;
        mirrorHeadRig.weight = Mathf.MoveTowards(mirrorHeadRig.weight, targetWeight, headBlendSpeed * Time.deltaTime);
    }

    private float GetLookFactor()
    {
        if (sherlock == null || mirrorSurface == null || zoneSphere == null)
            return 0f;

        Vector3 directionToMirror = mirrorSurface.position - sherlock.position;
        directionToMirror.y = 0f;
        Vector3 bodyForward = sherlock.forward;
        bodyForward.y = 0f;
        if (directionToMirror.sqrMagnitude < 0.0001f || bodyForward.sqrMagnitude < 0.0001f)
            return 0f;

        float facingDot = Vector3.Dot(bodyForward.normalized, directionToMirror.normalized);
        float facingFactor = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(noLookBelowDot, Mathf.Max(noLookBelowDot + 0.01f, fullLookAboveDot), facingDot));

        Vector3 localPosition = transform.InverseTransformPoint(sherlock.position) - zoneSphere.center;
        float radialDistance = new Vector2(localPosition.x, localPosition.z).magnitude /
                               Mathf.Max(0.001f, zoneSphere.radius);
        float edgeFactor = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(Mathf.Min(fullWeightRadiusFraction, 0.99f), 1f, radialDistance));

        return facingFactor * edgeFactor;
    }

    private void OnDisable()
    {
        sherlockColliders.Clear();
        sherlockCamera?.SetMirrorZoneActive(false);
        if (mirrorHeadRig != null)
            mirrorHeadRig.weight = 0f;
    }
}
