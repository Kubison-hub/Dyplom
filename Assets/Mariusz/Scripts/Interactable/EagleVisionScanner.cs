using System.Collections.Generic;
using UnityEngine;


public class EagleVisionScanner : MonoBehaviour
{
    public static EagleVisionScanner Instance;

    public Transform playerTransform;

    public Material[] footprintMaterials;
    public Material footprintMaterial;
    public float radius = 5.0f;
    public float bacgroundTreshold = 10f;
    private ParticleSystem scannerPS;
    private SphereCollider scannerCollider;

    public float duration = 5;
    public float size = 30;
    public float scannerSpeed = 15;

    public bool isScanning = false;
    private float timer = 0f;

    private float currentProgress = 0f;

    public bool footPrints;

    public List<GameObject> footprintsSplines = new List<GameObject>();



    private void Start()
    {
        Instance = this;

        scannerPS = GetComponentInChildren<ParticleSystem>();
        scannerCollider = GetComponent<SphereCollider>();

        scannerCollider.enabled = false;
    }

    private void Update()
    {
        FindshaderFootPrints();
    }


    public void ScanSherlock(bool isActive)
    {
        if (scannerPS == null) return;

        isScanning = isActive;
        scannerCollider.enabled = isScanning; 

        if (isActive)
        {
            var main = scannerPS.main;
            main.startLifetime = duration;
            main.startSize = size;

            scannerPS.Play();

            if (footPrints)
            {
                foreach (var footprint in footprintsSplines) 
                footprint.SetActive(true);
            }

        }
        else
        {
            // Przy wy³¹czaniu pozwalamy Update zaj¹æ siê "zwijaniem" promienia
            // Jeœli chcesz, by cz¹steczki zniknê³y natychmiast:
            Debug.Log("else");
            scannerPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            foreach (var footprint in footprintsSplines)
                footprint.SetActive(false);
            
        }

        Debug.Log($"Scanner State: {isActive}");
    }



   



    private void FindshaderFootPrints()

    {
        //float target = isScanning ? 1f : 0f;
        //currentProgress = Mathf.MoveTowards(currentProgress, target, Time.deltaTime / duration);

        //if (scannerCollider != null)
        //{
        //    scannerCollider.radius = (currentProgress * scannerSpeed) / 2f;

        //    if (currentProgress <= 0f && !isScanning)
        //        scannerCollider.enabled = false;
        //}

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

    //FIX
}