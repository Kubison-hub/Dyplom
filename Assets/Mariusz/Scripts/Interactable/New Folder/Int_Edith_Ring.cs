using UnityEngine;

public class Int_Edith_Ring : MonoBehaviour
{
    
    private Interactable interactable;
    public bool performed = false;

    [SerializeField] private Int_EdithExamBody edith;
    [SerializeField, Range(0, 2)] private int edithClueIndex = 0;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (edith == null)
            edith = GetComponentInParent<Int_EdithExamBody>();

        if (edith == null)
            edith = FindFirstObjectByType<Int_EdithExamBody>();

        if (edith == null)
            Debug.LogWarning("Int_Edith_Ring: Int_EdithExamBody was not found.", this);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed || edith == null || !edith.IsExaminationActive)
            return;

        performed = true;
        interactable?.MarkCompleted();
        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        interactable.AddClue(0);
        edith?.RegisterExamClue(edithClueIndex, player);

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    

}
