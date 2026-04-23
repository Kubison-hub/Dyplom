using UnityEngine;

public class Q2_FirstInteraction : MonoBehaviour
{
    private GameObject watsonGO;
    private Animator watsonAnimator;
    private void Start()
    {
        Int2_WatsonDialogSherlock.Instance.interactable.isInteractableActive = true;
        Int2_WatsonDialogViolet.Instance.interactable.isInteractableActive = true;
        //Int2_WatsonDialogSelma.Instance.interactable.isInteractableActive = true;

        watsonGO = GameObject.Find("Watson");
        watsonAnimator = watsonGO.GetComponent<Animator>();
        
        watsonAnimator.SetBool("AnimEdithExam", false);
    }




}
