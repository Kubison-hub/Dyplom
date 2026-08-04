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
    [Header("Interaction Shader")]
    [SerializeField, Range(0f, 1f)] private float interactionShaderVisibility = 1f;
    [SerializeField, Min(0f)] private float interactionShaderPulseSpeed = 1.5f;

    [Space]
    public Clues_SO[] clues;
    [Space]


    public VisualEffect questionVFX;
    public VisualEffect interactionVFX;
    public bool fxVisibleOnStart = true;
    [Header("Eagle Vision Question FX")]
    [SerializeField] private string questionFxRateProperty = "Rate";
    [SerializeField, Min(0f)] private int eagleVisionQuestionRate = 100;
    [SerializeField] private string questionFxLifetimeProperty = "Lifetime";
    [SerializeField, Range(0.1f, 1f)] private float eagleVisionExitLifetimeMultiplier = 0.5f;
    [Tooltip("Allows QuestionFX to run in Eagle Vision even when normal click interaction is disabled.")]
    public bool allowQuestionFXWhenInactive = false;
    private bool missingQuestionFxRateReported;
    private bool missingQuestionFxLifetimeReported;
    private bool hasCachedQuestionFxLifetime;
    private float defaultQuestionFxLifetime;
    private InteractionShaderFader interactionShaderFader;

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
        {
            InteractionShaderFader fader = EnsureInteractionShaderFader();
            fader.Configure(interactionShaderVisibility, interactionShaderPulseSpeed);
            fader.HideImmediately();
        }

        CacheQuestionFxDefaultLifetime();

        SetQuestionFxRate(0f);

        //if (interactabePoint == null)
        //    interactabePoint = transform;




    }

    public void SetInteractionShaderHover(bool isHovered)
    {
        InteractionHoverLabelManager.SetHovered(this, isHovered);

        InteractionShaderFader fader = EnsureInteractionShaderFader();
        if (fader == null)
            return;

        fader.Configure(interactionShaderVisibility, interactionShaderPulseSpeed);

        if (isHovered)
            fader.FadeIn();
        else
            fader.FadeOut();
    }

    private InteractionShaderFader EnsureInteractionShaderFader()
    {
        if (interactiveShader == null)
            return null;

        if (interactionShaderFader == null)
            interactionShaderFader = interactiveShader.GetComponent<InteractionShaderFader>();

        if (interactionShaderFader == null)
            interactionShaderFader = interactiveShader.AddComponent<InteractionShaderFader>();

        return interactionShaderFader;
    }
    public void SetInteractionType(InteractionType type)
    {
        interactionType = type;
    }
    // --- KLIKNIÊCIE ---
    public void TryToInteract(PlayerController player)
    {
        //if (!isInteractableActive || ConversationManager.Instance.inConversation 
        //    || TutorialManager.Instance.isTutorialActive) return;

        if (!isInteractableActive) return;

        // BlockBox consumes the click immediately, so the player never receives a NavMesh destination behind it.
        if (interactionType == InteractionType.Int_lv1_BlockBox)
        {
            Int_lv1_BlockBox blockBox = GetComponent<Int_lv1_BlockBox>();
            if (blockBox != null)
                blockBox.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_BlockBox is null");

            return;
        }

        IVioletRoomInteractionGate violetGate =
            GetComponent(typeof(IVioletRoomInteractionGate)) as IVioletRoomInteractionGate;
        if (violetGate != null && violetGate.RedirectWhenVioletIsInRoom(player))
            return;

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

        Int_WatsonSwitchTutorial watsonSwitchTutorial = GetComponent<Int_WatsonSwitchTutorial>();
        if (watsonSwitchTutorial != null)
        {
            watsonSwitchTutorial.PerformInteraction(player);
            return;
        }

        IVioletRoomInteractionGate violetGate =
            GetComponent(typeof(IVioletRoomInteractionGate)) as IVioletRoomInteractionGate;
        if (violetGate != null && violetGate.RedirectWhenVioletIsInRoom(player))
            return;

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

        if (interactionType == InteractionType.int_lv3_easyTable)
        {
            int_lv3_easyTable interaction = GetComponent<int_lv3_easyTable>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("int_lv3_easyTable is null");
            }
        }
        if (interactionType == InteractionType.Int_lv1_BlockBox)
        {
            Int_lv1_BlockBox interaction = GetComponent<Int_lv1_BlockBox>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_BlockBox is null");
        }
        if (interactionType == InteractionType.int_lv1_HenryNPC)
        {
            int_lv1_HenryNPC interaction = GetComponent<int_lv1_HenryNPC>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("int_lv1_HenryNPC is null");
        }

        if (interactionType == InteractionType.int_lv1_ArthurNPC)
        {
            int_lv1_ArthurNPC interaction = GetComponent<int_lv1_ArthurNPC>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("int_lv1_ArthurNPC is null");
        }

        if (interactionType == InteractionType.int_vl1_GeorgeNPC)
        {
            int_vl1_GeorgeNPC interaction = GetComponent<int_vl1_GeorgeNPC>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("int_vl1_GeorgeNPC is null");
        }
        if (interactionType == InteractionType.Int_lv1_BigGramm)
        {
            Int_lv1_BigGramm interaction = GetComponent<Int_lv1_BigGramm>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_lv1_BigGramm is null");
            }
        }

        if (interactionType == InteractionType.Int_lv1_BigGramm_button)
        {
            Int_lv1_BigGramm_button interaction = GetComponent<Int_lv1_BigGramm_button>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_lv1_BigGramm_button is null");
            }
        }
        if (interactionType == InteractionType.Int_lv1_EthelPassageDoor)
        {
            Int_lv1_EthelPassageDoor interaction = GetComponent<Int_lv1_EthelPassageDoor>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_lv1_EthelPassageDoor is null");
            }
        }
        if (interactionType == InteractionType.Int_lv1_SecretDoor)
        {
            Int_lv1_SecretDoor interaction = GetComponent<Int_lv1_SecretDoor>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_lv1_SecretDoor is null");
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

        if (interactionType == InteractionType.GlobusButton)
        {
            int_Globus_Button interaction = GetComponent<int_Globus_Button>();
            if (interaction != null)
            {
                if (SwitchCharacter.Instance.activePlayerIndex == 0)
                    interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Globus button interaction is null");
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

        //--------------------   LVL  1  NEW ---------------------------------------------------------------------------

        if (interactionType == InteractionType.Int_Edith_Paper)
        {
            Int_Edith_Paper interaction = GetComponent<Int_Edith_Paper>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("interaction is null");  
        }
        if (interactionType == InteractionType.Int_Edith_BulletHole)
        {
            Int_Edith_BulletHole interaction = GetComponent<Int_Edith_BulletHole>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("interaction is null");
        }
        if (interactionType == InteractionType.Int_Edith_Ring)
        {
            Int_Edith_Ring interaction = GetComponent<Int_Edith_Ring>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("interaction is null");
        }

        if (interactionType == InteractionType.int_LibraryPainting)
        {
            int_LibraryPainting interaction = GetComponent<int_LibraryPainting>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("int_LibraryPainting is null");
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
        if (interactionType == InteractionType.Int_lv1_SecretLetter)
        {
            Int_lv1_SecretLetter interaction = GetComponent<Int_lv1_SecretLetter>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_lv1_SecretLetter is null");
            }
        }
        if (interactionType == InteractionType.Int_NiebieskaFigurka)
        {
            Int_NiebieskaFigurka interaction = GetComponent<Int_NiebieskaFigurka>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Int_NiebieskaFigurka is null");
        }
        if (interactionType == InteractionType.Int_ZielonaFigurka)
        {
            Int_ZielonaFigurka interaction = GetComponent<Int_ZielonaFigurka>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Int_ZielonaFigurka is null");
        }
        if (interactionType == InteractionType.Int_CzerwonaFigurka)
        {
            Int_CzerwonaFigurka interaction = GetComponent<Int_CzerwonaFigurka>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("Int_CzerwonaFigurka is null");
        }

        //--------------------   LVL  3   ---------------------------------------------------------------------------

        if (interactionType == InteractionType.Int_LibraryBook)
        {
            Int_LibraryBook interaction = GetComponent<Int_LibraryBook>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_LibraryBook is null");
        }

        if (interactionType == InteractionType.Int_LibraryKey)
        {
            Int_LibraryKey interaction = GetComponent<Int_LibraryKey>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_LibraryKey is null");
        }

        if (interactionType == InteractionType.Int_LibrarySafe)
        {
            Int_LibrarySafe interaction = GetComponent<Int_LibrarySafe>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_LibrarySafe is null");
        }


        if (interactionType == InteractionType.Int_lv1_LibraryBooks)
        {
            Int_lv1_LibraryBooks interaction = GetComponent<Int_lv1_LibraryBooks>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_LibraryBooks is null");
        }
        if (interactionType == InteractionType.Int_lv1_CircleTable)
        {
            Int_lv1_CircleTable interaction = GetComponent<Int_lv1_CircleTable>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_CircleTable is null");
        }
        if (interactionType == InteractionType.Int_lv1_HidenWallMask)
        {
            Int_lv1_HidenWallMask interaction = GetComponent<Int_lv1_HidenWallMask>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_HidenWallMask is null");
        }

        if (interactionType == InteractionType.Int_lv1_Fireplace)
        {
            Int_lv1_Fireplace interaction = GetComponent<Int_lv1_Fireplace>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_Fireplace is null");
        }

        if (interactionType == InteractionType.Int_lv1_WindowBulletCP)
        {
            Int_lv1_WindowBulletCP interaction = GetComponent<Int_lv1_WindowBulletCP>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_WindowBulletCP is null");
        }

        if (interactionType == InteractionType.Int_lv1_WindowBullet)
        {
            Int_lv1_WindowBullet interaction = GetComponent<Int_lv1_WindowBullet>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_WindowBullet is null");
        }
        if (interactionType == InteractionType.Int_lv1_lamp)
        {
            Int_lv1_lamp interaction = GetComponent<Int_lv1_lamp>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_lamp is null");
        }
        if (interactionType == InteractionType.lvl3_int_BrickButton)
        {
            lvl3_int_BrickButton interaction = GetComponent<lvl3_int_BrickButton>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl3_int_BrickButton is null");
        }

        if (interactionType == InteractionType.lvl3_int_ChestLockpick)
        {
            lvl3_int_ChestLockpick interaction = GetComponent<lvl3_int_ChestLockpick>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl3_int_ChestLockpick is null");
        }

        if (interactionType == InteractionType.lvl3_int_Lamp)
        {
            lvl3_int_Lamp interaction = GetComponent<lvl3_int_Lamp>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl3_int_Lamp is null");
        }

        if (interactionType == InteractionType.lvl3_int_BasementDoor)
        {
            lvl3_int_BasementDoor interaction = GetComponent<lvl3_int_BasementDoor>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl3_int_BasementDoor is null");
        }

        if (interactionType == InteractionType.lvl3_int_SecretLeverWall)
        {
            lvl3_int_SecretLeverWall interaction = GetComponent<lvl3_int_SecretLeverWall>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl3_int_SecretLeverWall is null");
        }

        if (interactionType == InteractionType.lvl3_int_SecretLever)
        {
            lvl3_int_SecretLever interaction = GetComponent<lvl3_int_SecretLever>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
                Debug.LogError("lvl3_int_SecretLever is null");
        }
        if (interactionType == InteractionType.Int_WatsonSwitchTutorial)
        {
            Int_WatsonSwitchTutorial interaction = GetComponent<Int_WatsonSwitchTutorial>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_WatsonSwitchTutorial is null");
        }

        if (interactionType == InteractionType.Int_lv3_Ethel)
        {
            Int_lv3_Ethel interaction = GetComponent<Int_lv3_Ethel>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_Ethel is null");
        }

        if (interactionType == InteractionType.Int_lv3_ExitDoor)
        {
            Int_lv3_ExitDoor interaction = GetComponent<Int_lv3_ExitDoor>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_ExitDoor is null");
        }

        
    }





    //-----------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------


    public void AddClue(int listNumber, Transform cardPosition = null)
    {
        if (clues == null || clues.Length == 0)
        {
            Debug.LogWarning($"{name}: AddClue called, but no clues are assigned.");
            return;
        }

        if (listNumber < 0 || listNumber >= clues.Length)
        {
            Debug.LogError($"{name}: clue index {listNumber} is outside assigned clues range 0-{clues.Length - 1}.");
            return;
        }

        if (clues[listNumber] == null)
        {
            Debug.LogError($"{name}: clue at index {listNumber} is null.");
            return;
        }

        if (ClueManager.Instance == null)
        {
            Debug.LogError($"{name}: ClueManager.Instance is null.");
            return;
        }

        if (cardPosition == null)
            cardPosition = this.transform;

        ClueManager.Instance.AddClue(clues[listNumber], cardPosition.position);

        if (questionVFX != null)
        {
            questionVFX.Stop();
            //Destroy(ClueVisualFx, 5f);
        }

    }
    public void SetQuestionFXEagleVisionState(bool active)
    {
        if (questionVFX == null)
            return;

        if (!active || (!isInteractableActive && !allowQuestionFXWhenInactive))
        {
            SetQuestionFxLifetimeMultiplier(eagleVisionExitLifetimeMultiplier);
            SetQuestionFxRate(0f);
            return;
        }

        SetQuestionFxLifetimeMultiplier(1f);
        SetQuestionFxRate(eagleVisionQuestionRate);
        questionVFX.Play();
    }

    private void CacheQuestionFxDefaultLifetime()
    {
        if (questionVFX == null || string.IsNullOrWhiteSpace(questionFxLifetimeProperty))
            return;

        if (questionVFX.HasFloat(questionFxLifetimeProperty))
        {
            defaultQuestionFxLifetime = questionVFX.GetFloat(questionFxLifetimeProperty);
            hasCachedQuestionFxLifetime = true;
        }
        else if (questionVFX.HasInt(questionFxLifetimeProperty))
        {
            defaultQuestionFxLifetime = questionVFX.GetInt(questionFxLifetimeProperty);
            hasCachedQuestionFxLifetime = true;
        }
    }

    private void SetQuestionFxLifetimeMultiplier(float multiplier)
    {
        if (questionVFX == null || string.IsNullOrWhiteSpace(questionFxLifetimeProperty))
            return;

        if (!hasCachedQuestionFxLifetime)
            CacheQuestionFxDefaultLifetime();

        if (!hasCachedQuestionFxLifetime)
        {
            if (!missingQuestionFxLifetimeReported)
            {
                Debug.LogWarning($"{name}: QuestionFX needs an exposed int or float named '{questionFxLifetimeProperty}' for lifetime control.");
                missingQuestionFxLifetimeReported = true;
            }

            return;
        }

        float lifetime = Mathf.Max(0f, defaultQuestionFxLifetime * multiplier);
        if (questionVFX.HasFloat(questionFxLifetimeProperty))
            questionVFX.SetFloat(questionFxLifetimeProperty, lifetime);
        else if (questionVFX.HasInt(questionFxLifetimeProperty))
            questionVFX.SetInt(questionFxLifetimeProperty, Mathf.RoundToInt(lifetime));
    }

    public void SetQuestionFXRate(float rate)
    {
        SetQuestionFxRate(rate);
    }

    private void SetQuestionFxRate(float rate)
    {
        if (questionVFX == null || string.IsNullOrWhiteSpace(questionFxRateProperty))
            return;

        if (questionVFX.HasInt(questionFxRateProperty))
        {
            questionVFX.SetInt(questionFxRateProperty, Mathf.RoundToInt(rate));
            return;
        }

        if (questionVFX.HasFloat(questionFxRateProperty))
        {
            questionVFX.SetFloat(questionFxRateProperty, rate);
            return;
        }

        if (!missingQuestionFxRateReported)
        {
            Debug.LogWarning($"{name}: QuestionFX needs an exposed int or float named '{questionFxRateProperty}'.");
            missingQuestionFxRateReported = true;
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

    // LVL1 - NEW

    

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
    lvl2_Int_StairsExit,

    // LVL1 - NEW

    Int_Edith_Paper,
    Int_Edith_Ring,
    Int_Edith_BulletHole,
    int_LibraryPainting,
    Int_CzerwonaFigurka,
    Int_lv1_SecretLetter,
    Int_ZielonaFigurka,
    Int_NiebieskaFigurka,

    // LVL3

    lvl3_int_BrickButton,
    lvl3_int_ChestLockpick,
    lvl3_int_Lamp,
    lvl3_int_BasementDoor,
    lvl3_int_SecretLeverWall,
    lvl3_int_SecretLever,

    GlobusButton,

    Int_LibraryBook,
    Int_LibraryKey,
    Int_LibrarySafe,
    Int_lv1_Fireplace,
    Int_lv1_LibraryBooks,
    Int_lv1_CircleTable,
    Int_lv1_HidenWallMask,
    Int_lv1_lamp,
    Int_lv1_WindowBullet,
    Int_lv1_WindowBulletCP,
    Int_lv1_SecretDoor,
    int_lv3_easyTable,
    Int_lv1_EthelPassageDoor,
    Int_lv1_BigGramm,
    Int_lv1_BigGramm_button,
    int_lv1_HenryNPC,
    int_lv1_ArthurNPC,
    int_vl1_GeorgeNPC,
    Int_lv1_BlockBox,
    Int_WatsonSwitchTutorial,
    Int_lv3_Ethel,
    Int_lv3_ExitDoor
}