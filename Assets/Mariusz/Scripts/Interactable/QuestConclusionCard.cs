using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class QuestConclusionCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cardType;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public Image image;
    public GameObject cardPanel;
    public VisualEffect questionmarkEffect;

    public float displayDuration = 5f;
    private Coroutine autoCloseCoroutine;

    private Transform cameraTransform;


    public void ShowQuestConclusion(QuestConclusions_SO conlcusion)
    {
        SetupCard(conlcusion.type, conlcusion.displayName, conlcusion.shortDescription);
    }

    private void SetupCard(string typeText, string titleText, string descriptionText)
    {
        cardType.text = typeText;
        title.text = titleText;
        description.text = ($"      {descriptionText}");

        cardPanel.SetActive(true);

        CanvasGroup cg = cardPanel.GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 0; StartCoroutine(FadeIn(cg)); }

        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine(cg));

    }

    private System.Collections.IEnumerator AutoCloseRoutine(CanvasGroup cg)
    {
        yield return new WaitForSeconds(10);


        if (cg != null)
        {
            while (cg.alpha > 0)
            {
                cg.alpha -= Time.deltaTime * 2;
                yield return null;
            }
        }

         
        if (questionmarkEffect != null)
        {

            questionmarkEffect.Stop();
            //Destroy(ClueVisualFx, 5f);
        }
        cardPanel.SetActive(false);
        autoCloseCoroutine = null;
    }

    public void Close() => cardPanel.SetActive(false);

    private System.Collections.IEnumerator FadeIn(CanvasGroup cg)
    {
        yield return new WaitForSeconds(displayDuration);

        while (cg.alpha < 1) { cg.alpha += Time.deltaTime * 2; yield return null; }

    }


}
