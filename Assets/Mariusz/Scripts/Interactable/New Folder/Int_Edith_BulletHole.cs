using UnityEngine;

public class Int_Edith_BulletHole : MonoBehaviour
{
    private Interactable interactable;
    public bool performed = false;


    [SerializeField] private Int_EdithExamBody edith;
    [SerializeField] private DetectiveIdeaPoint ideaPoint;
    [SerializeField, Range(0, 2)] private int edithClueIndex = 2;
    [Header("Notebook")]
    [SerializeField] private bool addDatabaseNoteOnSuccessfulExamination = true;
    [SerializeField, Min(0)] private int successfulExaminationNoteIndex = 0;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (edith == null)
            edith = GetComponentInParent<Int_EdithExamBody>();

        if (edith == null)
            edith = FindFirstObjectByType<Int_EdithExamBody>();

        if (edith == null)
            Debug.LogWarning("Int_Edith_BulletHole: Int_EdithExamBody was not found.", this);

        if (ideaPoint != null)
            ideaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed || edith == null || !edith.IsExaminationActive || edith.IsBulletExamInProgress)
            return;

        if (player != null)
            player.currentInteractable = null;

        edith.RegisterBulletExamClue(player, this);
    }

    public void CompleteSuccessfulExamination()
    {
        if (performed)
            return;

        performed = true;
        interactable?.MarkCompleted();
        interactable?.AddClue(0);
        ideaPoint?.RevealFromExternalSource();

        if (addDatabaseNoteOnSuccessfulExamination)
            interactable?.AddNote(successfulExaminationNoteIndex);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
            interactable.interactiveShader = null;
        }

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }
}
