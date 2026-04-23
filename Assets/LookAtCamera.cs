using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LookAtCamera : MonoBehaviour
{
    private Transform cam;
    private Material mat;

    [Header("Sta³y rozmiar na ekranie")]
    [SerializeField] private float scaleMultiplier = 0.01f;
    [SerializeField] private float referenceDistance = 10f;
    [SerializeField] private bool keepConstantSize = true;

    void Start()
    {
        cam = Camera.main.transform;

        Renderer renderer = GetComponent<Renderer>();
        mat = renderer.material;

        if (mat != null)
        {
            mat.renderQueue = 4000;
            mat.SetInt("_ZTest", 8);
        }
    }

    void LateUpdate()
    {
        if (cam == null)
            return;

        
        transform.LookAt(transform.position + cam.forward, cam.up);

   
        if (keepConstantSize)
        {
            float distance = Vector3.Distance(transform.position, cam.position);
            float scale = scaleMultiplier * (distance / referenceDistance);
            transform.localScale = Vector3.one * scale;
        }
    }
}