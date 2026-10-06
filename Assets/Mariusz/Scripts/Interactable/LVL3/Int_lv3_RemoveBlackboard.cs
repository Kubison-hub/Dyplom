using System.Collections;
using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;

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
        // Po wczytaniu zapisu ten obiekt zostaje wlaczony razem z grupa poziomu,
        // wiec Start() wykonuje sie DOPIERO po zakonczeniu wczytywania. Bez
        // ponizszego warunku zapalalby z powrotem czarne plyty, ktore gracz
        // juz usunal. 'hasTriggered' jest przywracane przez system zapisu.
        if (hasTriggered)
        {
            ApplyTriggeredStateImmediately();
            return;
        }

        if (!activateObjectsOnStart)
            return;

        var discoveryStates = SaveLoadManager.GetBlackboardDiscoveryStates();
        foreach (GameObject target in objectsToActivateOnStart)
        {
            if (target != null)
            {
                bool revealed;
                target.SetActive(!discoveryStates.TryGetValue(target, out revealed) || !revealed);
            }
        }
    }

    // Czy gracz juz usunal ta plyte (dla systemu zapisu).
    public bool HasTriggered => hasTriggered;

    // Obiekt plyty, ktora ten wyzwalacz usuwa (dla systemu zapisu).
    public GameObject BlackBoard => blackBoardToRemove;

    // Ustawia koncowy stan od razu, bez animacji wygaszania.
    // Uzywane po wczytaniu zapisu, gdy plyta byla juz usunieta.
    //
    // Podmieniamy material na wygaszony i zerujemy alfe, a dopiero potem
    // gasimy obiekt. Dzieki temu nawet jesli inny skrypt zapali plyte
    // po wczytaniu, pozostanie ona calkowicie przezroczysta.
    public void ApplyTriggeredStateImmediately()
    {
        hasTriggered = true;

        if (blackBoardToRemove == null)
            return;

        Renderer blackBoardRenderer = blackBoardToRemove.GetComponent<Renderer>();

        if (blackBoardRenderer != null)
        {
            if (blackBoardFadeMaterial != null)
                blackBoardRenderer.material = blackBoardFadeMaterial;

            Material material = blackBoardRenderer.material;
            string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

            if (material.HasProperty(colorProperty))
            {
                Color color = material.GetColor(colorProperty);
                color.a = 0f;
                material.SetColor(colorProperty, color);
            }

            blackBoardRenderer.enabled = false;
        }

        blackBoardToRemove.SetActive(false);

        Debug.Log("Int_lv3_RemoveBlackboard: przywrocono usunieta plyte '" +
                  blackBoardToRemove.name + "'.", this);
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
