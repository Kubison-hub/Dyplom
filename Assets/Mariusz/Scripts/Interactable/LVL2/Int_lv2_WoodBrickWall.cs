using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Interactable))]
public class Int_lv2_WoodBrickWall : Lvl3InteractionDialogueBase
{
    [Serializable]
    private class WoodBlockWallVariant
    {
        public ItemType itemType;
        [Tooltip("The matching wooden block model shown while this block is mounted in the wall.")]
        public GameObject wallEquivalent;
        [Tooltip("This mounted block's button. Its Is Correct Door setting decides whether it opens these doors.")]
        public lvl2_Int_EthelWallButton ethelWallButton;
        [Tooltip("Optional objects enabled only while this block is mounted.")]
        public GameObject[] activateWhileMounted;
        [Tooltip("Optional objects disabled only while this block is mounted.")]
        public GameObject[] deactivateWhileMounted;
    }

    [Header("Wooden Blocks")]
    [SerializeField] private WoodBlockWallVariant[] blockVariants;
    [SerializeField] private ItemType correctWoodBlock = ItemType.WoodBlockLevel2;

    [Header("Door To Test")]
    [SerializeField] private lvl2_Int_EthelWallButton ethelWallButton;
    [SerializeField] private Int_lv4_HiddenDoor hiddenDoor;

    [Header("Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] noBlockDialogue;
    [SerializeField] private Lvl3DialogueLine[] insertDialogue;
    [SerializeField] private Lvl3DialogueLine[] wrongBlockDialogue;
    [SerializeField] private Lvl3DialogueLine[] returnDialogue;
    [SerializeField] private Lvl3DialogueLine[] alreadyRejectedBlockDialogue;

    [Header("Notebook")]
    [SerializeField] private int openedDoorNoteIndex = 0;

    [Header("Correct Door")]
    [Tooltip("Optional object disabled after the correct door starts opening. The WoodBrickWall itself stays active.")]
    [SerializeField] private GameObject gameObjectToDeactivateAfterCorrectDoor;
    [Tooltip("Optional additional Question FX or GameObject disabled after the correct door opens.")]
    [SerializeField] private GameObject additionalObjectToDeactivateAfterCorrectDoor;
    [Tooltip("Optional Interactable whose Question FX follows normal Eagle Vision rules and is disabled after the correct door opens.")]
    [SerializeField] private Interactable additionalQuestionFxInteractable;
    [Tooltip("Time allowed for the correct door's opening animation before the assigned object is disabled.")]
    [SerializeField, Min(0f)] private float deactivateAfterCorrectDoorDelay = 1.1f;

    private WoodBlockWallVariant mountedVariant;
    private bool blockWasTested;
    private bool correctBlockAccepted;
    private bool openedDoorNoteAdded;
    private Interactable socketInteractable;
    private readonly HashSet<ItemType> rejectedBlockTypes = new HashSet<ItemType>();

    protected override Lvl3DialogueLine[] DefaultDialogueLines => noBlockDialogue;

    private void Awake()
    {
        SetupInteractable(InteractionType.Int_lv2_WoodBrickWall);
        socketInteractable = GetComponent<Interactable>();
        if (socketInteractable != null)
            socketInteractable.addDatabaseNotesAutomatically = false;
        SetMountedVisual(null, false);
    }

    private void Reset() => SetupInteractable(InteractionType.Int_lv2_WoodBrickWall);
    private void OnValidate() => SetupInteractable(InteractionType.Int_lv2_WoodBrickWall);

    public void PerformInteraction(PlayerController player)
    {
        if (mountedVariant == null)
        {
            TryInsertSelectedBlock(player);
            return;
        }

        UseMountedBlock(player);
    }

    public void UseMountedBlock(PlayerController player)
    {
        if (mountedVariant == null)
            return;

        // The correct wooden button stays mounted after it opens the door.
        if (correctBlockAccepted)
            return;

        if (!blockWasTested)
        {
            blockWasTested = true;
            TestMountedBlock(player);
            return;
        }

        ReturnMountedBlock(player);
    }

    public bool IsMountedWallButton(lvl2_Int_EthelWallButton wallButton)
    {
        return mountedVariant != null && mountedVariant.ethelWallButton == wallButton;
    }

    private void TryInsertSelectedBlock(PlayerController player)
    {
        if (InventoryManager.Instance == null ||
            !TryTakePreferredWallBlock(out ItemType selectedBlock))
        {
            PlayDialogue(player, noBlockDialogue);
            return;
        }

        if (rejectedBlockTypes.Contains(selectedBlock))
        {
            InventoryManager.Instance.TryAddItem(selectedBlock);
            PlayDialogue(player, alreadyRejectedBlockDialogue);
            return;
        }

        WoodBlockWallVariant variant = FindVariant(selectedBlock);
        if (variant == null)
        {
            InventoryManager.Instance.TryAddItem(selectedBlock);
            PlayDialogue(player, noBlockDialogue);
            return;
        }

        mountedVariant = variant;
        blockWasTested = false;
        correctBlockAccepted = false;
        SetMountedVisual(mountedVariant, true);
        if (socketInteractable != null)
            socketInteractable.isInteractableActive = false;

        PlayDialogue(player, insertDialogue);
    }

    private bool TryTakePreferredWallBlock(out ItemType selectedBlock)
    {
        selectedBlock = default;

        // Element 0 is the intended block for this wall. Prefer it even when the
        // inventory UI currently has the other wooden block selected.
        if (blockVariants != null && blockVariants.Length > 0 &&
            blockVariants[0] != null &&
            InventoryManager.Instance.items.Contains(blockVariants[0].itemType) &&
            InventoryManager.Instance.TryRemoveItem(blockVariants[0].itemType))
        {
            selectedBlock = blockVariants[0].itemType;
            return true;
        }

        return InventoryManager.Instance.TryTakeSelectedWoodBlock(out selectedBlock);
    }

    private void TestMountedBlock(PlayerController player)
    {
        if (!IsMountedBlockCorrect())
        {
            rejectedBlockTypes.Add(mountedVariant.itemType);
            PlayDialogue(player, wrongBlockDialogue);
            return;
        }

        correctBlockAccepted = true;
        GetComponent<Interactable>()?.MarkCompleted();
        SetMountedVisual(mountedVariant, true);

        lvl2_Int_EthelWallButton mountedButton = GetMountedEthelWallButton();
        if (mountedButton != null)
        {
            mountedButton.OpenWithInstalledWoodBlock(player);
            AddOpenedDoorNotebookNote();
            CluesLog.Instance?.CompleteEthelHiddenDoorObjective();
            StartCoroutine(DeactivateAfterCorrectDoorOpens());
            return;
        }

        if (hiddenDoor != null)
        {
            hiddenDoor.OpenWithInstalledWoodBlock(player);
            AddOpenedDoorNotebookNote();
            CluesLog.Instance?.CompleteEthelHiddenDoorObjective();
            StartCoroutine(DeactivateAfterCorrectDoorOpens());
        }
    }

    private IEnumerator DeactivateAfterCorrectDoorOpens()
    {
        if (deactivateAfterCorrectDoorDelay > 0f)
            yield return new WaitForSeconds(deactivateAfterCorrectDoorDelay);

        if (additionalQuestionFxInteractable != null)
        {
            additionalQuestionFxInteractable.isInteractableActive = false;
            additionalQuestionFxInteractable.allowQuestionFXWhenInactive = false;
            additionalQuestionFxInteractable.SetQuestionFXEagleVisionState(false);
        }

        if (additionalObjectToDeactivateAfterCorrectDoor != null)
            additionalObjectToDeactivateAfterCorrectDoor.SetActive(false);

        if (gameObjectToDeactivateAfterCorrectDoor != null)
            gameObjectToDeactivateAfterCorrectDoor.SetActive(false);
    }

    private void AddOpenedDoorNotebookNote()
    {
        if (openedDoorNoteAdded || openedDoorNoteIndex < 0 || socketInteractable == null)
            return;

        openedDoorNoteAdded = true;
        socketInteractable.AddNote(openedDoorNoteIndex);
    }

    private bool IsMountedBlockCorrect()
    {
        // Level 2's physical Ethel wall button owns the door-match decision.
        // The fallback remains for older scenes and walls that open a Level 4 HiddenDoor instead.
        lvl2_Int_EthelWallButton mountedButton = GetMountedEthelWallButton();
        if (mountedButton != null)
            return mountedButton.IsCorrectDoor();

        return mountedVariant != null && mountedVariant.itemType == correctWoodBlock;
    }

    private lvl2_Int_EthelWallButton GetMountedEthelWallButton()
    {
        if (mountedVariant != null && mountedVariant.ethelWallButton != null)
            return mountedVariant.ethelWallButton;

        return ethelWallButton;
    }

    private void ReturnMountedBlock(PlayerController player)
    {
        if (mountedVariant == null || InventoryManager.Instance == null ||
            !InventoryManager.Instance.TryAddItem(mountedVariant.itemType))
        {
            PlayDialogue(player, noBlockDialogue);
            return;
        }

        SetMountedVisual(mountedVariant, false);
        mountedVariant = null;
        blockWasTested = false;
        correctBlockAccepted = false;
        if (socketInteractable != null)
            socketInteractable.isInteractableActive = true;
        PlayDialogue(player, returnDialogue);
    }

    private WoodBlockWallVariant FindVariant(ItemType itemType)
    {
        if (blockVariants == null)
            return null;

        foreach (WoodBlockWallVariant variant in blockVariants)
        {
            if (variant != null && variant.itemType == itemType)
                return variant;
        }

        return null;
    }

    private void SetMountedVisual(WoodBlockWallVariant mounted, bool isMounted)
    {
        if (blockVariants == null)
            return;

        foreach (WoodBlockWallVariant variant in blockVariants)
        {
            if (variant == null)
                continue;

            bool active = isMounted && variant == mounted;
            if (variant.wallEquivalent != null)
                variant.wallEquivalent.SetActive(active);

            SetObjectsActive(variant.activateWhileMounted, active);
            SetObjectsActive(variant.deactivateWhileMounted, !active);
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject current in objects)
        {
            if (current != null)
                current.SetActive(active);
        }
    }

    // ---------------------------------------------------------------
    // SYSTEM ZAPISU
    // ---------------------------------------------------------------

    // Indeks wlozonego klocka w tablicy blockVariants, albo -1 gdy gniazdo puste.
    // 'mountedVariant' to referencja, wiec nie przezywa wczytania - bez tego
    // gniazdo zostawalo martwe: nieaktywne do interakcji i bez mozliwosci
    // odebrania klocka.
    public int MountedVariantIndex
    {
        get
        {
            if (mountedVariant == null || blockVariants == null)
                return -1;

            for (int i = 0; i < blockVariants.Length; i++)
            {
                if (blockVariants[i] == mountedVariant)
                    return i;
            }

            return -1;
        }
    }

    public void RestoreMountedVariant(int index)
    {
        if (socketInteractable == null)
            socketInteractable = GetComponent<Interactable>();

        if (index < 0 || blockVariants == null || index >= blockVariants.Length)
        {
            // Gniazdo puste - klocek mozna wlozyc.
            mountedVariant = null;
            SetMountedVisual(null, false);

            if (socketInteractable != null)
                socketInteractable.isInteractableActive = true;

            return;
        }

        mountedVariant = blockVariants[index];
        SetMountedVisual(mountedVariant, true);

        if (socketInteractable != null)
            socketInteractable.isInteractableActive = false;

        Debug.Log("Int_lv2_WoodBrickWall: przywrocono wlozony klocek '" +
                  mountedVariant.itemType + "' w gniezdzie '" + name + "'.", this);
    }
}