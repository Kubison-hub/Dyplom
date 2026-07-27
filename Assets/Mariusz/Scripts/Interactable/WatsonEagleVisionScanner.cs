using UnityEngine;

public class WatsonEagleVisionScanner : MonoBehaviour
{
    public static WatsonEagleVisionScanner Instance;

    public Transform playerTransform;
    public GameObject watsonScanRangeIndicator;

    public bool isScanning = false;
    private Collider scanCollider;

    public Transform movePoint;
    private void Start()
    {
        Instance = this;

        watsonScanRangeIndicator.SetActive(false);
        scanCollider = GetComponent<Collider>();
        scanCollider.enabled = false;

        if (movePoint == null)
            Debug.LogError("MovePoint is null");
    }

    public void ScanWatson(bool isActive)
    {
        isScanning = isActive;

        if (isActive)
        {
            watsonScanRangeIndicator.SetActive(true);
            scanCollider.enabled = true;
        }
        else
        {
            watsonScanRangeIndicator.SetActive(false);
            scanCollider.enabled = false;
            Debug.Log("Exit");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WatsonScan"))
        {
            Int2_WatsonScan watsonScan = other.GetComponent<Int2_WatsonScan>();
            if (watsonScan != null)
            {
                watsonScan.canInteract = true;
            }
            else
            {
                Debug.LogError("WatsonScan is Null");
            }
                
        }
       
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("WatsonScan"))
            return;

        Int2_WatsonScan watsonScan = other.GetComponent<Int2_WatsonScan>();
        if (watsonScan != null)
        {
            watsonScan.canInteract = false;
        }
        else
        {
            Debug.LogError("WatsonScan is Null");
        }
    }
}
