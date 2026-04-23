using DialogueEditor;
using System.Collections;
using System.Net.Mime;
using Unity.Cinemachine;
using UnityEngine;

public class Int2_WatsonDialogSherlock : MonoBehaviour
{
    public static Int2_WatsonDialogSherlock Instance;

    public GameObject WatsonGO;
    public GameObject SherlockGO;
    public float watsonRotationSpeed = 5f;

    public bool performed = false;
    public bool dialogPerformed = false;

    public Interactable interactable;

    [HideInInspector] public PartyInteraction partyInteraction;
    private NPCConversation conversation;

    public CinemachineCamera dialogCam;
    private CinemachineSplineDolly splineDolly;

    public GameObject content;
    private Collider GOCollider;
    private void Awake()
    {
        content = GameObject.Find("Int2_WatsonDialogSherlockContent");

       

    }
    void Start()
    {
        WatsonGO = GameObject.Find("Watson");
        SherlockGO = GameObject.Find("Sherlock");
        transform.parent = WatsonGO.transform;
        transform.position = WatsonGO.transform.position;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        


        WatsonGO = GameObject.Find("Watson");
        SherlockGO = GameObject.Find("Sherlock");

        interactable = GetComponent<Interactable>();

        dialogCam = GetComponentInChildren<CinemachineCamera>();
        splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        


    }

    private void Update()
    {
        if (!interactable.isInteractableActive) return;

        if (!performed)
            RotateWatsonTowardSherlock();
    }

    public void SwitchAndShowTutInfo()
    {
        StartCoroutine(SwitchAndShowTutInfoCor());
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        interactable.questionVFX.Stop();
        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        StartConversation();

    }


    private void StartConversation()
    {
        ChangeCamera();

        PartyInteraction partyInteraction = SherlockGO.GetComponent<PartyInteraction>();
        conversation = partyInteraction.rozmowaMiedzyNami;

        ConversationManager.Instance.StartConversation(conversation);

        
    }

    private void RotateWatsonTowardSherlock()
    {
        Vector3 direction = SherlockGO.transform.position - WatsonGO.transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            WatsonGO.transform.rotation = Quaternion.Lerp(
                WatsonGO.transform.rotation,
                lookRotation,
                Time.deltaTime * watsonRotationSpeed
            );
        }
    }
    private IEnumerator SwitchAndShowTutInfoCor()
    {

        yield return new WaitForSeconds(.5f);

        SwitchCharacter.Instance.canSwitch = true;
        SwitchCharacter.Instance.SetActivePlayer(1);

        yield return new WaitForSeconds(2);

        TutorialManager.Instance.PokazTutorial("U¿yj Watsona aby rozmawiaæ oraz odci¹gaæ uwagê od Sherlocka. " +
            "Wciœniæ Spacjê aby siê prze³¹czyæ miêdzy postaciami", "Watson");

        this.gameObject.SetActive(false);
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            splineDolly.CameraPosition = .6f;
            dialogCam.Priority = 50;
        }
    }

   

}
