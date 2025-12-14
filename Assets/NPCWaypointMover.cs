using UnityEngine;

public class NPCWaypointMover : MonoBehaviour
{
    [Header("Œcie¿ka")]
    [Tooltip("Przeci¹gnij tutaj puste obiekty (punkty), przez które NPC ma przejœæ")]
    public Transform[] punktySciezki;

    [Header("Ustawienia")]
    public float predkosc = 2.0f;
    public float predkoscObrotu = 5.0f;

    [Header("Animacja")]
    public Animator animator;
    public string nazwaBoolaChodzenia = "isWalking"; // SprawdŸ w Animatorze czy tak siê nazywa parametr

    // Zmienne wewnêtrzne
    private int obecnyIndexPunktu = 0;
    private bool czyMaIsc = false;

    // Tê funkcjê wywo³a Gramofon
    public void ZacznijWedrowke()
    {
        if (punktySciezki.Length > 0)
        {
            obecnyIndexPunktu = 0; // Zaczynamy od pierwszego punktu
            czyMaIsc = true;

            // W³¹czamy animacjê chodzenia
            if (animator != null) animator.SetBool(nazwaBoolaChodzenia, true);
        }
        else
        {
            Debug.LogError("NPCWaypointMover: Nie przypisano ¿adnych punktów œcie¿ki!");
        }
    }

    private void Update()
    {
        if (!czyMaIsc) return;

        // Jeœli doszliœmy do koñca listy punktów
        if (obecnyIndexPunktu >= punktySciezki.Length)
        {
            KoniecTrasy();
            return;
        }

        MoveToTarget();
    }

    private void MoveToTarget()
    {
        Transform cel = punktySciezki[obecnyIndexPunktu];

        // 1. RUCH: Przesuwamy siê w stronê celu (Vector3.MoveTowards to proste przesuwanie "po linii")
        // Ignorujemy oœ Y celu, ¿eby NPC nie wylatywa³ w powietrze ani nie zapada³ siê w ziemiê
        Vector3 celPozycja = new Vector3(cel.position.x, transform.position.y, cel.position.z);

        transform.position = Vector3.MoveTowards(transform.position, celPozycja, predkosc * Time.deltaTime);

        // 2. OBROT: Patrzymy w stronê celu
        Vector3 kierunek = (celPozycja - transform.position).normalized;
        if (kierunek != Vector3.zero)
        {
            Quaternion celObrot = Quaternion.LookRotation(kierunek);
            transform.rotation = Quaternion.Slerp(transform.rotation, celObrot, predkoscObrotu * Time.deltaTime);
        }

        // 3. SPRAWDZENIE: Czy jesteœmy ju¿ blisko punktu? (np. 10cm)
        if (Vector3.Distance(transform.position, celPozycja) < 0.1f)
        {
            obecnyIndexPunktu++; // Prze³¹czamy na kolejny punkt
        }
    }

    private void KoniecTrasy()
    {
        czyMaIsc = false;
        if (animator != null) animator.SetBool(nazwaBoolaChodzenia, false);
        Debug.Log("NPC dotar³ do celu!");
    }
}