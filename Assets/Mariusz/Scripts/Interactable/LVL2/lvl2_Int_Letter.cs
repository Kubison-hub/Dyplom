using TMPro;
using UnityEngine;

public class lvl2_Int_Letter : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;

    public bool deactivateAfterPerform = true;
    private bool letterAdded = false;
    public lvl2_Int_HatchExit hatchExit;
    public lvl2_Int_StairsExit stairsExit;

    [SerializeField] AudioSource audioFX;

    [SerializeField] private Renderer rend;



    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        //Debug.Log(interactable.name + ", interaction Performed");

        interactable.AddClue(0);
        AddLetter();

        audioFX.pitch = Random.Range(0.8f, 1.2f);
        audioFX.Play();

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ////intCollider.enabled = false;
        if (deactivateAfterPerform)
        {
            rend.enabled = false;
            //this.gameObject.SetActive(false);
        }
        
    }

    public void AddLetter()
    {
        if (!letterAdded)
        {
            letterAdded = true;
            hatchExit.AddLetter();
            stairsExit.AddLetter();
            Debug.Log("Letter Added");
        }
       
    }
}
