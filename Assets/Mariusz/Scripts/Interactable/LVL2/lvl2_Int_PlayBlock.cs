using UnityEngine;

public class lvl2_Int_PlayBlock : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    public bool EthelRoomBlocks;

    [Header("Top Text")]
    [SerializeField, TextArea] private string playBlocksText = "Te klocki muszą do czegoś służyć.";
    [SerializeField, TextArea] private string ethelRoomBlocksText = "Klocki Ethel tworzą znajomy wzór.";

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
            ShowTopText(ethelRoomBlocksText);
            Debug.Log("Ethel Room PlayBlocks interacted");
        }
        else
        {
            interactable.AddClue(1);
            ShowTopText(playBlocksText);
            Debug.Log("PlayBlock interacted");
        }
        

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private void ShowTopText(string text)
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(text, string.Empty);
    }
//FIX

}
