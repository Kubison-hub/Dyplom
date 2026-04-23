using UnityEngine;
using UnityEngine.UIElements;

public class EagleVisionScanner : MonoBehaviour
{

    public Transform playerTransform;

    public Material[] footprintMaterials;
    public float radius = 5.0f;
    public float bacgroundTreshold = 10f;
    private ParticleSystem scannerPS;
    private SphereCollider scannerCollider;

    public float duration = 5;
    public float size = 30;
    public float scannerSpeed = 15;

    private bool isScanning = false;
    private float timer = 0f;

    private float currentProgress = 0f;

    public bool footPrints;

    public GameObject footprintsSlines;
    

    private void Start()
    {
        scannerPS = GetComponentInChildren<ParticleSystem>();
        scannerCollider = GetComponentInChildren<SphereCollider>();

        if (scannerCollider == null && scannerPS != null)
        {
            scannerCollider = scannerPS.gameObject.AddComponent<SphereCollider>();
        }

        if (scannerCollider != null)
        {
            scannerCollider.isTrigger = true;
            scannerCollider.enabled = false;
        }
    }

    public void ScanSherlock(bool isActive)
    {
        if (scannerPS == null) return;

        isScanning = isActive;
        scannerCollider.enabled = true; 

        if (isActive)
        {
            var main = scannerPS.main;
            main.startLifetime = duration;
            main.startSize = size;

            scannerPS.Play();

            if (footPrints)
            {
                footprintsSlines.SetActive(true);
            }

        }
        else
        {
            // Przy wy³¹czaniu pozwalamy Update zaj¹æ siê "zwijaniem" promienia
            // Jeœli chcesz, by cz¹steczki zniknê³y natychmiast:
            Debug.Log("else");
            scannerPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            footprintsSlines.SetActive(false);
        }

        Debug.Log($"Scanner State: {isActive}");
    }

   

    private void Update()
    {
        float target = isScanning ? 1f : 0f;
        currentProgress = Mathf.MoveTowards(currentProgress, target, Time.deltaTime / duration);

        if (scannerCollider != null)
        {
            scannerCollider.radius = (currentProgress * scannerSpeed) / 2f;

            if (currentProgress <= 0f && !isScanning)
                scannerCollider.enabled = false;
        }

        // Opcjonalnie: Mo¿esz tu te¿ sterowaæ skal¹ obiektu z cz¹steczkami, 
        // jeœli chcesz, by wizualnie te¿ siê "kurczy³" zamiast tylko znikaæ.
        // scannerPS.transform.localScale = Vector3.one * currentProgress;

        if (playerTransform != null && footprintMaterials != null && footprintMaterials.Length > 0)
        {
            Vector3 pos = playerTransform.position;

            Material background = footprintMaterials[0];
            if (background != null)
            {
                background.SetVector("_PlayerPosition", pos);
                background.SetFloat("_VisibleRadius", radius + bacgroundTreshold);
            }

            for (int i = 1; i < footprintMaterials.Length; i++)
            {
                if (footprintMaterials[i] != null)
                {
                    footprintMaterials[i].SetVector("_PlayerPosition", pos);
                    footprintMaterials[i].SetFloat("_VisibleRadius", radius);
                }
            }


        }



    }
}