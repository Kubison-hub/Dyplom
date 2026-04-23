using System.Collections;
using UnityEngine;

public class Int_PrintPath : MonoBehaviour
{

    public bool performed = false;
    public Transform cardPosition;
    //public CinemachineCamera dialogCam;

    public Material footstepsMaterial;

    public GameObject[] nextInteractions;
    private Interactable interactable;
    private Collider intCollider;

    private bool isDeactivating = false;
    public bool playerInTrigger = false;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();

        footstepsMaterial.SetFloat("_AlphaStrength", 0.14f);
        footstepsMaterial.SetFloat("_FlickerSpeed", 0.6f);

        Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
        Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));

        intCollider = GetComponent<Collider>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

       
    }

    private void Update()
    {


        if (EagleVisionSystem.Instance.isActive)
        {
            interactable.isInteractableActive = true;
            intCollider.enabled = true;
        }
        else
        {
            if (!isDeactivating)
            {
                isDeactivating = true;
                StartCoroutine(Deactivating());
            }
        }
    }

    private IEnumerator Deactivating()
    {
        yield return new WaitForSeconds(5);

        interactable.isInteractableActive = false;
        isDeactivating = false;
        intCollider.enabled = false;
    }


    public void PerformInteraction(PlayerController player)
    {
        
        interactable.AddClue(0, cardPosition);
        performed = true;
        Debug.Log("Interaction Performed");


        

        if (nextInteractions  != null)
            ActivateNextInteractions();


        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ChangeFootprint();
        

    }
    private void ActivateNextInteractions()
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(true);
        }
    }
    public void ChangeFootprint()
    {


        Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
        Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));

        
        StartCoroutine(LerpValues(0.022f, 0f));
    }

    private IEnumerator DisableGOCor()
    {

        yield return new WaitForSeconds(5);

        this.gameObject.SetActive(false);
    }

    private IEnumerator LerpValues(float endAlpha, float endFlicker)
    {
        float time = 0;
        
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

            //Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
            //Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));

            yield return null; // Czekaj na nastêpn¹ klatkê
        }


        footstepsMaterial.SetFloat("_AlphaStrength", endAlpha);
        footstepsMaterial.SetFloat("_FlickerSpeed", endFlicker);
        this.gameObject.SetActive(false);
        //Debug.Log(footstepsMaterial.GetFloat("_AlphaStrength"));
        //Debug.Log(footstepsMaterial.GetFloat("_FlickerSpeed"));
        //Debug.Log("Done");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerA"))
        {
            interactable.isNearPlayer = true;

            if (interactable.questionVFX != null)
            {
                if (EagleVisionSystem.Instance.isActive)
                {
                    interactable.questionVFX.Play();

                }

            }

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerA"))
        {
            interactable.isNearPlayer = false;

            if (interactable.questionVFX != null)
            {
                interactable.questionVFX.Stop();
                
            }
        }
    }


    //public void ChangeToDialogCamera()
    //{
    //    if (dialogCam != null)
    //    {
    //        dialogCam.Priority = 50;
    //    }
    //}


    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
