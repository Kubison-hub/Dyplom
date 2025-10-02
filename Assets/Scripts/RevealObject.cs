using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RevealObject : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Material highlightMaterial; // glowing yellow material

    private Material[] originalMaterials;
    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterials = rend.materials; // cache multiple materials
        }
    }

    public void ApplyHighlight()
    {
        if (rend != null && highlightMaterial != null)
        {
            // Combine original + highlight overlay
            Material[] mats = new Material[originalMaterials.Length + 1];
            for (int i = 0; i < originalMaterials.Length; i++)
                mats[i] = originalMaterials[i];
            mats[mats.Length - 1] = highlightMaterial;

            rend.materials = mats;
        }
    }

    public void RemoveHighlight()
    {
        if (rend != null && originalMaterials != null)
        {
            rend.materials = originalMaterials;
        }
    }
}
