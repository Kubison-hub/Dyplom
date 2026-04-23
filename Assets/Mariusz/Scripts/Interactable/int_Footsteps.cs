using System.Collections;

using UnityEngine;

public class int_Footsteps : MonoBehaviour
{

    public EagleVisionScanner scanner;
    private Interactable interactable;

    public bool performed = false;
    public Transform cardPosition;
    private Collider intCollider;

    public GameObject[] nextInteractions;
    
    [SerializeField] private GameObject intFootPrints;
    public float intFootPrintsFadeDuration = 2;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        intCollider = GetComponent<Collider>();
        

        
        if (intFootPrints == null)
            Debug.LogError("intFootPronts GameObject is null");

        interactable.questionVFX.Stop();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

        ActiveNextInteractions(false);


    }

    private void OnEnable()
    {
        ShowIntFootPrints();
    }

    public void PerformInteraction(PlayerController player)
    {
        interactable.AddClue(0, cardPosition);
        performed = true;
        Debug.Log("Interaction Performed");

        ActiveNextInteractions(true);
        HideIntFootPrints();

        StartCoroutine(ShowTutInfo());

        scanner.footPrints = true;
        interactable.isInteractableActive = false;
        player.currentInteractable = null;
        intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }


    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }
    }

    public void HideIntFootPrints()
    {
        Renderer renderer = intFootPrints.GetComponent<Renderer>();
        Material material = renderer.material;

        Color color = material.GetColor("_BaseColor");


        StartCoroutine(FadeAlphaColor(material, 0.2f, intFootPrintsFadeDuration));
    }

    public void ShowIntFootPrints()
    {
        Renderer renderer = intFootPrints.GetComponent<Renderer>();
        Material material = renderer.material;

        Color color = material.GetColor("_BaseColor");
        color.a = 0f;
        material.SetColor("_BaseColor", color);
        
        StartCoroutine(FadeAlphaColor(material, 0.5f, intFootPrintsFadeDuration));

    }
    



    private IEnumerator FadeAlphaColor(Material material, float targetAlpha, float duration)
    {
        Color color = material.GetColor("_BaseColor");
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            color.a = newAlpha;
            material.SetColor("_BaseColor", color);

            yield return null;
        }

        color.a = targetAlpha;
        material.SetColor("_BaseColor", color);


        

    }

    private IEnumerator ShowTutInfo()
    {

        yield return new WaitForSeconds(2);

        TutorialManager.Instance.PokazTutorial("Sherlock potrafi równe¿ rozpoznawaæ œlady, prztrzymaj Lewy Shift aby wejœæ w tryb skupienia", "slady");
    }



}
