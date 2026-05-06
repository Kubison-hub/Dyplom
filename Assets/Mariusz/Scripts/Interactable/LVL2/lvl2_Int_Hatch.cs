using System.Collections;
using UnityEngine;

public class lvl2_Int_Hatch : MonoBehaviour
{
    private Interactable interactable;
    public GameObject door;
    
    public bool performed = false;

    private Coroutine openCoroutine;
    private bool isOpen = false;

    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    private Quaternion openRotation;
    [SerializeField] private float openSpeed = 120f;

    


    [SerializeField] AudioSource audioFX;
    private void Start()
    {
        openRotation = Quaternion.Euler(openEuler);
        interactable = GetComponent<Interactable>();

        transform.parent = door.transform;

       
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");

        if (!isOpen && openCoroutine == null)
        {
            //interactable.AddClue(0);
            
            interactable.interactiveShader  = null;
            openCoroutine = StartCoroutine(OpenDoor());
            audioFX.Play();
            player.currentInteractable = null;
            interactable.isInteractableActive = false;

        }

    }

    private IEnumerator OpenDoor()
    {
        isOpen = true;
        


        while (Quaternion.Angle(door.transform.localRotation, openRotation) > 0.5f)
        {
            door.transform.localRotation = Quaternion.RotateTowards(
                door.transform.localRotation,
                openRotation,
                openSpeed * Time.deltaTime
            );

            yield return null;
        }

        door.transform.localRotation = openRotation;
    }

    
}
