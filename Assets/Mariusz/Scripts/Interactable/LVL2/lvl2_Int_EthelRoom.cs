using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lvl2_Int_EthelRoom : Lvl3InteractionDialogueBase
{
    private static readonly Lvl3DialogueLine[] DefaultLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Nie znalazłem Ethel w jej pokoju.",
            duration = 2.5f
        }
    };

    private Interactable interactable;
    private Transform cardPosition;

    private EagleVisionScanner scanner;

    public bool performed = false;

    public GameObject spline1;
    public GameObject spline2;

    public lvl2_Int_Mirror mirror;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => DefaultLines;

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

        PlayInteractionDialogue(null);

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

        while (IsDialoguePlaying)
            yield return null;

        this.gameObject.SetActive(false);

    }

}
