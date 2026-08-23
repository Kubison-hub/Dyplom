using UnityEngine;
using System.Collections;
using UnityEngine.VFX;
using UnityEngine.AI;


public class Interactable : MonoBehaviour
{


    public bool isInteractableActive = true;
    [Space]

    [SerializeField] private InteractionType interactionType;
    public Transform interactabePoint;
    [Header("Interaction Point Occupancy")]
    [SerializeField] private bool useOccupiedPointFallback = true;
    [SerializeField, Min(0.1f)] private float interactionPointOccupiedRadius = 0.75f;
    [SerializeField, Min(0.1f)] private float interactionPointFallbackOffset = 0.9f;
    [SerializeField, Min(0.1f)] private float interactionPointNavMeshSampleRadius = 1.2f;
    [Space]
    [Header("Watson Reaction")]
    [Tooltip("When Sherlock performs this interaction, Watson turns toward this object.")]
    public bool rotateWatsonToInteraction = false;
    [Tooltip("0 means Watson only turns toward the interaction and does not walk beside Sherlock.")]
    [Min(0f)] public float watsonApproachDistance = 0f;
    public CompanionApproachMode watsonApproachMode = CompanionApproachMode.TowardActiveCharacterOnNavMesh;
    public WatsonApproachSide watsonApproachSide = WatsonApproachSide.Left;
    [Tooltip("Exact NavMesh destination used when Watson Approach Mode is Specific Transform.")]
    public Transform watsonSpecificApproachPoint;
    [Tooltip("Multiplier for Watson NavMesh movement while approaching Sherlock.")]
    [Min(0.01f)] public float watsonApproachSpeedMultiplier = 1f;
    [Tooltip("Multiplier for Watson movement and focus rotation speed.")]
    [Min(0.01f)] public float watsonApproachRotationSpeedMultiplier = 1f;
    [Tooltip("Delay between Sherlock's interaction and Watson starting to turn toward it.")]
    [Min(0f)] public float rotationReactionDelay = 0f;
    [Tooltip("Delay after Watson finishes turning before he starts approaching the interaction.")]
    [Min(0f)] public float speedReactionDelay = 0f;
    [Space]
    [Header("Sherlock Reaction")]
    [Tooltip("When Watson performs this interaction, Sherlock turns toward this object.")]
    public bool rotateSherlockToInteraction = true;
    [Tooltip("0 means Sherlock only turns toward the interaction and does not walk beside Watson.")]
    [Min(0f)] public float sherlockApproachDistance = 0f;
    public CompanionApproachMode sherlockApproachMode = CompanionApproachMode.TowardActiveCharacterOnNavMesh;
    public WatsonApproachSide sherlockApproachSide = WatsonApproachSide.Right;
    [Tooltip("Exact NavMesh destination used when Sherlock Approach Mode is Specific Transform.")]
    public Transform sherlockSpecificApproachPoint;
    [Tooltip("Multiplier for Sherlock NavMesh movement while approaching Watson.")]
    [Min(0.01f)] public float sherlockApproachSpeedMultiplier = 1f;
    [Tooltip("Multiplier for Sherlock rotation toward the interaction.")]
    [Min(0.01f)] public float sherlockApproachRotationSpeedMultiplier = 1f;
    [Tooltip("Delay between Watson's interaction and Sherlock starting to turn toward it.")]
    [Min(0f)] public float sherlockRotationReactionDelay = 0f;
    [Tooltip("Delay after Sherlock finishes turning before he starts approaching the interaction.")]
    [Min(0f)] public float sherlockSpeedReactionDelay = 0f;
    [SerializeField, Min(0.1f)] private float sherlockInteractionFocusDuration = 4f;
    [SerializeField, Min(0.1f)] private float sherlockNavMeshSampleRadius = 1f;
    [Space]

    public string objectDescription = "Wpisz nazwę obiektu";
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
    private bool watsonCarryGripAuthorized;
    private bool interactionShaderHovered;
    private bool interactionShaderForcedVisible;
    private Coroutine sherlockInteractionFocusCoroutine;

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
        interactionShaderHovered = isHovered;
        InteractionHoverLabelManager.SetHovered(this, isHovered);

        InteractionShaderFader fader = EnsureInteractionShaderFader();
        if (fader == null)
            return;

        fader.Configure(interactionShaderVisibility, interactionShaderPulseSpeed);

        if (isHovered || interactionShaderForcedVisible)
            fader.FadeIn();
        else
            fader.FadeOut();
    }
    public void SetInteractionShaderForcedVisible(bool isVisible)
    {
        interactionShaderForcedVisible = isVisible;

        InteractionShaderFader fader = EnsureInteractionShaderFader();
        if (fader == null)
            return;

        fader.Configure(interactionShaderVisibility, interactionShaderPulseSpeed);

        if (isVisible || interactionShaderHovered)
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
    // --- KLIKNIĘCIE ---
    public void TryToInteract(PlayerController player)
    {
        //if (!isInteractableActive || ConversationManager.Instance.inConversation 
        //    || TutorialManager.Instance.isTutorialActive) return;

        if (!isInteractableActive) return;

        if (!CanPlayerUseInteraction(player)) return;

        Int_lv1_SherlockWatsonSelmaDialog sherlockWatsonSelmaDialog =
            GetComponent<Int_lv1_SherlockWatsonSelmaDialog>();
        sherlockWatsonSelmaDialog?.NotifyInteractionSelected(player);

        WatsonEscortNPC escortNpc = GetComponent<WatsonEscortNPC>();
        if (escortNpc != null && escortNpc.CanBeEscortedBy(player))
        {
            WatsonEscortController escortController = player.GetComponent<WatsonEscortController>();
            if (escortController != null && escortController.TryStartEscort(escortNpc, player))
                return;
        }


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

        Int_StairsUp stairsUp = GetComponent<Int_StairsUp>();
        if (stairsUp != null && stairsUp.RedirectWhenSelmaGuardsStairs(player))
            return;

        if (TryRedirectWhenVioletObservesLibrary(player))
            return;

        IInteractionApproachGate approachGate =
            GetComponent(typeof(IInteractionApproachGate)) as IInteractionApproachGate;
        if (approachGate != null && !approachGate.CanApproachInteraction(player))
        {
            approachGate.ShowApproachBlockedText(player);
            return;
        }

        WatsonCarryable watsonCarryable = GetComponent<WatsonCarryable>();
        watsonCarryGripAuthorized = false;
        if (watsonCarryable != null &&
            player.playerCharacter == PlayerCharacter.Watson &&
            watsonCarryable.RequiresWatsonGrip &&
            MagnifierGlassController.IsWatsonGripActive)
        {
            watsonCarryGripAuthorized = true;
        }

        player.currentInteractable = this;
        player.ClearAutoInteractionApproachPoint();

        lvl3_int_Lamp levelThreeLamp = GetComponent<lvl3_int_Lamp>();
        Int_lv3_Doll levelThreeDoll = GetComponent<Int_lv3_Doll>();
        bool useNearestNavMeshApproach =
            levelThreeLamp != null && levelThreeLamp.UseNearestNavMeshApproach ||
            levelThreeDoll != null && levelThreeDoll.UseNearestNavMeshApproach;

        if (!useNearestNavMeshApproach && interactabePoint != null)
        {
            player.currentInteractionPoint = interactabePoint;

            if (useOccupiedPointFallback &&
                IsInteractionPointOccupied(player) &&
                TryGetFreeInteractionPoint(player, out Vector3 fallbackPoint))
            {
                player.currentInteractionPoint = null;
                player.SetAutoInteractionApproachPoint(fallbackPoint);
            }
        }
        else
        {
            player.currentInteractionPoint = null;

            if (useNearestNavMeshApproach && TryGetWatsonCarryApproachPoint(player, out Vector3 approachPoint))
                player.SetAutoInteractionApproachPoint(approachPoint);
        }

        if (watsonCarryable != null &&
            player.playerCharacter == PlayerCharacter.Watson &&
            (!watsonCarryable.RequiresWatsonGrip || watsonCarryGripAuthorized))
        {
            if (watsonCarryable.IsBlockedByHeldLamp)
            {
                player.RotateTowardsInteractableAndShowTopText(this, watsonCarryable.HeldLampFailureText);
                return;
            }

            if (!watsonCarryable.CanBePickedUp())
            {
                player.RotateTowardsInteractableAndShowTopText(this, watsonCarryable.LightRequirementFailureText);
                return;
            }

            if (interactabePoint == null && TryGetWatsonCarryApproachPoint(player, out Vector3 approachPoint))
                player.SetAutoInteractionApproachPoint(approachPoint);
        }

        player.MoveToInteractable();

        if (stairsUp != null)
            stairsUp.NotifySherlockStairsApproach(player);

        // Drzwi
        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();
            if (door != null) door.Interact(player); // Drzwi same ustawiają ruch gracza
            else Debug.LogError("DoorTrigger component not found");
        }



        // Pianino
        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();
            if (piano != null) piano.Interact(player);
            else Debug.LogError("Piano component not found");
        }

        // Przełącznik drzwi
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
            PlayerTopText.Instance.ShowTopText("Ta ściana odstaje od reszy", "Ewidentnie");
        }
    }

    // --- DOTARCIE DO CELU ---
    private bool TryRedirectWhenVioletObservesLibrary(PlayerController player)
    {
        Int_LibraryBook libraryBook = GetComponent<Int_LibraryBook>();
        if (libraryBook != null)
            return libraryBook.RedirectWhenVioletIsInRoom(player);

        Int_lv1_LibraryBooks libraryBooks = GetComponent<Int_lv1_LibraryBooks>();
        if (libraryBooks != null)
            return libraryBooks.RedirectWhenVioletIsInRoom(player);

        Int_Globus globus = GetComponent<Int_Globus>();
        if (globus != null)
            return globus.RedirectWhenVioletIsInRoom(player);

        IVioletRoomInteractionGate violetGate =
            GetComponent(typeof(IVioletRoomInteractionGate)) as IVioletRoomInteractionGate;
        return violetGate != null && violetGate.RedirectWhenVioletIsInRoom(player);
    }

    public void PerformInteraction(PlayerController player)
    {
        Int_lv4_EthelEntrance directEthelEntrance = GetComponent<Int_lv4_EthelEntrance>();
        if (directEthelEntrance != null)
        {
            directEthelEntrance.PerformInteraction(player);
            return;
        }

        Int_Lvl_3_Leadder directBasementLadder = GetComponent<Int_Lvl_3_Leadder>();
        if (directBasementLadder != null)
        {
            directBasementLadder.PerformInteraction(player);
            return;
        }

        Int_lv4_HatchToBassement directHatchToBassement = GetComponent<Int_lv4_HatchToBassement>();
        if (directHatchToBassement != null)
        {
            directHatchToBassement.PerformInteraction(player);
            return;
        }

        if (interactionType == InteractionType.Int_lv4_Hatch)
        {
            Int_lv4_Hatch lv4Hatch = GetComponent<Int_lv4_Hatch>();
            if (lv4Hatch != null)
            {
                lv4Hatch.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_lv4_HatchToBassement)
        {
            Int_lv4_HatchToBassement lv4HatchToBassementInteraction = GetComponent<Int_lv4_HatchToBassement>();
            if (lv4HatchToBassementInteraction != null)
            {
                lv4HatchToBassementInteraction.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_lv4_HiddenDoor)
        {
            Int_lv4_HiddenDoor lv4HiddenDoorInteraction = GetComponent<Int_lv4_HiddenDoor>();
            if (lv4HiddenDoorInteraction != null)
            {
                lv4HiddenDoorInteraction.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_lv1_WoodBlockButton)
        {
            Int_lv1_WoodBlockButton interaction = GetComponent<Int_lv1_WoodBlockButton>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_lv2_WoodBlockButton)
        {
            Int_lv2_WoodBlockButton interaction = GetComponent<Int_lv2_WoodBlockButton>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_lv2_WoodBrickWall)
        {
            Int_lv2_WoodBrickWall interaction = GetComponent<Int_lv2_WoodBrickWall>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_lv2_WoodBrickWallButton)
        {
            Int_lv2_WoodBrickWallButton interaction = GetComponent<Int_lv2_WoodBrickWallButton>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_EthelGotoLastPoint)
        {
            Int_EthelGotoLastPoint ethelGoToLastPoint = GetComponent<Int_EthelGotoLastPoint>();
            if (ethelGoToLastPoint != null)
            {
                ethelGoToLastPoint.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_SherlockWatsonGoToLastPoint)
        {
            Int_SherlockWatsonGoToLastPoint playersGoToLastPoint = GetComponent<Int_SherlockWatsonGoToLastPoint>();
            if (playersGoToLastPoint != null)
            {
                playersGoToLastPoint.PerformInteraction(player);
                return;
            }
        }

        if (interactionType == InteractionType.Int_SetupLastPoints)
        {
            Int_SetupLastPoints setupLastPoints = GetComponent<Int_SetupLastPoints>();
            if (setupLastPoints != null)
            {
                setupLastPoints.PerformInteraction(player);
                return;
            }
        }

        if (!isInteractableActive) return;

        if (!CanPlayerUseInteraction(player)) return;

        Int_StairsUp stairsUp = GetComponent<Int_StairsUp>();
        if (stairsUp != null && stairsUp.RedirectWhenSelmaGuardsStairs(player))
            return;

// The bsWallDoor calls Watson only after its loupe pattern is solved.
        // Sherlock may still react when Watson is the active player.
        if (GetComponent<Int_lv3_bsWallDoor>() == null || IsWatsonPlayer(player))
            UpdateCompanionInteractionFocus(player);

        lvl3_int_Lamp lvl3Lamp = GetComponent<lvl3_int_Lamp>();
        if (lvl3Lamp != null)
        {
            lvl3Lamp.PerformInteraction(player);
            return;
        }

        Int_lv3_Clock lvl3Clock = GetComponent<Int_lv3_Clock>();
        if (lvl3Clock != null)
        {
            lvl3Clock.PerformInteraction(player);
            return;
        }

        Lvl3ClockworkInteraction clockworkInteraction = GetComponent<Lvl3ClockworkInteraction>();
        if (clockworkInteraction != null)
        {
            clockworkInteraction.PerformInteraction(player);
            return;
        }

        WatsonCarryable watsonCarryable = GetComponent<WatsonCarryable>();
        if (watsonCarryable != null && player != null && player.playerCharacter == PlayerCharacter.Watson)
        {
            bool mayCarryObject = !watsonCarryable.RequiresWatsonGrip || watsonCarryGripAuthorized;
            watsonCarryGripAuthorized = false;

            if (mayCarryObject)
            {
                Int_lv3_Manequine mannequin = GetComponent<Int_lv3_Manequine>();
                if (mannequin != null)
                {
                    mannequin.TriggerWatsonTrap(player);
                    return;
                }

                watsonCarryable.PerformInteraction(player);
                return;
            }
        }

        Int_lv3_Manequine mannequinInteraction = GetComponent<Int_lv3_Manequine>();
        if (mannequinInteraction != null)
        {
            mannequinInteraction.PerformInteraction(player);
            return;
        }

        Int_lv3_ClockClue clockClue = GetComponent<Int_lv3_ClockClue>();
        if (clockClue != null)
        {
            clockClue.PerformInteraction(player);
            return;
        }

        Int_lv3_ManequineExamCP mannequinExamClue = GetComponent<Int_lv3_ManequineExamCP>();
        if (mannequinExamClue != null)
        {
            mannequinExamClue.PerformInteraction(player);
            return;
        }

        Int_lv3_SpecialBrick specialBrick = GetComponent<Int_lv3_SpecialBrick>();
        if (specialBrick != null)
        {
            specialBrick.PerformInteraction(player);
            return;
        }

        Int_lv3_SpecialBrickDoor specialBrickDoor = GetComponent<Int_lv3_SpecialBrickDoor>();
        if (specialBrickDoor != null)
        {
            specialBrickDoor.PerformInteraction(player);
            return;
        }

        Int_WatsonSwitchTutorial watsonSwitchTutorial = GetComponent<Int_WatsonSwitchTutorial>();
        if (watsonSwitchTutorial != null && watsonSwitchTutorial.isActiveAndEnabled)
        {
            watsonSwitchTutorial.PerformInteraction(player);
            return;
        }

        Int_lv4_Hatch hatch = GetComponent<Int_lv4_Hatch>();
        if (hatch != null)
        {
            hatch.PerformInteraction(player);
            return;
        }

        Int_lv4_HatchToBassement hatchToBassement = GetComponent<Int_lv4_HatchToBassement>();
        if (hatchToBassement != null)
        {
            hatchToBassement.PerformInteraction(player);
            return;
        }

        Int_lv4_HiddenDoor hiddenDoor = GetComponent<Int_lv4_HiddenDoor>();
        if (hiddenDoor != null)
        {
            hiddenDoor.PerformInteraction(player);
            return;
        }

        if (TryRedirectWhenVioletObservesLibrary(player))
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
            if (interaction != null && interaction.isActiveAndEnabled)
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
        if (interactionType == InteractionType.Int_lv1_HidenPassageButton)
        {
            Int_lv1_HidenPassageButton interaction = GetComponent<Int_lv1_HidenPassageButton>();
            if (interaction != null)
            {
                interaction.PerformInteraction(player);
            }
            else
            {
                Debug.LogError("Int_lv1_HidenPassageButton is null");
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

        if (interactionType == InteractionType.Int_lv1_SherlockWatsonSelmaDialog)
        {
            Int_lv1_SherlockWatsonSelmaDialog interaction = GetComponent<Int_lv1_SherlockWatsonSelmaDialog>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv1_SherlockWatsonSelmaDialog is null");
        }

        if (interactionType == InteractionType.Int_lv3_Ethel)
        {
            Int_lv3_Ethel interaction = GetComponent<Int_lv3_Ethel>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_Ethel is null");
        }

        if (interactionType == InteractionType.Int_lv3_Ethel_2)
        {
            Int_lv3_Ethel_2 interaction = GetComponent<Int_lv3_Ethel_2>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_Ethel_2 is null");
        }
        if (interactionType == InteractionType.Int_lv3_ExitDoor)
        {
            Int_lv3_ExitDoor interaction = GetComponent<Int_lv3_ExitDoor>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_ExitDoor is null");
        }

        if (interactionType == InteractionType.Int_lv3_HiddenPassageWall)
        {
            Int_lv3_HiddenPassageWall interaction = GetComponent<Int_lv3_HiddenPassageWall>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_HiddenPassageWall is null");
        }

        if (interactionType == InteractionType.Int_lv3_BoxWall)
        {
            Int_lv3_BoxWall interaction = GetComponent<Int_lv3_BoxWall>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_BoxWall is null");
        }

        if (interactionType == InteractionType.Int_lv3_Keyhole)
        {
            Int_lv3_Keyhole interaction = GetComponent<Int_lv3_Keyhole>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_Keyhole is null");
        }
        if (interactionType == InteractionType.Int_lv3_ControlUnit)
        {
            Int_lv3_ControlUnit interaction = GetComponent<Int_lv3_ControlUnit>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_ControlUnit is null");
        }
        if (interactionType == InteractionType.Int_lv3_HandSigns)
        {
            Int_lv3_HandSigns interaction = GetComponent<Int_lv3_HandSigns>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_HandSigns is null");
        }

        if (interactionType == InteractionType.Int_lv3_bsWallButton)
        {
            Int_lv3_bsWallButton interaction = GetComponent<Int_lv3_bsWallButton>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_bsWallButton is null");
        }

        if (interactionType == InteractionType.Int_lv3_bsWallDoor)
        {
            Int_lv3_bsWallDoor interaction = GetComponent<Int_lv3_bsWallDoor>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_bsWallDoor is null");
        }

        if (interactionType == InteractionType.Int_Lvl_3_Leadder)
        {
            Int_Lvl_3_Leadder interaction = GetComponent<Int_Lvl_3_Leadder>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_Lvl_3_Leadder is null");
        }

        if (interactionType == InteractionType.Int_lv3_boxPrint)
        {
            Int_lv3_boxPrint interaction = GetComponent<Int_lv3_boxPrint>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_boxPrint is null");
        }

        if (interactionType == InteractionType.Int_lv3_FootPath)
        {
            Int_lv3_FootPath interaction = GetComponent<Int_lv3_FootPath>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_FootPath is null");
        }
        if (interactionType == InteractionType.Int_lv3_SmallBox)
        {
            Int_lv3_SmallBox interaction = GetComponent<Int_lv3_SmallBox>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_SmallBox is null");
        }
        if (interactionType == InteractionType.Int_lv3_HeavyBox)
        {
            Int_lv3_HeavyBox interaction = GetComponent<Int_lv3_HeavyBox>();
            if (interaction != null)
                interaction.PerformInteraction(player);
            else
                Debug.LogError("Int_lv3_HeavyBox is null");
        }

        
    }





    private bool IsInteractionPointOccupied(PlayerController player)
    {
        if (interactabePoint == null)
            return false;

        foreach (PlayerController otherPlayer in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (otherPlayer == null || otherPlayer == player || !otherPlayer.gameObject.activeInHierarchy)
                continue;

            if (HorizontalDistance(otherPlayer.transform.position, interactabePoint.position) <= interactionPointOccupiedRadius)
                return true;

            bool isWalkingToThisPoint = otherPlayer.currentInteractable == this &&
                                        otherPlayer.currentInteractionPoint == interactabePoint &&
                                        otherPlayer.navMeshAgent != null &&
                                        otherPlayer.navMeshAgent.hasPath;
            if (isWalkingToThisPoint)
                return true;
        }

        return false;
    }

    private bool TryGetFreeInteractionPoint(PlayerController player, out Vector3 fallbackPoint)
    {
        fallbackPoint = interactabePoint != null ? interactabePoint.position : transform.position;
        if (interactabePoint == null || player == null || player.navMeshAgent == null)
            return false;

        Vector3[] directions =
        {
            interactabePoint.right,
            -interactabePoint.right,
            interactabePoint.forward,
            -interactabePoint.forward
        };

        foreach (Vector3 direction in directions)
        {
            Vector3 candidate = interactabePoint.position + direction * interactionPointFallbackOffset;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navMeshHit, interactionPointNavMeshSampleRadius, NavMesh.AllAreas))
                continue;

            if (IsFallbackPointOccupied(player, navMeshHit.position))
                continue;

            NavMeshPath path = new NavMeshPath();
            if (!player.navMeshAgent.CalculatePath(navMeshHit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;

            fallbackPoint = navMeshHit.position;
            return true;
        }

        return false;
    }

    private bool IsFallbackPointOccupied(PlayerController player, Vector3 point)
    {
        foreach (PlayerController otherPlayer in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (otherPlayer == null || otherPlayer == player || !otherPlayer.gameObject.activeInHierarchy)
                continue;

            if (HorizontalDistance(otherPlayer.transform.position, point) <= interactionPointOccupiedRadius)
                return true;
        }

        return false;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;
        return Vector3.Distance(first, second);
    }
    private bool TryGetWatsonCarryApproachPoint(PlayerController player, out Vector3 approachPoint)
    {
        approachPoint = transform.position;
        if (player == null || player.navMeshAgent == null)
            return false;

        Vector3 playerPosition = player.transform.position;
        Collider nearestCollider = null;
        Vector3 closestPoint = transform.position;
        float closestDistanceSqr = float.PositiveInfinity;

        foreach (Collider targetCollider in GetComponentsInChildren<Collider>())
        {
            if (targetCollider == null || !targetCollider.enabled)
                continue;

            Vector3 candidate = targetCollider.ClosestPoint(playerPosition);
            float distanceSqr = (candidate - playerPosition).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closestDistanceSqr = distanceSqr;
            closestPoint = candidate;
            nearestCollider = targetCollider;
        }

        if (nearestCollider == null)
            return false;

        Vector3 directionAwayFromBox = playerPosition - closestPoint;
        directionAwayFromBox.y = 0f;
        if (directionAwayFromBox.sqrMagnitude < 0.001f)
        {
            directionAwayFromBox = playerPosition - transform.position;
            directionAwayFromBox.y = 0f;
        }

        if (directionAwayFromBox.sqrMagnitude < 0.001f)
            directionAwayFromBox = -transform.forward;

        float clearance = Mathf.Max(player.navMeshAgent.radius + 0.1f, 0.4f);
        Vector3 desiredPoint = closestPoint + directionAwayFromBox.normalized * clearance;

        if (!NavMesh.SamplePosition(desiredPoint, out NavMeshHit navMeshHit, 1.5f, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!player.navMeshAgent.CalculatePath(navMeshHit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        approachPoint = navMeshHit.position;
        return true;
    }
    //-----------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------
    private bool CanPlayerUseInteraction(PlayerController player)
    {
        // This toggle must remain usable from either side of the passage.
        if (GetComponent<Int_lv1_HidenPassageButton>() != null)
            return true;

        if (GetComponent<Int_lv4_EthelEntrance>() != null)
            return true;

        if (GetComponent<Int_Lvl_3_Leadder>() != null)
            return true;

        if (GetComponent<Int_lv4_HatchToBassement>() != null)
            return true;


        // LVL4 hatch interactions are available to Watson.
        if (interactionType == InteractionType.Int_lv4_Hatch ||
            interactionType == InteractionType.Int_lv4_HatchToBassement)
        {
            return true;
        }

        if (player == null)
            return false;

        WatsonEscortNPC escortNpc = GetComponent<WatsonEscortNPC>();
        if (escortNpc != null)
        {
            bool isWatson = player.playerCharacter == PlayerCharacter.Watson ||
                            player.CompareTag("PlayerB") ||
                            (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1);
            return !isWatson || escortNpc.CanBeEscortedBy(player);
        }

        lvl3_int_Lamp lvl3Lamp = GetComponent<lvl3_int_Lamp>();
        if (lvl3Lamp != null)
            return lvl3Lamp.CanPlayerUse(player);

        lvl3_int_ChestLockpick lvl3Chest = GetComponent<lvl3_int_ChestLockpick>();
        if (lvl3Chest != null)
            return lvl3Chest.CanPlayerUse(player);

        // SmallChest uses Int_lv3_SmallBox and has distinct dialogue for Sherlock and Watson.
        if (GetComponent<Int_lv3_SmallBox>() != null)
            return true;

        Int_lv3_Clock lvl3Clock = GetComponent<Int_lv3_Clock>();
        if (lvl3Clock != null)
            return lvl3Clock.CanPlayerUse(player);

        Lvl3ClockworkInteraction clockworkInteraction = GetComponent<Lvl3ClockworkInteraction>();
        if (clockworkInteraction != null)
            return clockworkInteraction.CanPlayerUse(player);

        WatsonCarryable watsonCarryable = GetComponent<WatsonCarryable>();
        if (watsonCarryable != null)
        {
            bool isWatson = player.playerCharacter == PlayerCharacter.Watson ||
                            player.CompareTag("PlayerB") ||
                            (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1);

            bool canSherlockUse = player.playerCharacter == PlayerCharacter.Sherlock ||
                                  player.CompareTag("PlayerA") ||
                                  (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 0);

            // The mannequin has a normal inspection interaction for both characters,
            // while Watson holding F triggers its carryable trap.
            if (GetComponent<Int_lv3_Manequine>() != null)
                return isWatson || canSherlockUse;

            // A heavy box may also have a Sherlock inspection interaction on the same object.
            if (GetComponent<Int_lv3_HeavyBox>() != null)
                return isWatson || canSherlockUse;

            return isWatson;
        }

        bool isSherlock = player.playerCharacter == PlayerCharacter.Sherlock ||
                          player.CompareTag("PlayerA") ||
                          (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 0);

        return isSherlock || IsDialogueInteraction();
    }

    private bool IsDialogueInteraction()
    {
        if (GetComponent<Int_lv1_NpcDialogBase>() != null)
            return true;

        return interactionType == InteractionType.Dialog ||
               interactionType == InteractionType.SelmaDialog ||
               interactionType == InteractionType.VioletDialog ||
               interactionType == InteractionType.WatsonDialogSherlock ||
               interactionType == InteractionType.WatsonDialogViolet ||
               interactionType == InteractionType.WatsonDialogViolet_2;
    }
    private void UpdateCompanionInteractionFocus(PlayerController player)
    {
        if (player == null)
            return;

        Int_lv3_Manequine mannequin = GetComponent<Int_lv3_Manequine>();
        bool isRepeatMannequinInspection = mannequin != null && !mannequin.IsFirstInspection;

        if (IsSherlockPlayer(player))
        {
            WatsonCompanionController watson = WatsonCompanionController.Instance;
            if (watson == null)
                return;

            if (rotateWatsonToInteraction)
                watson.FocusInteraction(
                    transform,
                    isRepeatMannequinInspection ? 0f : watsonApproachDistance,
                    watsonApproachSpeedMultiplier,
                    watsonApproachRotationSpeedMultiplier,
                    watsonApproachMode,
                    watsonApproachSide,
                    interactabePoint,
                    rotationReactionDelay,
                    speedReactionDelay,
                    watsonSpecificApproachPoint);
            else
                watson.ClearInteractionFocus();

            return;
        }

        if (!IsWatsonPlayer(player) || !rotateSherlockToInteraction)
            return;

        if (sherlockInteractionFocusCoroutine != null)
            StopCoroutine(sherlockInteractionFocusCoroutine);

        sherlockInteractionFocusCoroutine = StartCoroutine(FocusSherlockOnInteraction(player));
    }

    private IEnumerator FocusSherlockOnInteraction(PlayerController watson)
    {
        PlayerController sherlock = GetSherlockPlayerController();
        if (sherlock == null || sherlock.navMeshAgent == null)
            yield break;

        NavMeshAgent sherlockAgent = sherlock.navMeshAgent;
        float originalSpeed = sherlockAgent.speed;
        float originalAngularSpeed = sherlockAgent.angularSpeed;

        if (sherlockRotationReactionDelay > 0f)
            yield return new WaitForSeconds(sherlockRotationReactionDelay);

        yield return RotateSherlockTowardInteraction(sherlock);

        if (sherlockSpeedReactionDelay > 0f)
            yield return new WaitForSeconds(sherlockSpeedReactionDelay);

        float effectiveSherlockApproachDistance =
            GetComponent<Int_lv3_Manequine>() is Int_lv3_Manequine mannequin && !mannequin.IsFirstInspection
                ? 0f
                : sherlockApproachDistance;

        bool useSpecificSherlockPoint = sherlockApproachMode == CompanionApproachMode.SpecificTransform &&
                                       sherlockSpecificApproachPoint != null;
        if ((effectiveSherlockApproachDistance > 0f || useSpecificSherlockPoint) &&
            watson != null && sherlockAgent.isOnNavMesh)
        {
            bool needsApproach;
            Vector3 desiredPosition;

            if (useSpecificSherlockPoint)
            {
                desiredPosition = sherlockSpecificApproachPoint.position;
                needsApproach = true;
            }
            else if (sherlockApproachMode == CompanionApproachMode.InteractionPointSideOffset)
            {
                Transform anchor = interactabePoint != null ? interactabePoint : transform;
                Vector3 sideDirection = sherlockApproachSide == WatsonApproachSide.Left
                    ? -anchor.right
                    : anchor.right;
                desiredPosition = anchor.position + sideDirection * effectiveSherlockApproachDistance;
                needsApproach = true;
            }
            else
            {
                Vector3 directionFromWatson = sherlock.transform.position - watson.transform.position;
                directionFromWatson.y = 0f;
                float currentDistance = directionFromWatson.magnitude;

                // Sherlock approaches only when the characters are farther apart than requested.
                needsApproach = currentDistance > effectiveSherlockApproachDistance && currentDistance > 0.001f;
                desiredPosition = watson.transform.position +
                                  directionFromWatson.normalized * effectiveSherlockApproachDistance;
            }

            if (needsApproach &&
                NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, sherlockNavMeshSampleRadius, NavMesh.AllAreas))
            {
                sherlockAgent.speed = originalSpeed * sherlockApproachSpeedMultiplier;
                sherlockAgent.angularSpeed = originalAngularSpeed * sherlockApproachRotationSpeedMultiplier;
                sherlockAgent.isStopped = false;
                sherlockAgent.SetDestination(hit.position);

                while (sherlockAgent.pathPending)
                    yield return null;

                while (sherlockAgent.hasPath &&
                       sherlockAgent.remainingDistance > sherlockAgent.stoppingDistance + 0.08f)
                    yield return null;

                sherlockAgent.ResetPath();
            }
        }

        sherlockAgent.speed = originalSpeed;
        sherlockAgent.angularSpeed = originalAngularSpeed;

        float focusUntil = Time.time + sherlockInteractionFocusDuration;
        while (Time.time < focusUntil)
        {
            Vector3 direction = transform.position - sherlock.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                sherlock.transform.rotation = Quaternion.RotateTowards(
                    sherlock.transform.rotation,
                    targetRotation,
                    220f * sherlockApproachRotationSpeedMultiplier * Time.deltaTime);
            }

            yield return null;
        }

        sherlockInteractionFocusCoroutine = null;
    }

    private IEnumerator RotateSherlockTowardInteraction(PlayerController sherlock)
    {
        if (sherlock == null)
            yield break;

        while (true)
        {
            Vector3 direction = transform.position - sherlock.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                yield break;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            sherlock.transform.rotation = Quaternion.RotateTowards(
                sherlock.transform.rotation,
                targetRotation,
                220f * sherlockApproachRotationSpeedMultiplier * Time.deltaTime);

            if (Quaternion.Angle(sherlock.transform.rotation, targetRotation) <= 1f)
                yield break;

            yield return null;
        }
    }

    private static bool IsSherlockPlayer(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Sherlock || player.CompareTag("PlayerA"));
    }

    private static bool IsWatsonPlayer(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
    }

    private static PlayerController GetSherlockPlayerController()
    {
        foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (IsSherlockPlayer(player))
                return player;
        }

        return null;
    }


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
    Int_lv3_ExitDoor,
    Int_lv3_HiddenPassageWall,
    Int_lv3_BoxWall,
    Int_lv3_HandSigns,
    Int_lv3_bsWallButton,
    Int_lv3_bsWallDoor,
    Int_Lvl_3_Leadder,
    Int_lv3_boxPrint,
    Int_lv3_FootPath,    Int_lv3_SmallBox,
    Int_lv3_HeavyBox,
    Int_lv3_ControlUnit,
    Int_lv3_Keyhole,
    Int_lv3_ClockKey,
    Int_lv3_Ethel_2,
    Int_lv3_Manequine,
    Int_lv3_Doll,
    Int_lv3_ClockClue,
    Int_lv3_SpecialBrick,
    Int_lv3_SpecialBrickDoor,
    Int_lv3_ManequineExamCP,
    Int_lv4_Hatch,
    Int_lv4_HatchToBassement,
    Int_lv4_HiddenDoor,
    Int_lv4_EthelEntrance,
    Int_lv1_HidenPassageButton,
    Int_lv1_SherlockWatsonSelmaDialog,
    Int_EthelGotoLastPoint,
    Int_SherlockWatsonGoToLastPoint,
    Int_SetupLastPoints,
    Int_lv1_WoodBlockButton,
    Int_lv2_WoodBlockButton,
    Int_lv2_WoodBrickWall,
    Int_lv2_WoodBrickWallButton
}
