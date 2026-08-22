using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Int_lv3_RemoveBlackboard : MonoBehaviour
{
    [Header("Blackboard")]
    [SerializeField] private GameObject blackBoardToRemove;
    [SerializeField] private Material blackBoardFadeMaterial;
    [SerializeField, Min(0f)] private float fadeDelay = 0f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 1f;

    [Header("Start Activation")]
    [SerializeField] private bool activateObjectsOnStart;
    [SerializeField] private GameObject[] objectsToActivateOnStart;

    private bool hasTriggered;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void OnValidate()
    {
        ConfigureTriggerCollider();
    }

    private void Start()
    {
        if (!activateObjectsOnStart)
            return;

        foreach (GameObject target in objectsToActivateOnStart)
        {
            if (target != null)
                target.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !IsPlayableCharacter(other))
            return;

        hasTriggered = true;
        StartCoroutine(FadeBlackBoardOut());
    }

    private IEnumerator FadeBlackBoardOut()
    {
        if (fadeDelay > 0f)
            yield return new WaitForSeconds(fadeDelay);

        if (blackBoardToRemove == null)
            yield break;

        Renderer blackBoardRenderer = blackBoardToRemove.GetComponent<Renderer>();
        if (blackBoardRenderer == null)
        {
            blackBoardToRemove.SetActive(false);
            yield break;
        }

        if (blackBoardFadeMaterial != null)
            blackBoardRenderer.material = blackBoardFadeMaterial;

        Material material = blackBoardRenderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (!material.HasProperty(colorProperty))
        {
            blackBoardToRemove.SetActive(false);
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            material.SetColor(colorProperty, color);
            yield return null;
        }

        color.a = 0f;
        material.SetColor(colorProperty, color);
        blackBoardToRemove.SetActive(false);
    }

    private static bool IsPlayableCharacter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return false;

        return player.CompareTag("PlayerA") || player.CompareTag("PlayerB");
    }

    private void ConfigureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }
}
