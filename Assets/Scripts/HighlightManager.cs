using UnityEngine;
using System.Collections.Generic;

public class HighlightManager : MonoBehaviour
{
    [Header("Reveal Settings")]
    public KeyCode revealKey = KeyCode.V;
    public float darkenIntensity = 0.6f; // how dark the scene gets (0 = normal, 1 = pitch black)

    private RevealObject[] revealObjects;
    private bool isRevealing = false;

    [Header("Scene Darkening")]
    private Material darkenMaterial;
    private bool darkenEnabled = false;

    private void Start()
    {
        // Cache all revealable objects in scene
        revealObjects = FindObjectsOfType<RevealObject>();

        // Create darkening fullscreen material
        Shader shader = Shader.Find("Hidden/DarkenOverlay");
        if (shader != null)
        {
            darkenMaterial = new Material(shader);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(revealKey))
        {
            StartReveal();
        }
        else if (Input.GetKeyUp(revealKey))
        {
            EndReveal();
        }
    }

    private void StartReveal()
    {
        if (isRevealing) return;
        isRevealing = true;

        foreach (var obj in revealObjects)
        {
            obj.ApplyHighlight();
        }

        darkenEnabled = true;
    }

    private void EndReveal()
    {
        if (!isRevealing) return;
        isRevealing = false;

        foreach (var obj in revealObjects)
        {
            obj.RemoveHighlight();
        }

        darkenEnabled = false;
    }

    // Post-process overlay to darken scene
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (darkenEnabled && darkenMaterial != null)
        {
            darkenMaterial.SetFloat("_Intensity", darkenIntensity);
            Graphics.Blit(src, dest, darkenMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
