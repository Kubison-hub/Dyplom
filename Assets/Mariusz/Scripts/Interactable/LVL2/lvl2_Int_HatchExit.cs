using NUnit.Framework;
using System.Collections;
using UnityEngine;

public class lvl2_Int_HatchExit : MonoBehaviour
{
    private Interactable interactable;

    public string text = "Najpierw powinienem dok³adniej przeszukaæ piêtro";
    

    public bool performed = false;

    [SerializeField] private int requiredLetters = 3;
    public static int lettersCollected = 0;

    public void AddLetter()
    {
        lettersCollected++;
        
    }

    public bool HasAllLetters()
    {
        return lettersCollected >= requiredLetters;
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");


        TryExit();

        
        player.currentInteractable = null;

        
        
    }

    public void TryExit()
    {
        if (!HasAllLetters())
        {
            StartCoroutine(AddText());
            Debug.Log("Brakuje listów");
            
            return;
        }
        Debug.Log("Wszystkie listy zebrane. OPUSZCZAM LEVEL.");
        interactable.isInteractableActive = false;
        
    }

    private IEnumerator AddText()
    {

        ClueManager.Instance.SherlockText.text = text;
        yield return new WaitForSeconds(3);
        ClueManager.Instance.SherlockText.text = "";
        interactable.isInteractableActive = true;

    }

    

}
