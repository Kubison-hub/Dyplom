using System.Collections;
using UnityEngine;


public class lvl2_Int_EthelWallButton : MonoBehaviour
{

    private Interactable interactable;
    public GameObject hiddenRoom;
    public bool performed = false;
    [SerializeField] private Renderer intRenderer;
    public string text = "Nie da siê wcisn¹æ...";
    public GameObject door;
    public GameObject button;
    [SerializeField] private GameObject blackBoard;

    private bool isOpen = false;

    public float targetLocalX;

    private Coroutine openCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine pushCoroutine;

    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    private Quaternion openRotation;
    private Quaternion closedRotation;

    [SerializeField] private float fadeDuration = 1f;

    [SerializeField] private float openDuration = 1f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private float pushDuration = 0.3f;
    [SerializeField] private AnimationCurve pushCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private AudioSource audioFX;
    [SerializeField] private Material newMaterial;

    public lvl2_Int_StairsExit stairsExit;

    private Coroutine addClueCoroutine;
    private bool firstTry = true;
    public bool canOpenDoor;
    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (door == null)
        {
            Debug.LogError("Door is null");
            return;
        }

        closedRotation = door.transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(openEuler);

        hiddenRoom.gameObject.SetActive(false);
        blackBoard.gameObject.SetActive(true);
    }

    public void PerformInteraction(PlayerController player)
    {
        Debug.Log(interactable.name + ", interaction Performed");

        if (canOpenDoor)
        {
            OpenHidenDoor();
            hiddenRoom.gameObject.SetActive(true);
            
            interactable.AddClue(1);
            interactable.isInteractableActive = false;
        }
        else
        {
            if (firstTry)
            {
                firstTry = false;
                addClueCoroutine = StartCoroutine(AddClue(player));
            }
            else
            {
                StartCoroutine(AddText(text));
            }  
        }

        player.currentInteractable = null;
    }

    private IEnumerator AddClue(PlayerController player)
    {
        interactable.isInteractableActive = false;
        player.currentInteractable = null;
        yield return null; 
        
        interactable.AddClue(0);
       
        yield return new WaitForSeconds(5);
        interactable.isInteractableActive = true;

        Debug.Log("AddClue2");
    }

    private void OpenHidenDoor()
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");


        if (isOpen)
            return;

        audioFX.Play();
        stairsExit.hiddenDoorDiscovered = true;
        Renderer renderer = blackBoard.GetComponent<Renderer>();
        renderer.material = newMaterial;

        if (renderer == null)
        {
            Debug.LogError("blackBoard has no Renderer");
            return;
        }

        isOpen = true;
        

        if (openCoroutine == null)
            openCoroutine = StartCoroutine(OpenDoor());

        if (fadeCoroutine == null)
            fadeCoroutine = StartCoroutine(FadeAlpha(renderer.material, 0f, fadeDuration));

        if (pushCoroutine == null)
            pushCoroutine = StartCoroutine(PushCoroutine());
    }

    private IEnumerator OpenDoor()
    {
        Quaternion startRotation = door.transform.localRotation;
        float time = 0f;

        while (time < openDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / openDuration);
            float curvedT = openCurve.Evaluate(t);

            door.transform.localRotation = Quaternion.Slerp(
                startRotation,
                openRotation,
                curvedT
            );

            yield return null;
        }

        door.transform.localRotation = openRotation;
        interactable.isInteractableActive = true;
        openCoroutine = null;
    }

    private IEnumerator PushCoroutine()
    {
        Vector3 startPosition = button.transform.localPosition;
        Vector3 targetPosition = startPosition;
        targetPosition.x = targetLocalX;

        float time = 0f;

        while (time < pushDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / pushDuration);
            float curvedT = pushCurve.Evaluate(t);

            button.transform.localPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                curvedT
            );

            yield return null;
        }

        button.transform.localPosition = targetPosition;
        pushCoroutine = null;
    }

    private IEnumerator FadeAlpha(Material material, float targetAlpha, float duration)
    {
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        if (!material.HasProperty(colorProperty))
        {
            Debug.LogError("Materia³ nie ma _Color ani _BaseColor");
            fadeCoroutine = null;
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float curvedT = fadeCurve.Evaluate(t);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, curvedT);
            material.SetColor(colorProperty, color);

            yield return null;
        }

        color.a = targetAlpha;
        material.SetColor(colorProperty, color);

        blackBoard.SetActive(false);
        fadeCoroutine = null;
    }

    private IEnumerator AddText(string stringText)
    {

        ClueManager.Instance.SherlockText.text = stringText;
        yield return new WaitForSeconds(3);
        ClueManager.Instance.SherlockText.text = "";
        interactable.isInteractableActive = true;

    }
}