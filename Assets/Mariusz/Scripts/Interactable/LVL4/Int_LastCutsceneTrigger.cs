using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class Int_LastCutsceneTrigger : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private PlayerController sherlock;
    [SerializeField] private PlayerController watson;
    [SerializeField] private Transform sherlockLastPoint;
    [SerializeField] private Transform watsonLastPoint;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;

    [Header("Camera")]
    [SerializeField] private float cameraHorizontalAxis = -203f;
    [SerializeField, Min(0.1f)] private float cameraHorizontalOrbitSpeed = 6f;

    [Header("Scene Fade Out")]
    [Tooltip("Optional existing full-screen CanvasGroup. When empty, a black overlay is created at runtime.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField, Min(0f)] private float fadeOutDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutHoldDuration = 0.15f;

    [Header("Completion")]
    [Tooltip("Leave false for the final scene: player input remains locked after both actors arrive.")]
    [SerializeField] private bool restoreInputOnComplete;
    [Tooltip("Optional scene loaded after Sherlock and Watson reach their Last Points. The scene must be added to Build Settings.")]
    [SerializeField] private string nextSceneName;

    private bool sequenceStarted;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController triggeringPlayer = other.GetComponentInParent<PlayerController>();
        if (sequenceStarted || triggeringPlayer == null)
            return;

        StartCoroutine(RunFinalSequence(triggeringPlayer));
    }

    private IEnumerator RunFinalSequence(PlayerController triggeringPlayer)
    {
        sequenceStarted = true;
        ResolvePlayers();

        sherlock?.SetTutorialInputLocked(true);
        watson?.SetTutorialInputLocked(true);

        bool sherlockArrived = sherlock == null || sherlockLastPoint == null;
        bool watsonArrived = watson == null || watsonLastPoint == null;
        if (!sherlockArrived)
            StartCoroutine(MoveActorTo(sherlock, sherlockLastPoint, () => sherlockArrived = true));
        if (!watsonArrived)
            StartCoroutine(MoveActorTo(watson, watsonLastPoint, () => watsonArrived = true));

        CameraController triggeringCamera = triggeringPlayer.GetComponent<CameraController>();
        if (triggeringCamera == null)
            triggeringCamera = triggeringPlayer.GetComponentInChildren<CameraController>(true);

        if (triggeringCamera != null)
        {
            triggeringCamera.OrbitHorizontalAxisTo(cameraHorizontalAxis, cameraHorizontalOrbitSpeed);
        }

        while (!sherlockArrived || !watsonArrived)
            yield return null;

        Debug.Log("KONIEC");

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            yield return FadeOutScene();
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        if (restoreInputOnComplete)
        {
            sherlock?.SetTutorialInputLocked(false);
            watson?.SetTutorialInputLocked(false);
        }
    }

    private IEnumerator FadeOutScene()
    {
        if (fadeOutDelay > 0f)
            yield return new WaitForSecondsRealtime(fadeOutDelay);

        CanvasGroup overlay = ResolveFadeCanvasGroup();
        if (overlay == null)
            yield break;

        overlay.gameObject.SetActive(true);
        overlay.blocksRaycasts = true;
        overlay.interactable = true;

        float startAlpha = overlay.alpha;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        overlay.alpha = 1f;
        if (fadeOutHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(fadeOutHoldDuration);
    }

    private CanvasGroup ResolveFadeCanvasGroup()
    {
        if (fadeCanvasGroup != null)
            return fadeCanvasGroup;

        GameObject canvasObject = new GameObject(
            "FinalSceneFadeCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        fadeCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = true;

        return fadeCanvasGroup;
    }

    private IEnumerator MoveActorTo(PlayerController actor, Transform marker, Action onArrived)
    {
        NavMeshAgent agent = actor != null ? actor.GetComponent<NavMeshAgent>() : null;
        if (agent == null || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(marker.position, out NavMeshHit destination, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: {actor?.name ?? "Actor"} cannot reach final marker '{marker.name}'.", this);
            onArrived?.Invoke();
            yield break;
        }

        agent.ResetPath();
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(destination.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.remainingDistance > Mathf.Max(arrivalDistance, agent.stoppingDistance))
                yield return null;
        }
        else
        {
            Debug.LogWarning($"{name}: {actor.name} has no complete path to '{marker.name}'.", this);
        }

        agent.ResetPath();
        agent.isStopped = true;
        actor.transform.rotation = marker.rotation;
        onArrived?.Invoke();
    }

    private void ResolvePlayers()
    {
        if (sherlock == null || watson == null)
        {
            foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (player.playerCharacter == PlayerCharacter.Sherlock)
                    sherlock ??= player;
                else if (player.playerCharacter == PlayerCharacter.Watson)
                    watson ??= player;
            }
        }

    }
}
