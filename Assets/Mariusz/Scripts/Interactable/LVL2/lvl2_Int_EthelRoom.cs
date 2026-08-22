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

    [Header("Top Text")]
    [SerializeField, TextArea] private string roomDiscoveryText = "Nie znalazłem Ethel w jej pokoju.";

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        cardPosition = transform; 
    }


    public void AddSplines()
    {
        if (performed)
            return;

        performed = true;
        StartCoroutine(AddClue());

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(roomDiscoveryText, string.Empty);

        //EagleVisionScanner.Instance.footprintsSplines.Add(spline);

        if (mirror == null || !mirror.isOpen)
        {
            if (spline1 != null)
                spline1.SetActive(true);
        }
        
        if (spline2 != null)
            spline2.SetActive(true);




        Debug.Log("AddSpines");
        interactable.isInteractableActive = false;
        
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!performed && other.CompareTag("PlayerA"))
        {
            AddSplines();

        }
    }

    private IEnumerator AddClue()
    {
        yield return new WaitForSeconds(1);
        if (interactable != null)
        {
            interactable.AddClue(0);
            interactable.isInteractableActive = false;
        }
        yield return new WaitForSeconds(3);
        

        this.gameObject.SetActive(false);

    }

}
