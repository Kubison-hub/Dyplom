using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class ConclusionCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cardType;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public Image image;
    public GameObject cardPanel;
    public GameObject questionFXPrefab;
    public float displayDuration = 5f;
    private Coroutine autoCloseCoroutine;

    private Transform cameraTransform;
    

    public void ShowConclusion(Conclusions_SO conlcusion)
    {
        StartCoroutine(WaitForCard(0.1f, conlcusion.type, conlcusion.displayName, conlcusion.shortDescription));
    }

    public void ShowQuestConclusion(QuestConclusions_SO conlcusion)
    {
        StartCoroutine(WaitForCard(10, conlcusion.type, conlcusion.displayName, conlcusion.shortDescription));
    }

    private void SetupCard(string typeText, string titleText, string descriptionText)
    {
        cardType.text = typeText;
        title.text = titleText;
        description.text = descriptionText;

        cardPanel.SetActive(true);

        CanvasGroup cg = cardPanel.GetComponent<CanvasGroup>();

        if (cg != null)
        {
            cg.alpha = 0; 
            StartCoroutine(FadeInAndOut(cg));
        }

        // questionFXPrefab.SetActive(true);
        //VisualEffect fx = questionFXPrefab.GetComponent<VisualEffect>();
        //if (fx != null ) { fx.Play(); }
    }

    private IEnumerator WaitForCard(float time, string typeText, string titleText, string descriptionText)
    {
        yield return new WaitForSeconds(time);
        //SetupCard(typeText, titleText, descriptionText);
        

    }

    private IEnumerator FadeInAndOut(CanvasGroup cg)
    {
        yield return new WaitForSeconds(5);

        while (cg.alpha < 1) { cg.alpha += Time.deltaTime * 2; yield return null; }

        yield return new WaitForSeconds(5);

        while (cg.alpha > 0)
        {
            cg.alpha -= Time.deltaTime * 2;
            yield return null;
        }


        

        questionFXPrefab.SetActive(false);
        cardPanel.SetActive(false);
        autoCloseCoroutine = null;
    }

}
