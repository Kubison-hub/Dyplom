using UnityEngine;
using System.Collections;
using UnityEngine.VFX;


public class Interactable : MonoBehaviour
{


    public bool isInteractableActive = true;
    [Space]

    [SerializeField] private InteractionType interactionType;
    public Transform interactabePoint;
    [Space]

    public string objectDescription = "Wpisz nazwê obiektu";
    public GameObject interactiveShader;

    [Space]
    public Clues_SO[] clues;
    [Space]


    public VisualEffect questionVFX;
    public VisualEffect interactionVFX;
    public bool fxVisibleOnStart = true;

    public bool isFootPrintInteraction = false;
    public bool isNearPlayer;

    private void Start()
    {


        VisualEffect[] allVFX = GetComponentsInChildren<VisualEffect>(true);

        Transform qTransform = transform.Find("QuestionFX");
        if (qTransform != null) questionVFX = qTransform.GetComponent<VisualEffect>();

        Transform iTransform = transform.Find("interactionFx");
        if (iTransform != null) interactionVFX = iTransform.GetComponent<VisualEffect>();


        if (questionVFX != null)
        {
            if (!fxVisibleOnStart)
                questionVFX.Stop();
        }
        else
        {
            //Debug.LogError("QuestionFX is null");
        }

        if (interactionVFX != null)
        {
            interactionVFX.Stop();
        }
        else
        {
            //Debug.LogError("interactionVFX is null");
        }

        if (interactiveShader != null)
            interactiveShader.SetActive(false);

        //if (interactabePoint == null)
        //    interactabePoint = transform;




    }

    // --- KLIKNIÊCIE ---
    public void TryToInteract(PlayerController player)
    {
        //if (!isInteractableActive || ConversationManager.Instance.inConversation 
        //    || TutorialManager.Instance.isTutorialActive) return;

        if (!isInteractableActive) return;

        player.currentInteractable = this;

        if (interactabePoint != null)
        {
            player.currentInteractionPoint = interactabePoint;

        }
        else
        {
            // player.currentInteractionPoint = player.transform;

        }

        player.MoveToInteractable();


        // Drzwi
        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();
            if (door != null) door.Interact(player); // Drzwi same ustawiaj¹ ruch gracza
            else Debug.LogError("DoorTrigger component not found");
        }



        // Pianino
        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();
            if (piano != null) piano.Interact(player);
            else Debug.LogError("Piano component not found");
        }

        // Prze³¹cznik drzwi
        if (interactionType == InteractionType.DoorSwitcher)
        {
            DoorSwitcher doorSwitcher = GetComponent<DoorSwitcher>();
            if (doorSwitcher != null) doorSwitcher.Interact(player);
            else Debug.LogError("DoorSwitcher component not found");
        }

        // --- PICKUP (Zmienione na wzór DoorTrigger) ---
        if (interactionType == InteractionType.Pickup)
        {
            PickupItem pickup = GetComponent<PickupItem>();

            // Teraz to PickupItem ustawia ruch gracza (tak jak Drzwi)
            if (pickup != null) pickup.Interact(player);
            else Debug.LogError("PickupItem component not found");
        }



        //--- TABLE

        if (interactionType == InteractionType.Table)
        {
            Table table = GetComponent<Table>();
            if (table != null) table.Interact(player);
            else Debug.LogError("Table component not found");
        }





        //SecretWall
        if (interactionType == InteractionType.SecretWall)
        {
            PlayerTopText.Instance.ShowTopText("Ta œciana odstaje od reszy", "Ewidentnie");
        }
    }

    // --- DOTARCIE DO CELU ---
    public void PerformInteraction(PlayerController player)
    {
        if (!isInteractableActive) return;

        // Default Interaction
        if (interactionType == InteractionType.Interaction)
        {

            Interaction interaction = GetComponent<Interaction>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Interaction is null");
            }
        }

        if (interactionType == InteractionType.VioletDialog)
        {

            Int_VioletDialog interaction = GetComponent<Int_VioletDialog>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("VioletDialog is null");
            }
        }

        if (interactionType == InteractionType.SelmaDialog)
        {

            Int_SelmaDialog interaction = GetComponent<Int_SelmaDialog>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("SelmaDialog  is null");
            }
        }

        if (interactionType == InteractionType.HindenWall)
        {

            Int_HidenWall interaction = GetComponent<Int_HidenWall>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("HidenWall is null");
            }
        }

        if (interactionType == InteractionType.PrintPath)
        {

            Int_PrintPath interaction = GetComponent<Int_PrintPath>();
            if (interaction != null)
            {

                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_PrintPath is null");
            }
        }

        if (interactionType == InteractionType.EmptyWall)
        {

            EmptyWall interaction = GetComponent<EmptyWall>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("EmptyWall is null");
            }
        }

        if (interactionType == InteractionType.Footsteps)
        {

            int_Footsteps interaction = GetComponent<int_Footsteps>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("int_Footsteps is null");
            }
        }

        if (interactionType == InteractionType.EthelPrints)
        {

            int_EthelPrints interaction = GetComponent<int_EthelPrints>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("EthelPrints is null");
            }
        }


        if (interactionType == InteractionType.ClueInteraction)
        {

            Int_EdithExamBody clueInteraction = GetComponent<Int_EdithExamBody>();
            if (clueInteraction != null)
            {
                clueInteraction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("ClueInteraction is null");
            }
        }

        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();
            if (door != null) door.PerformInteraction();
        }

        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();
            if (piano != null) piano.PerformInteraction(player);
        }

        if (interactionType == InteractionType.DoorSwitcher)
        {
            DoorSwitcher doorSwitcher = GetComponent<DoorSwitcher>();
            if (doorSwitcher != null) doorSwitcher.PerformInteraction();
        }

        // --- PICKUP ---
        if (interactionType == InteractionType.Pickup)
        {
            PickupItem item = GetComponent<PickupItem>();
            if (item != null) item.PerformInteraction();
        }

        if (interactionType == InteractionType.Table)
        {
            Table table = GetComponent<Table>();
            if (table != null) table.PerformInteraction();

        }

        if (interactionType == InteractionType.Globus)
        {
            Int_Globus interaction = GetComponent<Int_Globus>();
            if (interaction != null)
            {
                if (SwitchCharacter.Instance.activePlayerIndex == 0)
                    interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Interaction is null");
        }

        if (interactionType == InteractionType.Stairs)
        {
            Int_StairsUp interaction = GetComponent<Int_StairsUp>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Interaction is null");

        }

        // -------------------- QUEST 2 -------------------

        if (interactionType == InteractionType.WatsonDialogSherlock)
        {
            Int2_WatsonDialogSherlock interaction = GetComponent<Int2_WatsonDialogSherlock>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Interaction is null");
        }

        if (interactionType == InteractionType.WatsonDialogViolet)
        {
            Int2_WatsonDialogViolet interaction = GetComponent<Int2_WatsonDialogViolet>();
            if (interaction != null)
            {
                if (SwitchCharacter.Instance.activePlayerIndex == 1)
                    interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Interaction is null");
        }

        if (interactionType == InteractionType.WatsonDialogViolet_2)
        {
            Int2_WatsonDialogViolet_2 interaction = GetComponent<Int2_WatsonDialogViolet_2>();
            if (interaction != null)
            {
                if (SwitchCharacter.Instance.activePlayerIndex == 1)
                    interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Interaction is null");
        }

        if (interactionType == InteractionType.WatsonScan)
        {
            Int2_WatsonScan interaction = GetComponent<Int2_WatsonScan>();
            if (interaction != null)
            {
                if (SwitchCharacter.Instance.activePlayerIndex == 1)
                    interaction.PerformInteraction(player);
            }

            else
                Debug.LogError("WatsonScan is null");
        }

        //--------------------   LVL  2   ---------------------------------------------------------------------------


        if (interactionType == InteractionType.Book)
        {
            lvl2_Int_Book interaction = GetComponent<lvl2_Int_Book>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);

            }


            else
                Debug.LogError("lvl2_Int_Book is null");
        }

        if (interactionType == InteractionType.lvl2_Int_DoorEthel)
        {
            lvl2_Int_DoorEthel interaction = GetComponent<lvl2_Int_DoorEthel>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_DoorEthel is null");
        }

        if (interactionType == InteractionType.lvl2_Int_SelmaDoor)
        {
            lvl2_Int_SelmaDoor interaction = GetComponent<lvl2_Int_SelmaDoor>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_SelmaDoor is null");
        }

        if (interactionType == InteractionType.lvl2_Int_EthelWallButton)
        {
            lvl2_Int_EthelWallButton interaction = GetComponent<lvl2_Int_EthelWallButton>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_EthelWallButton is null");
        }

        if (interactionType == InteractionType.lvl2_Int_Mirror)
        {
            lvl2_Int_Mirror interaction = GetComponent<lvl2_Int_Mirror>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_Mirror is null");
        }

        if (interactionType == InteractionType.lvl2_Int_HidenDorrSwitcher)
        {
            lvl2_Int_HidenDoorSwitcher interaction = GetComponent<lvl2_Int_HidenDoorSwitcher>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_HidenDorrSwitcher is null");
        }

        if (interactionType == InteractionType.lvl2_Int_Letter)
        {
            lvl2_Int_Letter interaction = GetComponent<lvl2_Int_Letter>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_Letter is null");
        }
        if (interactionType == InteractionType.lvl2_Int_PlayBlock)
        {
            lvl2_Int_PlayBlock interaction = GetComponent<lvl2_Int_PlayBlock>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_PlayBlock is null");
        }
        if (interactionType == InteractionType.lvl2_Int_Gramophone)
        {
            lvl2_Int_Gramophone interaction = GetComponent<lvl2_Int_Gramophone>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_Gramophone is null");
        }
        if (interactionType == InteractionType.lvl2_Int_Hatch)
        {
            lvl2_Int_Hatch interaction = GetComponent<lvl2_Int_Hatch>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_Hatch is null");
        }
        if (interactionType == InteractionType.lvl2_Int_HatchExit)
        {
            lvl2_Int_HatchExit interaction = GetComponent<lvl2_Int_HatchExit>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_HatchExit is null");
        }
        if (interactionType == InteractionType.lvl2_Int_StairsExit)
        {
            lvl2_Int_StairsExit interaction = GetComponent<lvl2_Int_StairsExit>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl2_Int_StairsExit is null");
        }

    }





    //-----------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------


    public void AddClue(int listNumber, Transform cardPosition = null)
    {
        if (clues == null) return;
        if (cardPosition == null)
            cardPosition = this.transform;

        ClueManager.Instance.AddClue(clues[listNumber], cardPosition.position);

        if (questionVFX != null)
        {
            questionVFX.Stop();
            //Destroy(ClueVisualFx, 5f);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scanner"))
        {
            if (questionVFX != null && isInteractableActive)
            {
                if (!isFootPrintInteraction)
                {

                    questionVFX.Play();


                }
                else
                {
                    if (isNearPlayer)
                    {
                        questionVFX.Play();
                    }

                }


            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Scanner"))
        {

            if (questionVFX != null) questionVFX.Stop();
        }
    }

    private IEnumerator PushVisualEffect(Collider other)
    {


        questionVFX.Play();
        yield return new WaitForSeconds(60);

    }
}



public enum InteractionType
{
    None,
    Door,
    Dialog,
    Piano,
    DoorSwitcher,
    Pickup,
    Table,
    Globus,
    Stairs,
    SecretWall,
    Interaction,
    ClueInteraction,
    EmptyWall,
    Footsteps,
    EthelPrints,
    PrintPath,
    HindenWall,
    SelmaDialog,
    VioletDialog,

    // Q2

    WatsonDialogSherlock,
    WatsonDialogViolet,
    WatsonDialogViolet_2,
    WatsonScan,


    // LVL2

    Book,
    lvl2_Int_DoorEthel,
    lvl2_Int_EthelWallButton,
    lvl2_Int_Mirror,
    lvl2_Int_SelmaDoor,
    lvl2_Int_HidenDorrSwitcher,
    lvl2_Int_Letter,
    lvl2_Int_PlayBlock,
    lvl2_Int_Gramophone,
    lvl2_Int_Hatch,
    lvl2_Int_HatchExit,
    lvl2_Int_StairsExit

}