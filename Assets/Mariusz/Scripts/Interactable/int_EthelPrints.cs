
using System.Collections;

using UnityEngine;



public class int_EthelPrints : MonoBehaviour
{
    public EagleVisionScanner scanner;

    private Interactable interactable;

    public bool performed = false;
    public Transform cardPosition;

    public GameObject[] nextInteractions;

    public Material footstepsMaterial;

    private Collider intCollider;
    private void Awake()
    {


        interactable = GetComponent<Interactable>();



        footstepsMaterial.SetFloat("_AlphaStrength", 0.14f);
        footstepsMaterial.SetFloat("_FlickerSpeed", 0.6f);


        intCollider = GetComponent<Collider>();

        Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
        Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

        ActiveNextInteractions(false);
    }



    public void PerformInteraction(PlayerController player)
    {
        interactable.AddClue(0, cardPosition);
        performed = true;
        Debug.Log("Interaction Performed");

        ActiveNextInteractions(true);

        ChangeFootprint();

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        intCollider.enabled = false;
    }


    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }
    }
    public void ChangeFootprint()
    {
        //footstepsMaterial.SetFloat("_AlphaStrength", 0.022f);
        //footstepsMaterial.SetFloat("_FlickerSpeed", 0f);

        Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
        Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));

        StopAllCoroutines();
        StartCoroutine(LerpValues(0.022f, 0f));
    }

    private IEnumerator LerpValues(float endAlpha, float endFlicker)
    {
        float time = 0;
        // Pobieramy aktualne wartoœci jako punkt startowy
        float startAlpha = footstepsMaterial.GetFloat("_AlphaStrength");
        float startFlicker = footstepsMaterial.GetFloat("_FlickerSpeed");

        while (time < 10)
        {
            time += Time.deltaTime;
            float t = time / 10;

            
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
            float currentFlicker = Mathf.Lerp(startFlicker, endFlicker, t);

            footstepsMaterial.SetFloat("_AlphaStrength", currentAlpha);
            //footstepsMaterial.SetFloat("_FlickerSpeed", currentFlicker);

            Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
            //Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));

            yield return null; // Czekaj na nastêpn¹ klatkê
        }

        // Upewniamy siê, ¿e na koñcu s¹ dok³adnie takie wartoœci, jakie chcieliœmy
        footstepsMaterial.SetFloat("_AlphaStrength", endAlpha);
        footstepsMaterial.SetFloat("_FlickerSpeed", endFlicker);

        Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
        Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));
        Debug.Log("Done");
    }

}
