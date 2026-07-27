using UnityEngine;

public class Int_Edith_Paper : MonoBehaviour
{
    private Interactable interactable;
    public bool performed = false;

    public GameObject paper;
    public AudioSource paper_Audio;
    [SerializeField] private Int_EdithExamBody edith;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (edith == null)
            edith = GetComponentInParent<Int_EdithExamBody>();

        if (edith == null)
            edith = FindFirstObjectByType<Int_EdithExamBody>();

        if (edith == null)
            Debug.LogWarning("Int_Edith_Paper: Int_EdithExamBody was not found.", this);
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        interactable.AddClue(0);
        edith?.RegisterExamClue();

        if (paper_Audio != null)
        {
            paper_Audio.Play();
        }

        if (paper != null)
        {
            paper.SetActive(false);
        }

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }
}
