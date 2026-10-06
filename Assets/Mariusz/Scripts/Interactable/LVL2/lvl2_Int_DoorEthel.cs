using System.Collections;
using UnityEngine;

public class lvl2_Int_DoorEthel : MonoBehaviour
{
    private Interactable interactable;
    public GameObject door;
    public GameObject ethelRoom;
    public bool performed = false;

    private Coroutine openCoroutine;
    private Coroutine fadeCoroutine;
    private bool isOpen = false;

    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    private Quaternion openRotation;
    [SerializeField] private float openSpeed = 120f;

    [SerializeField] private GameObject blackBoard;
    [SerializeField] private float fadeDuration = 1f;

    [SerializeField] private AudioSource audiosource;

    [SerializeField] private Material newMaterial;
    [SerializeField] private Collider intCollider;

    [Header("Save Restoration")]
    [Tooltip("Restores opened doors without resetting their room on Start. Enable only for doors that need this save behavior.")]
    [SerializeField] private bool restoreOpenedStateOnStart = false;

    public bool UsesSaveStateRestoration => restoreOpenedStateOnStart;
    public bool IsOpen => isOpen;
    public GameObject Blackboard => blackBoard;



    private void Start()
    {
        openRotation = Quaternion.Euler(openEuler);
        interactable = GetComponent<Interactable>();

        transform.parent = door.transform;

        if (restoreOpenedStateOnStart && isOpen)
        {
            RestoreOpenedStateFromSave();
            return;
        }

        ethelRoom.SetActive(false);
        blackBoard.SetActive(true);
    }

    public void RestoreOpenedStateFromSave()
    {
        if (!restoreOpenedStateOnStart)
            return;

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (!isOpen)
            return;

        // Apply scene state even when the saved flags were restored earlier.
        performed = true;
        openRotation = Quaternion.Euler(openEuler);
        interactable = GetComponent<Interactable>();
        if (door != null)
        {
            transform.SetParent(door.transform, true);
            door.transform.localRotation = openRotation;
        }
        if (ethelRoom != null)
            ethelRoom.SetActive(true);
        if (blackBoard != null)
            blackBoard.SetActive(false);
        if (interactable != null)
            interactable.isInteractableActive = false;
        if (intCollider != null)
            intCollider.enabled = false;
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        //Debug.Log(interactable.name + ", interaction Performed");

        if (!isOpen && openCoroutine == null)
        {
            openCoroutine = StartCoroutine(OpenDoor());
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        Renderer renderer = blackBoard.GetComponent<Renderer>();
        renderer.material = newMaterial;
    


    fadeCoroutine = StartCoroutine(FadeAlpha(renderer.material, 0f, fadeDuration));

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        if (intCollider != null)
            intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private IEnumerator OpenDoor()
    {
        isOpen = true;
        ethelRoom.SetActive(true);
        audiosource.Play();

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

    private IEnumerator FadeAlpha(Material material, float targetAlpha, float duration)
    {
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        if (!material.HasProperty(colorProperty))
        {
            Debug.LogError("Materiał nie ma _Color ani _BaseColor");
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            material.SetColor(colorProperty, color);

            yield return null;
        }

        color.a = targetAlpha;
        material.SetColor(colorProperty, color);
        blackBoard.SetActive(false);

        fadeCoroutine = null;
    }

}
