using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LookAtCamera : MonoBehaviour
{
    private Transform cam;
    private Material mat;

    void Start()
    {
        cam = Camera.main.transform;

        // Wymuszenie renderowania zawsze na wierzchu
        Renderer renderer = GetComponent<Renderer>();
        mat = renderer.material; // Uwaga: to tworzy kopiê materia³u!

        if (mat != null)
        {
            mat.renderQueue = 4000;       // Overlay
            mat.SetInt("_ZTest", 8);      // 8 = Always (zawsze rysuj, nawet przez inne obiekty)
        }
    }

    void LateUpdate()
    {
        // Billboard — patrz w stronê kamery bez obracania siê "do góry nogami"
        transform.LookAt(transform.position + cam.forward, cam.up);
    }
}
