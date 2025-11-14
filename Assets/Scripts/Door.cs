using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Animacja")]
    [SerializeField] private float openAngleY = 90.0f;
    [SerializeField] private float openSpeed = 2.0f;
    [SerializeField] private float autoCloseDelay = 10.0f;

    private bool isBusy = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        // Zapamiêtaj rotacje
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngleY, 0);
    }

    // Tê funkcjê wywo³a DŸwignia
    public void OpenDoor()
    {
        if (isBusy) return; // Jeœli drzwi ju¿ pracuj¹, ignoruj
        StartCoroutine(DoorCycle());
    }

    private IEnumerator DoorCycle()
    {
        isBusy = true;

        // 1. Otwórz
        yield return StartCoroutine(MoveDoor(closedRotation, openRotation));

        // 2. Czekaj
        if (autoCloseDelay > 0)
        {
            yield return new WaitForSeconds(autoCloseDelay);

            // 3. Zamknij
            yield return StartCoroutine(MoveDoor(openRotation, closedRotation));
        }

        isBusy = false;
    }

    private IEnumerator MoveDoor(Quaternion from, Quaternion to)
    {
        float time = 0;
        while (time < 1)
        {
            transform.localRotation = Quaternion.Slerp(from, to, time);
            time += Time.deltaTime / openSpeed;
            yield return null;
        }
        transform.localRotation = to;
    }
}