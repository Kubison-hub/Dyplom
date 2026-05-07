using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lvl2_Int_EthelRoom : MonoBehaviour
{
    private Interactable interactable;
    private Transform cardPosition;

    private EagleVisionScanner scanner;

    public bool performed = false;

    public GameObject spline1;
    public GameObject spline2;

   public lvl2_Int_Mirror mirror;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        cardPosition = transform; 
    }


    public void AddSplines()
    {
        StartCoroutine(AddClue());

       
        //EagleVisionScanner.Instance.footprintsSplines.Add(spline);

        if (!mirror.isOpen)
        {
            spline1.gameObject.SetActive(true);
        }
        
        spline2.gameObject.SetActive(true);




        Debug.Log("AddSpines");
        interactable.isInteractableActive = false;
        
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerA"))
        {
            AddSplines();

        }
    }

    private IEnumerator AddClue()
    {
        yield return new WaitForSeconds(1);
        interactable.AddClue(0);
        interactable.isInteractableActive = false;
        yield return new WaitForSeconds(3);
        

        this.gameObject.SetActive(false);

    }

}
