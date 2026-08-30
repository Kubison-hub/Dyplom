using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Post-teleport Ethel sequence. Keep this GameObject inactive until the ladder activates it.
/// </summary>
public class Int_lv4_EthelRun : MonoBehaviour
{
    [Header("Ethel")]
    [SerializeField] private Transform ethel;
    [SerializeField] private NavMeshAgent ethelAgent;
    [SerializeField] private Animator ethelAnimator;
    [SerializeField] private Transform secondMarker;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;
    [SerializeField, Min(0.1f)] private float runningSpeed = 3.5f;

    [Header("Animation")]
    [SerializeField] private string isWalkingParameter = "IsWalking";
    [SerializeField] private string isRunningParameter = "IsRunning";

    [Header("Opening Sequence")]
    [Tooltip("Objects disabled immediately when Ethel's final run begins.")]
    [SerializeField] private GameObject[] deactivateOnSequenceStart;
    [SerializeField, Min(0f)] private float initialWaitDuration = 2f;
    [SerializeField, TextArea] private string ethelText = "Tędy!";
    [SerializeField] private AudioSource ethelVoiceSource;
    [SerializeField] private AudioClip ethelVoiceClip;
    [SerializeField, Min(0.1f)] private float textDuration = 2f;
    [SerializeField] private GameObject triggerToActivate;

    private bool sequenceStarted;

    private void Awake()
    {
        if (ethel == null)
            ethel = transform;

        if (ethelAgent == null && ethel != null)
            ethelAgent = ethel.GetComponent<NavMeshAgent>();

        if (ethelAnimator == null && ethel != null)
            ethelAnimator = ethel.GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (!sequenceStarted)
            StartCoroutine(RunSequence());
    }

    public void BeginRun()
    {
        if (!sequenceStarted)
            StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        sequenceStarted = true;

        if (deactivateOnSequenceStart != null)
        {
            foreach (GameObject target in deactivateOnSequenceStart)
            {
                if (target != null && target != gameObject)
                    target.SetActive(false);
            }
        }

        if (initialWaitDuration > 0f)
            yield return new WaitForSeconds(initialWaitDuration);

        PlayerTopText.Instance?.ShowEthelTopText(ethelText, textDuration);
        if (ethelVoiceSource != null && ethelVoiceClip != null)
            ethelVoiceSource.PlayOneShot(ethelVoiceClip);

        if (triggerToActivate != null)
            triggerToActivate.SetActive(true);

        yield return MoveEthelTo(secondMarker);
    }

    private IEnumerator MoveEthelTo(Transform marker)
    {
        if (ethel == null || ethelAgent == null || marker == null)
        {
            Debug.LogWarning($"{name}: assign Ethel, Ethel Agent and the final movement marker.", this);
            yield break;
        }

        if (!ethelAgent.isOnNavMesh ||
            !NavMesh.SamplePosition(marker.position, out NavMeshHit destination, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Ethel or marker '{marker.name}' is outside the NavMesh.", this);
            yield break;
        }

        ethelAgent.speed = runningSpeed;
        ethelAgent.isStopped = false;
        ethelAgent.updateRotation = true;
        SetRunningAnimation(true);
        ethelAgent.SetDestination(destination.position);

        while (ethelAgent.pathPending)
            yield return null;

        if (ethelAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            SetRunningAnimation(false);
            Debug.LogWarning($"{name}: Ethel cannot reach '{marker.name}'.", this);
            yield break;
        }

        while (ethelAgent.remainingDistance > Mathf.Max(arrivalDistance, ethelAgent.stoppingDistance))
            yield return null;

        ethelAgent.ResetPath();
        ethelAgent.isStopped = true;
        ethel.transform.rotation = marker.rotation;
        SetRunningAnimation(false);
    }

    private void SetRunningAnimation(bool isRunning)
    {
        if (ethelAnimator == null)
            return;

        if (!string.IsNullOrWhiteSpace(isWalkingParameter))
            ethelAnimator.SetBool(isWalkingParameter, false);

        if (!string.IsNullOrWhiteSpace(isRunningParameter))
            ethelAnimator.SetBool(isRunningParameter, isRunning);
    }
}
