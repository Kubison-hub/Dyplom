using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_WallLampHolder : Lvl3ClockworkInteraction, IInteractionApproachGate
{
    [Header("Lamp References")]
    [Tooltip("First lamp that may be mounted on this holder.")]
    [FormerlySerializedAs("heldLamp")]
    [SerializeField] private GameObject heldLamp1;
    [Tooltip("Second lamp that may be mounted on this holder.")]
    [SerializeField] private GameObject heldLamp2;
    [Tooltip("LampHolder on Sherlock. This confirms that Sherlock is currently carrying the lamp.")]
    [SerializeField] private Transform sherlockLampHolder;
    [Tooltip("LampHolder on Watson. This confirms that Watson is currently carrying the lamp.")]
    [SerializeField] private Transform watsonLampHolder;
    [Tooltip("The wall socket where the lamp should be placed.")]
    [SerializeField] private Transform wallLampSocket;
    [SerializeField] private Vector3 wallLampLocalPosition;
    [SerializeField] private Vector3 wallLampLocalEulerAngles;
    [SerializeField] private Vector3 carriedLampLocalPosition;
    [SerializeField] private Vector3 carriedLampLocalEulerAngles;

    [Header("Mounted Lamp Light")]
    [Tooltip("Additional light object enabled only while a lamp is mounted in this wall holder.")]
    [SerializeField] private GameObject mountedLampLight;

    [Header("Lamp Audio")]
    [Tooltip("Optional source used when this holder picks up or places a lamp.")]
    [SerializeField] private AudioSource lampAudioSource;
    [SerializeField] private AudioClip pickupLampAudio;
    [SerializeField] private AudioClip placeLampAudio;

    [Header("Dialogue")]
    [SerializeField, TextArea] private string missingLampText = "Najpierw muszę znaleźć lampę.";
    [SerializeField, TextArea] private string placedLampText = "Tak będzie lepiej. Teraz mamy trochę światła.";
    [SerializeField, TextArea] private string pickedUpLampText = "Zabiorę lampę ze sobą.";
    [SerializeField, TextArea] private string alreadyHoldingLampText = "Nie mogę nieść dwóch lamp.";

    private void Start()
    {
        RefreshMountedLampLight();
    }

    public override bool CanPlayerUse(PlayerController player)
    {
        if (player == null)
            return false;

        return base.CanPlayerUse(player);
    }

    public bool CanApproachInteraction(PlayerController player)
    {
        return GetMountedLamp() != null || GetCarriedLamp(player) != null;
    }

    public void ShowApproachBlockedText(PlayerController player)
    {
        if (player == null || Interactable == null)
            return;

        player.RotateTowardsInteractableAndShowTopText(Interactable, missingLampText);
    }

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        GameObject mountedLamp = GetMountedLamp();
        if (mountedLamp != null)
        {
            PickUpLamp(player, mountedLamp);
            return;
        }

        GameObject carriedLamp = GetCarriedLamp(player);
        if (carriedLamp == null)
        {
            ShowTopTextForPlayer(player, missingLampText);
            return;
        }

        if (wallLampSocket == null)
        {
            Debug.LogWarning($"{name}: Assign Wall Lamp Socket before using the wall lamp holder.", this);
            return;
        }

        carriedLamp.transform.SetParent(wallLampSocket, false);
        carriedLamp.transform.localPosition = wallLampLocalPosition;
        carriedLamp.transform.localRotation = Quaternion.Euler(wallLampLocalEulerAngles);
        carriedLamp.SetActive(true);
        RefreshMountedLampLight();

        PlayLampAudio(placeLampAudio);

        ShowTopTextForPlayer(player, placedLampText);
    }

    private void PickUpLamp(PlayerController player, GameObject mountedLamp)
    {
        Transform targetLampHolder = GetLampHolder(player);
        if (mountedLamp == null || targetLampHolder == null)
        {
            Debug.LogWarning($"{name}: Assign Held Lamp and a Lamp Holder before picking up the wall lamp.", this);
            return;
        }

        if (HasActiveHeldLamp(targetLampHolder))
        {
            ShowTopTextForPlayer(player, alreadyHoldingLampText);
            return;
        }

        mountedLamp.transform.SetParent(targetLampHolder, false);
        mountedLamp.transform.localPosition = carriedLampLocalPosition;
        mountedLamp.transform.localRotation = Quaternion.Euler(carriedLampLocalEulerAngles);
        mountedLamp.SetActive(true);
        RefreshMountedLampLight();

        PlayLampAudio(pickupLampAudio);

        ShowTopTextForPlayer(player, pickedUpLampText);
    }

    private GameObject GetCarriedLamp(PlayerController player)
    {
        Transform currentPlayerLampHolder = GetLampHolder(player);
        if (currentPlayerLampHolder == null)
            return null;

        if (IsLampCarriedByHolder(heldLamp1, currentPlayerLampHolder))
            return heldLamp1;

        if (IsLampCarriedByHolder(heldLamp2, currentPlayerLampHolder))
            return heldLamp2;

        return null;
    }

    private Transform GetLampHolder(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
        return isWatson && watsonLampHolder != null ? watsonLampHolder : sherlockLampHolder;
    }

    private static bool HasActiveHeldLamp(Transform lampHolder)
    {
        if (lampHolder == null)
            return false;

        foreach (Transform child in lampHolder)
        {
            if (child.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private static bool IsLampCarriedByHolder(GameObject lamp, Transform lampHolder)
    {
        if (lamp == null || lampHolder == null || !lamp.activeInHierarchy)
            return false;

        return lamp.transform.IsChildOf(lampHolder);
    }

    private GameObject GetMountedLamp()
    {
        if (IsLampMountedOnWall(heldLamp1))
            return heldLamp1;

        if (IsLampMountedOnWall(heldLamp2))
            return heldLamp2;

        return null;
    }

    private bool IsLampMountedOnWall(GameObject lamp)
    {
        return lamp != null &&
               wallLampSocket != null &&
               lamp.activeInHierarchy &&
               lamp.transform.IsChildOf(wallLampSocket);
    }

    private void RefreshMountedLampLight()
    {
        if (mountedLampLight != null)
            mountedLampLight.SetActive(GetMountedLamp() != null);
    }

    private void PlayLampAudio(AudioClip clip)
    {
        if (lampAudioSource != null && clip != null)
            lampAudioSource.PlayOneShot(clip);
    }
}
