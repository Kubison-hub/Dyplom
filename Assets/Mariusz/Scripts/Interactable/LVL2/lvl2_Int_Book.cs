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

    [Header("Top Text")]
    [SerializeField, TextArea] private string topText = "Znalazłem mały kluczyk.";

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

        if (switcher != null)
            switcher.canOpen = true;

        interactable.AddClue(0);

        if (audioFX != null)
            audioFX.Play();

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, string.Empty);

        if (spline1 != null)
            spline1.SetActive(false);

        if (spline2 != null)
            spline2.SetActive(false);
        Debug.Log("KEY ADDED");
        keyFounded = true;

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    


}
