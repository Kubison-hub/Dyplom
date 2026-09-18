using UnityEngine;

[System.Obsolete("Legacy scanner kept only for compatibility with archived scenes.")]
public class WatsonEagleVisionScanner : MonoBehaviour
{
    public static WatsonEagleVisionScanner Instance;

    public Transform playerTransform;
    public GameObject watsonScanRangeIndicator;
    public bool isScanning;
    public Transform movePoint;

    private Collider scanCollider;

    private void Awake()
    {
        Instance = this;
        if (watsonScanRangeIndicator != null)
            watsonScanRangeIndicator.SetActive(false);

        scanCollider = GetComponent<Collider>();
        if (scanCollider != null)
            scanCollider.enabled = false;
    }

    public void ScanWatson(bool active)
    {
        isScanning = active;
        if (watsonScanRangeIndicator != null)
            watsonScanRangeIndicator.SetActive(active);
        if (scanCollider != null)
            scanCollider.enabled = active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WatsonScan") && other.TryGetComponent(out Int2_WatsonScan watsonScan))
            watsonScan.canInteract = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WatsonScan") && other.TryGetComponent(out Int2_WatsonScan watsonScan))
            watsonScan.canInteract = false;
    }
}
