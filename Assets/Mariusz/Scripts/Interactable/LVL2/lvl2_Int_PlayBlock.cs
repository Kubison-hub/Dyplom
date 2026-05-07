using UnityEngine;

public class lvl2_Int_PlayBlock : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    public bool EthelRoomBlocks;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");
        if (EthelRoomBlocks)
        {
            interactable.AddClue(0);
            Debug.Log("Ethel Room PlayBlocks interacted");
        }
        else
        {
            interactable.AddClue(1);
            Debug.Log("PlayBlock interacted");
        }
        

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }


}
