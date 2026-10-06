using System.Collections;
using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;

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

        // Zapamietujemy podswietlenie i zamknieta rotacje drzwi.
        // PerformInteraction kasuje referencje do shadera (ustawia null),
        // a to nieodwracalne - bez kopii po wczytaniu zapisu nie da sie
        // juz kliknac klapy.
        oryginalnyShader = interactable != null ? interactable.interactiveShader : null;

        if (door != null)
            zamknietaRotacja = door.transform.localRotation;

        transform.parent = door.transform;


    }

    // --- SYSTEM ZAPISU ---

    private GameObject oryginalnyShader;
    private Quaternion zamknietaRotacja;
    private bool zainicjalizowano;

    private void Inicjalizuj()
    {
        if (zainicjalizowano)
            return;

        if (interactable == null)
            interactable = GetComponent<Interactable>();

        if (openRotation == default)
            openRotation = Quaternion.Euler(openEuler);

        if (oryginalnyShader == null && interactable != null)
            oryginalnyShader = interactable.interactiveShader;

        if (door != null && zamknietaRotacja == default)
            zamknietaRotacja = door.transform.localRotation;

        zainicjalizowano = true;
    }

    public bool IsOpen => isOpen;

    // Odtwarza stan klapy po wczytaniu zapisu.
    // LoadGame nie przeladowuje sceny, wiec bez tego klapa zostaje
    // w stanie z biezacej sesji: bez podswietlenia i z wylaczona interakcja.
    public void RestoreHatchState(bool otwarta)
    {
        // Klapa lezy w LEVEL_2, ktory przy starcie sceny jest wylaczony.
        // Jej Start() uruchamia sie dopiero po wlaczeniu poziomu przez zapis,
        // czyli PO LoadGame - dlatego inicjalizujemy sie tu sami.
        Inicjalizuj();

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }

        isOpen = otwarta;
        performed = otwarta;

        if (door != null)
            door.transform.localRotation = otwarta ? openRotation : zamknietaRotacja;

        if (interactable == null)
        {
            Debug.LogWarning("lvl2_Int_Hatch: brak komponentu Interactable na '" + name +
                             "' - nie moge przywrocic stanu klapy.", this);
            return;
        }

        interactable.isInteractableActive = !otwarta;
        interactable.interactiveShader = otwarta ? null : oryginalnyShader;

        Debug.Log("lvl2_Int_Hatch: przywrocono klape '" + name +
                  "' jako " + (otwarta ? "otwarta" : "zamknieta") + ".", this);
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");

        if (!isOpen && openCoroutine == null)
        {
            //interactable.AddClue(0);

            interactable.interactiveShader = null;
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