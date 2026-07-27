using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_Fireplace : MonoBehaviour
{
    [SerializeField] private DetectiveIdeaPoint firePlaceIdeaPoint;
    [SerializeField, TextArea] private string topText =
        "Przy zgaszonym świetle kominek był jedynym źródłem światła w salonie.";

    [Header("Completion FX")]
    [SerializeField, Min(0f)] private float completionQuestionFxRate = 7f;
    [SerializeField, Min(0f)] private float questionFxFadeDelay = 1.5f;
    [SerializeField] private bool deactivateQuestionFxAfterFade = true;

    private Interactable interactable;
    private bool performed;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        FindFirePlaceIdeaPointIfNeeded();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");

        FindFirePlaceIdeaPointIfNeeded();
        firePlaceIdeaPoint?.RevealFromExternalSource();

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXRate(completionQuestionFxRate);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
            StartCoroutine(FadeOutQuestionFX());
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private IEnumerator FadeOutQuestionFX()
    {
        if (questionFxFadeDelay > 0f)
            yield return new WaitForSeconds(questionFxFadeDelay);

        if (interactable == null)
            yield break;

        interactable.SetQuestionFXRate(0f);

        if (deactivateQuestionFxAfterFade && interactable.questionVFX != null)
            interactable.questionVFX.gameObject.SetActive(false);
    }

    private void FindFirePlaceIdeaPointIfNeeded()
    {
        if (firePlaceIdeaPoint != null)
            return;

        DetectiveIdeaPoint[] ideaPoints = FindObjectsByType<DetectiveIdeaPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (DetectiveIdeaPoint point in ideaPoints)
        {
            if (point.ideaId != "IdeaPoint_FirePlace" && point.gameObject.name != "IdeaPoint_FirePlace")
                continue;

            firePlaceIdeaPoint = point;
            firePlaceIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;
            return;
        }

        Debug.LogWarning("Int_lv1_Fireplace: IdeaPoint_FirePlace was not found.");
    }
}
