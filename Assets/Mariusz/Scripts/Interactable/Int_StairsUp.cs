using UnityEngine;

public class Int_StairsUp : MonoBehaviour
{

    public bool canGoUpStairs = false;
    public bool performed = false;

    public GameObject SherlockGO;

    public Transform moveDestination;

    private Interactable interactable;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        SherlockGO = GameObject.Find("Sherlock");
    }
    private void Update()
    {
        if (canGoUpStairs)
        {
            interactable.interactabePoint = moveDestination;
        }
        else
        {
            interactable.interactabePoint = SherlockGO.transform;
        }
    }

    public void PerformInteraction(PlayerController player)
    {

        if (canGoUpStairs)
        {
            performed = true;
            
            interactable.isInteractableActive = false;
            player.currentInteractable = null;

            GoUpStairs();
        }
        else
        {
            PlayerTopText.Instance.ShowTopText("Madam Selma pilnuje schodów na górê, interesuj¹ce", 
                "Na razie nie przejdziemy, rozejrzyjmy siê po domu.");
            player.currentInteractable = null;
        }
  
    }

    private void GoUpStairs()
    {
        Debug.Log("KOOOOOONNIEEEEEEECCC!!!");
    }
}
