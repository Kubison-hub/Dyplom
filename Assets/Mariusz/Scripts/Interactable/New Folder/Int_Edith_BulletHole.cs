using UnityEngine;

public class Int_Edith_BulletHole : MonoBehaviour
{
    private Interactable interactable;
    public bool performed = false;


    [SerializeField] private Int_EdithExamBody edith;
    [SerializeField] private DetectiveIdeaPoint ideaPoint;
    [SerializeField, TextArea] private string foundTopText =
        "Pocisk wskaże nam trajektorię lotu";

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
        if (performed)
            return;

        if (player != null)
            player.currentInteractable = null;

        performed = true;

        interactable?.AddClue(0);
        edith?.RegisterExamClue();
        ideaPoint?.RevealFromExternalSource();

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(foundTopText, "");

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
