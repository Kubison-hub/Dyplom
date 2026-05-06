using System.Collections;
using TMPro;
using UnityEngine;

public class lvl2_Int_Book : MonoBehaviour
{
    private Interactable interactable;
    public GameObject spline1;
    public GameObject spline2;

    public bool performed = false;
    public lvl2_Int_HidenDoorSwitcher switcher;

    [SerializeField] private Renderer intRenderer;
    [SerializeField] private AudioSource audioFX;

    public bool keyFounded = false;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");

        if (intRenderer != null)
            intRenderer.enabled = false;

        switcher.canOpen = true;
        interactable.AddClue(0);

        audioFX.Play();

        spline1.gameObject.SetActive(false);
        spline2.gameObject.SetActive(false);
        Debug.Log("KEY ADDED");
        keyFounded = true;

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    


}
