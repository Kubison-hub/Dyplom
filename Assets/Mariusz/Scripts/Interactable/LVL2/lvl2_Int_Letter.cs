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

    [Header("Top Text")]
    [SerializeField, TextArea] private string topText = "Ten list może wyjaśnić więcej, niż się wydaje.";



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

        if (audioFX != null)
        {
            audioFX.pitch = Random.Range(0.8f, 1.2f);
            audioFX.Play();
        }

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, string.Empty);

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ////intCollider.enabled = false;
        if (deactivateAfterPerform)
        {
            if (rend != null)
                rend.enabled = false;
            //this.gameObject.SetActive(false);
        }
        
    }

    public void AddLetter()
    {
        if (!letterAdded)
        {
            letterAdded = true;
            if (hatchExit != null)
                hatchExit.AddLetter();

            if (stairsExit != null)
                stairsExit.AddLetter();
            Debug.Log("Letter Added");
        }
       
    }
}
