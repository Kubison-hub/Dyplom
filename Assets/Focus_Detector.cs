using System.Diagnostics.Contracts;
using System.Xml.Schema;
using UnityEngine;

public class Focus_Detector : MonoBehaviour
{

    public bool performed = false;

    private Coroutine fadeCoroutine;
    private bool isFaded = false;
    [SerializeField] private float fadeSpeed = 120f;

    [SerializeField] AudioSource audioFX;

    
    public LayerMask targetLayerMask;
    MeshRenderer meshRenderer;

    private void Start()
    {
      

    }

    public void ChangeLayer()
    {
        
        var newLayer = LayerMask.NameToLayer("Hiden");
        var obj = this.gameObject;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

}
