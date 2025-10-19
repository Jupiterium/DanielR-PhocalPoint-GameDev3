using UnityEngine;

 /*
 * This script manages the persistent, internal state (Ghost/Normal) of an interactable object.
 * This component are to be added to the objects that are to be modified.
 */

public class SuperliminalObject : MonoBehaviour
{
    // Persistent State
    private Rigidbody rb;
    private Renderer objRenderer;

    // Ghost State
    [Header("Ghost State")]
    public bool IsGhost = false; // Persistent flag to track the object's state

    // Material Caching
    private Material originalMaterialInstance;
    private Color originalColor;

    // Capture the original state before getting affected 
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objRenderer = GetComponent<Renderer>();

        if (rb == null || objRenderer == null)
        {
            Debug.LogError($"Interactable object {gameObject.name} is missing a Rigidbody or Renderer. Disabling SuperliminalObject component.");
            enabled = false;
            return;
        }

        // Capture original state
        // Use 'objRenderer.material' to get a unique instance that can be modified safely.
        originalMaterialInstance = objRenderer.material;

        // Get original color based on available property
        if (originalMaterialInstance.HasProperty("_BaseColor"))
        {
            originalColor = originalMaterialInstance.GetColor("_BaseColor");
        }
        else if (originalMaterialInstance.HasProperty("_Color"))
        {
            originalColor = originalMaterialInstance.GetColor("_Color");
        }
    }

    // Toggles the object's permanent state between Ghost (Transparent/Zero-G) and Normal.
    // Called by Superliminal.cs upon 'T' key press or 'Shutter Capture'.
    public void ToggleState()
    {
        IsGhost = !IsGhost;

        if (IsGhost)
        {
            // Apply ghost state
            ApplyMaterialState(true); // FIX: Calling the renamed method with only the boolean
            rb.useGravity = false;
        }
        else
        {
            // Restore normal state
            ApplyMaterialState(false); // FIX: Calling the renamed method with only the boolean
            rb.useGravity = true;
        }
    }

    // Method which modifies the target's material (alpha/color) or restores it.
    void ApplyMaterialState(bool applyModification) 
    {
        // Use the cached material instance
        Material material = originalMaterialInstance;
        if (material == null) return;

        Color targetColor;
        if (applyModification)
        {
            targetColor = new Color(1f, 0f, 0f, 0.5f);

            // Enable Transparency (this is for URP)
            material.SetFloat("_Surface", 1);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            targetColor = originalColor;

            // Disable Transparency (basically enable Opaque mode back)
            material.SetFloat("_Surface", 0);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", targetColor);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", targetColor);
        }
    }
}