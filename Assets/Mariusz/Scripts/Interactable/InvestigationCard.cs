using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class InvestigationCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cardType;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public Image image;
    public GameObject cardPanel;
    public GameObject panelParent;

    public VisualEffect fx;

    public TextMeshPro SherlockTMP;
    public TextMeshPro WatsonTMP;

    private Coroutine autoCloseCoroutine;

    private Transform cameraTransform;
    void Start()
    {
        cameraTransform = Camera.main.transform;

        
    }
    void LateUpdate()
    {
        if (cardPanel.activeSelf)
        {
            
            panelParent.transform.LookAt(panelParent.transform.position + cameraTransform.rotation * Vector3.forward,
                                         cameraTransform.rotation * Vector3.up);     
        }
    }

    public void ShowClue(Clues_SO clue)
    {



        SetupClueCard(clue, clue.type, clue.displayName, clue.shortDescription, clue.displayDuration);
        
    }

    private void SetupClueCard (Clues_SO clue, string typeText, string titleText, string descriptionText, float duration)
    {
        cardType.text = typeText;
        title.text = titleText;
        description.text = ($"{descriptionText}");

        SherlockTMP = ClueManager.Instance.SherlockText;
        WatsonTMP = ClueManager.Instance.WatsonText;

        //SherlockTMP.text = descriptionText;

        //StartCoroutine(ShowNotification(typeText, titleText, descriptionText, clue));

        cardPanel.SetActive(true);

        CanvasGroup cg = cardPanel.GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 0; StartCoroutine(FadeIn(cg)); }

        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine(cg, duration, typeText, titleText, descriptionText, clue));
        
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        if (fx != null)
        {
            fx.Play();
        }

        while (cg.alpha < 1) { cg.alpha += Time.deltaTime * 2; yield return null; }

    }

    private IEnumerator ShowNotification(string typeText, string titleText, string descriptionText, Clues_SO clue)
    {
        yield return new WaitForSeconds(3);
        SherlockTMP.text = ("");
        yield return new WaitForSeconds(0.1f);
        ClueManager.Instance.ShowNotification(typeText, titleText, descriptionText, clue);
    }

    private IEnumerator AutoCloseRoutine(CanvasGroup cg, float duration, string typeText, string titleText, string descriptionText, Clues_SO clue)
    {
        yield return new WaitForSeconds(5);

       
        if (cg != null)
        {
            while (cg.alpha > 0)
            {
                cg.alpha -= Time.deltaTime * 2;
                yield return null;
            }
        }

        if (fx != null)
        {

            fx.Stop();
           
        }


        cardPanel.SetActive(false);
        autoCloseCoroutine = null;
        Destroy(this.gameObject, 5f);

        yield return new WaitForSeconds(0.5f);

        ClueManager.Instance.ShowNotification(typeText, titleText, descriptionText, clue);
    }

    public void Close() => cardPanel.SetActive(false);

    


}
