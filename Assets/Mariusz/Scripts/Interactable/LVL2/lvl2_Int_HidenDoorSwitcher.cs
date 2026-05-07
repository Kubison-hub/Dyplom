using System.Collections;
using UnityEngine;

public class lvl2_Int_HidenDoorSwitcher : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;

    private bool isOpen;

    public bool canOpen = false;

    public lvl2_Int_EthelWallButton wallButton;
    public GameObject button;

    public string text = "potrzebny specjalny kluczyk, gdzie on mo¿e byæ?";

    [SerializeField] private AudioSource audioFx;

    public Animator animator;

    [SerializeField] private Renderer intRenderer;
    private void Start()
    {
        isOpen = false;
        interactable = GetComponent<Interactable>();

        
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        //Debug.Log(interactable.name + ", interaction Performed");

        if (canOpen)
        {
            SwitchDoor();
            interactable.AddClue(0);
            interactable.isInteractableActive = false;
            player.currentInteractable = null;

        }
        else
        {
            StartCoroutine(AddText());

            
        }

            player.currentInteractable = null;

    }

    private void SwitchDoor()
    {
        interactable.interactiveShader = null;
        isOpen = true;
        wallButton.canOpenDoor = true;
        audioFx.Play();
        animator.SetTrigger("Use");
        Vector3 targetPosition = wallButton.button.transform.localPosition;
        targetPosition.x = -6.9f;

        button.transform.localPosition = targetPosition;

    }

    private IEnumerator AddText()
    {
        
        ClueManager.Instance.SherlockText.text = text;
        yield return new WaitForSeconds(2);
        ClueManager.Instance.SherlockText.text = "";
        interactable.isInteractableActive = true;

    }
}
