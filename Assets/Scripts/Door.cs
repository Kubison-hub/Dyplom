using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Ustawienia otwierania")]
    [Tooltip("O ile stopni obróciæ drzwi na osi Y.")]
    [SerializeField] private float openAngleY = 90.0f;

    [Tooltip("Jak szybko drzwi maj¹ siê otwieraæ/zamykaæ.")]
    [SerializeField] private float openSpeed = 2.0f;

    [Tooltip("Czas w sekundach, po którym drzwi same siê zamkn¹.")]
    [SerializeField] private float autoCloseDelay = 10.0f;

    private bool isBusy = false; // Flaga: czy drzwi s¹ w trakcie cyklu (ruch lub czekanie)
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        // Zapamiêtaj pozycjê zamkniêt¹ (lokaln¹!)
        closedRotation = transform.localRotation;
        // Oblicz pozycjê otwart¹
        openRotation = closedRotation * Quaternion.Euler(0, openAngleY, 0);
    }

    public void OpenDoor()
    {
        // Jeœli drzwi ju¿ coœ robi¹ (otwieraj¹ siê, czekaj¹ lub zamykaj¹), ignoruj klikniêcie
        if (isBusy) return;

        // Uruchom pe³ny cykl
        StartCoroutine(DoorCycle());
    }

    // G³ówna korutyna zarz¹dzaj¹ca ca³ym cyklem ¿ycia drzwi
    private IEnumerator DoorCycle()
    {
        isBusy = true;

        // 1. Otwieranie (czekaj a¿ skoñczy siê ruszaæ)
        yield return StartCoroutine(MoveDoor(closedRotation, openRotation));

        // 2. Czekanie (odliczanie czasu)
        Debug.Log($"Drzwi otwarte. Czekam {autoCloseDelay} sekund...");
        yield return new WaitForSeconds(autoCloseDelay);

        // 3. Zamykanie (czekaj a¿ skoñczy siê ruszaæ)
        Debug.Log("Zamykanie drzwi...");
        yield return StartCoroutine(MoveDoor(openRotation, closedRotation));

        // Koniec cyklu - drzwi s¹ gotowe do ponownego u¿ycia
        isBusy = false;
        Debug.Log("Drzwi zamkniête.");
    }

    // Generyczna korutyna do ruchu (u¿ywana i do otwierania, i do zamykania)
    private IEnumerator MoveDoor(Quaternion from, Quaternion to)
    {
        float time = 0;
        while (time < 1)
        {
            transform.localRotation = Quaternion.Slerp(from, to, time);
            time += Time.deltaTime / openSpeed;
            yield return null;
        }
        // Upewnij siê, ¿e rotacja jest idealna na koniec ruchu
        transform.localRotation = to;
    }
}