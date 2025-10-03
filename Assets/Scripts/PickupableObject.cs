using UnityEngine;

public class PickupableObject : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float holdHeight = 2f;  // wysokość nad graczem podczas podnoszenia

    [Header("Highlight")]
    public Material highlightMaterial;  // przypisz w Inspectorze żółty lub inny glow

    private Transform holder = null;    // kto trzyma przedmiot
    private bool isHeld = false;

    private Renderer[] renderers;
    private Material[] originalMaterials;

    // PUBLICZNE WŁAŚCIWOŚCI
    public bool IsCarried => isHeld;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }

    private void Update()
    {
        // jeśli ktoś trzyma przedmiot, podążaj za nim
        if (isHeld && holder != null)
        {
            UpdatePosition();
        }
    }

    // ------------------- PODNOSZENIE I UPUSZCZANIE -------------------

    public void Pickup(Transform player)
    {
        if (isHeld) return; // jeśli ktoś już trzyma, nic nie rób

        holder = player;
        isHeld = true;
    }

    public void Drop()
    {
        if (!isHeld || holder == null) return;

        // upuszczamy przedmiot przed graczem
        Vector3 dropPos = holder.position + holder.forward * 1.5f;
        transform.position = dropPos;

        holder = null;
        isHeld = false;
    }

    public void UpdatePosition()
    {
        if (holder != null)
        {
            transform.position = holder.position + Vector3.up * holdHeight;
        }
    }

    // ------------------- PODŚWIETLENIE -------------------

    public void Highlight(bool on)
    {
        // jeśli nie przypisano materiału highlight, nie zmieniaj nic
        if (highlightMaterial == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = on ? highlightMaterial : originalMaterials[i];
        }
    }
}
