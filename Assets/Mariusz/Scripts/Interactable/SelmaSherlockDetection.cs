using UnityEngine;

public class SelmaSherlockDetection : MonoBehaviour
{
    private Int2_WatsonScan watsonScan;
    private Collider detectionCollider;

    private bool lastDistractionState;

    private void Start()
    {
        watsonScan = GetComponentInParent<Int2_WatsonScan>();
        detectionCollider = GetComponent<Collider>();

        if (detectionCollider != null)
            detectionCollider.enabled = false;
    }

    private void Update()
    {
        if (watsonScan == null || detectionCollider == null)
            return;

        if (watsonScan.isOnDistraction != lastDistractionState)
        {
            detectionCollider.enabled = watsonScan.isOnDistraction;
            lastDistractionState = watsonScan.isOnDistraction;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerA"))
            return;

        if (watsonScan == null || !watsonScan.isOnDistraction)
            return;

        watsonScan.OnSherlockDetected();
    }
}