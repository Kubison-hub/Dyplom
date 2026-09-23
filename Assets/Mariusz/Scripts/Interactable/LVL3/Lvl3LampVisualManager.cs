using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;

/// <summary>
/// Keeps the identity of the two basement lamps while using one held-lamp visual for each character.
/// </summary>
public class Lvl3LampVisualManager : MonoBehaviour
{
    public static Lvl3LampVisualManager Instance { get; private set; }

    [Header("Held Lamp Visuals")]
    [Tooltip("Inactive visual prefab instance under Sherlock's LampHolder.")]
    [SerializeField] private GameObject sherlockHeldLampVisual;
    [Tooltip("Inactive visual prefab instance under Watson's LampHolder.")]
    [SerializeField] private GameObject watsonHeldLampVisual;

    [Header("Held Lamp Lights")]
    [Tooltip("Prepared GameObject with the Point Light used while Sherlock carries a lamp.")]
    [SerializeField] private GameObject sherlockHeldLampLight;
    [Tooltip("Prepared GameObject with the Point Light used while Watson carries a lamp.")]
    [SerializeField] private GameObject watsonHeldLampLight;

    private GameObject sherlockCarriedLamp;
    private GameObject watsonCarriedLamp;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    private void Start()
    {
        SetHeldVisualActive(false, false);
        SetHeldVisualActive(true, false);
        SetHeldLampLightActive(false, false);
        SetHeldLampLightActive(true, false);
    }

    public bool IsPlayerHoldingLamp(PlayerController player)
    {
        return IsWatson(player) ? watsonCarriedLamp != null : sherlockCarriedLamp != null;
    }

    public GameObject GetCarriedLamp(PlayerController player)
    {
        return IsWatson(player) ? watsonCarriedLamp : sherlockCarriedLamp;
    }

    public bool TryCarryLamp(GameObject lamp, PlayerController player)
    {
        if (lamp == null || player == null || IsPlayerHoldingLamp(player) || IsLampCarried(lamp))
            return false;

        bool isWatson = IsWatson(player);
        if (isWatson)
            watsonCarriedLamp = lamp;
        else
            sherlockCarriedLamp = lamp;

        lamp.SetActive(false);
        SetHeldVisualActive(isWatson, true);
        SetHeldLampLightActive(isWatson, true);
        return true;
    }

    public bool TryMountCarriedLamp(
        PlayerController player,
        Transform wallLampSocket,
        Vector3 localPosition,
        Vector3 localEulerAngles)
    {
        if (player == null || wallLampSocket == null)
            return false;

        bool isWatson = IsWatson(player);
        GameObject carriedLamp = isWatson ? watsonCarriedLamp : sherlockCarriedLamp;
        if (carriedLamp == null)
            return false;

        if (isWatson)
            watsonCarriedLamp = null;
        else
            sherlockCarriedLamp = null;

        SetHeldVisualActive(isWatson, false);
        SetHeldLampLightActive(isWatson, false);
        carriedLamp.transform.SetParent(wallLampSocket, false);
        carriedLamp.transform.localPosition = localPosition;
        carriedLamp.transform.localRotation = Quaternion.Euler(localEulerAngles);
        carriedLamp.SetActive(true);
        return true;
    }

    private bool IsLampCarried(GameObject lamp)
    {
        return lamp != null && (lamp == sherlockCarriedLamp || lamp == watsonCarriedLamp);
    }

    private void SetHeldVisualActive(bool isWatson, bool isActive)
    {
        GameObject heldVisual = isWatson ? watsonHeldLampVisual : sherlockHeldLampVisual;
        if (heldVisual != null && heldVisual.activeSelf != isActive)
            heldVisual.SetActive(isActive);
    }

    private void SetHeldLampLightActive(bool isWatson, bool isActive)
    {
        GameObject heldLampLight = isWatson ? watsonHeldLampLight : sherlockHeldLampLight;
        if (heldLampLight != null && heldLampLight.activeSelf != isActive)
            heldLampLight.SetActive(isActive);
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
    }

    // ---------------------------------------------------------------
    // SYSTEM ZAPISU - niesione lampy
    // ---------------------------------------------------------------

    // Zwraca obiekt lampy niesionej przez wskazana postac (null = nie niesie).
    public GameObject GetCarriedLampForSave(bool isWatson)
    {
        return isWatson ? watsonCarriedLamp : sherlockCarriedLamp;
    }

    // Przywraca stan niesienia lampy. Start() bezwarunkowo gasi wizualizacje
    // i swiatlo, wiec po wczytaniu zapisu trzeba je wlaczyc z powrotem -
    // inaczej piwnica zostaje calkowicie ciemna.
    public void RestoreCarriedLamp(bool isWatson, GameObject lamp)
    {
        if (isWatson)
            watsonCarriedLamp = lamp;
        else
            sherlockCarriedLamp = lamp;

        bool niesie = lamp != null;

        if (niesie)
            lamp.SetActive(false);

        SetHeldVisualActive(isWatson, niesie);
        SetHeldLampLightActive(isWatson, niesie);

        Debug.Log("Lvl3LampVisualManager: " + (isWatson ? "Watson" : "Sherlock") +
                  (niesie ? " niesie lampe '" + lamp.name + "'." : " nie niesie lampy."));
    }
}