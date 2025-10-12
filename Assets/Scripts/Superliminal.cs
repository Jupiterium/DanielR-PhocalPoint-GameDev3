using UnityEngine;
using UnityEngine.InputSystem;

/*
 * The purpose of this script is to:
 * 1. Resize the selected object (target)
 * 2. Toggle the object's zero-gravity/material state (opacity/color) using 'T'.
 * 3. Shutter capture an object while it's in the ghost (transparent) state.
*/
public class Superliminal : MonoBehaviour
{
    [Header("Resizing Limits")]
    public float minScaleLimit = 0.1f;
    public float maxScaleLimit = 10f;

    private Transform target; // The object to be resized
    private Collider targetCollider; // We need target's collider to disable/enable it for the Resizing mechanic.

    [Header("Parameters")]
    public LayerMask targetMask; // For picking up/selecting targets
    public LayerMask ignoreTargetMask; // For resizing raycast (walls/floor)
    public LayerMask ghostCaptureMask; // Layer mask for the Shutter Capture raycast
    public float offsetFactor;

    // Material state (tracks the last object modified)
    private Transform lastModifiedTarget; // Store the last ghost object
    private Material originalTargetMaterial;
    private Color originalColor;
    private bool isMaterialModified = false; // Tracks if the last modified object is currently in ghost state

    float originialDistance;
    float originalScale;
    Vector3 targetScale;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate() // This update for physics related stuff
    {
        ResizeTarget();
    }

    // This method Encapsulates the T-key logic
    void ToggleTargetState(Transform objToToggle)
    {
        Rigidbody rb = objToToggle.GetComponent<Rigidbody>();
        Renderer targetRenderer = objToToggle.GetComponent<Renderer>();

        if (targetRenderer == null || rb == null)
        {
            Debug.LogWarning("Target is missing Renderer or Rigidbody.");
            return;
        }

        // This if statement works because the 'isMaterialModified' flag tracks
        // the state of the one and only object stored in 'lastModifiedTarget'.
        if (!isMaterialModified)
        {
            // --- APPLY Modified State (Ghost) ---

            // 1. Cache ORIGINAL state (only done on the first toggle from normal to modified)
            // Note: Since this is in one script, we must assume the first selected object's material is the one to cache.
            originalTargetMaterial = targetRenderer.material;
            if (originalTargetMaterial.HasProperty("_BaseColor"))
            {
                originalColor = originalTargetMaterial.GetColor("_BaseColor");
            }
            else if (originalTargetMaterial.HasProperty("_Color"))
            {
                originalColor = originalTargetMaterial.GetColor("_Color");
            }

            // 2. APPLY Modified State
            ModifyTargetMaterial(originalTargetMaterial, true); // Set to transparent
            rb.useGravity = false; // Zero Gravity

            isMaterialModified = true;
            lastModifiedTarget = objToToggle; // Set the current object as the ghost
            Debug.Log(objToToggle.name + " state modified (Transparent/Zero-G)!");
        }
        else if (objToToggle == lastModifiedTarget) // Only toggle back if it's the same object
        {
            // --- RESTORE Original State (Normal/Opaque) ---
            ModifyTargetMaterial(originalTargetMaterial, false); // Restore to original
            rb.useGravity = true; // Restore Gravity

            isMaterialModified = false;
            lastModifiedTarget = null; // Clear the ghost reference
            Debug.Log(objToToggle.name + " state restored (Opaque/Gravity On)!");
        }
    }

    void HandleInput()
    {
        // Selection/Deselection of an object (Left Mouse Button)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (target == null)
            {
                // Selection
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    target = hit.transform;
                    Rigidbody rb = target.GetComponent<Rigidbody>();
                    targetCollider = target.GetComponent<Collider>();

                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.constraints = RigidbodyConstraints.FreezeRotation;
                    }

                    if (targetCollider != null)
                    {
                        targetCollider.enabled = false;
                    }

                    originialDistance = Vector3.Distance(transform.position, target.position);
                    originalScale = target.localScale.x;
                    targetScale = target.localScale;

                    Debug.Log("Target selected");
                }
            }
            else
            {
                // Deselection
                if (targetCollider != null)
                {
                    targetCollider.enabled = true;
                    targetCollider = null;
                }

                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.constraints = RigidbodyConstraints.None;
                }

                target = null;
                Debug.Log("Target deselected");
            }
        }

        // --- T KEY (STATE TOGGLE WHILE SELECTED) ---
        if (target != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleTargetState(target);
        }

        // --- SHUTTER CAPTURE (RMB + T) ---
        // This runs only when the player is NOT holding an object (target == null)
        if (target == null && Mouse.current.rightButton.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Attempting Shutter Capture...");

            // 1. Raycast to see what the player is looking at
            RaycastHit hit;
            // Use ghostCaptureMask to only hit objects that can be reverted
            if (Physics.Raycast(transform.position, transform.forward, out hit, 20f, targetMask)) // Using targetMask for now
            {
                // 2. Check if the hit object is the current ghost object
                if (hit.transform == lastModifiedTarget && isMaterialModified)
                {
                    // 3. Revert the Ghost state
                    ToggleTargetState(hit.transform); // Revert the state
                    Debug.Log("Shutter Capture successful! Object reverted to normal state.");
                }
                else
                {
                    Debug.Log("Capture failed: Object is not the active ghost.");
                }
            }
            else
            {
                Debug.Log("Capture failed: No object hit.");
            }
        }
    }

    // Method which resizes the selected object (Superliminal mechanic pretty much)
    void ResizeTarget()
    {
        if (target == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            float hitDistance = hit.distance;
            float s = hitDistance / originialDistance;

            // Clamp the scale ratio
            s = Mathf.Clamp(s, minScaleLimit / originalScale, maxScaleLimit / originalScale);

            // Lerp for smoother scale transitions
            float targetS = Mathf.Lerp(targetScale.x, s, 0.5f);
            targetScale.x = targetScale.y = targetScale.z = targetS;

            target.transform.localScale = targetScale * originalScale;

            // Position the target slightly in front of the hit point
            float halfObjectSize = target.transform.localScale.x * 0.5f;
            float visualClearance = 0.05f;

            target.position = hit.point - transform.forward * (halfObjectSize + visualClearance);
        }
    }

    // Method which modifies the target's material (alpha/color) or restores it
    void ModifyTargetMaterial(Material material, bool applyModification)
    {
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








//using UnityEngine;
//using UnityEngine.InputSystem;

///*
// * The purpose of this script is to:
// * 1. Resize the selected object (target)
// * 2. Toggle the object's zero-gravity/material state (opacity/color) using 'T'.
// * This state persists after the object is dropped.
//*/
//public class Superliminal : MonoBehaviour
//{
//    [Header("Resizing Limits")]
//    public float minScaleLimit = 0.1f;
//    public float maxScaleLimit = 10f;

//    private Transform target; // The object to be resized
//    private Collider targetCollider;

//    [Header("Parameters")]
//    public LayerMask targetMask;
//    public LayerMask ignoreTargetMask;
//    public float offsetFactor;

//    // Material state (only used for caching original properties upon first 'T' press)
//    private Material originalTargetMaterial;
//    private Color originalColor;
//    private bool isMaterialModified = false; // Tracks if the T state is currently active on the target

//    float originialDistance;
//    float originalScale;
//    Vector3 targetScale;

//    private void Start()
//    {
//        Cursor.visible = false;
//        Cursor.lockState = CursorLockMode.Locked;
//    }

//    private void Update()
//    {
//        HandleInput();
//    }

//    private void FixedUpdate()
//    {
//        ResizeTarget();
//    }

//    void HandleInput()
//    {
//        if (Mouse.current.leftButton.wasPressedThisFrame) // Select/deselect target
//        {
//            if (target == null)
//            {
//                // --- SELECTION ---
//                RaycastHit hit;
//                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
//                {
//                    target = hit.transform;
//                    Rigidbody rb = target.GetComponent<Rigidbody>();
//                    targetCollider = target.GetComponent<Collider>(); // Cache the collider

//                    if (rb != null)
//                    {
//                        rb.isKinematic = true;
//                        rb.constraints = RigidbodyConstraints.FreezeRotation;
//                    }

//                    // 🎯 FIX: DISABLE COLLIDER WHILE SELECTED
//                    if (targetCollider != null)
//                    {
//                        targetCollider.enabled = false;
//                    }

//                    originialDistance = Vector3.Distance(transform.position, target.position);
//                    originalScale = target.localScale.x;
//                    targetScale = target.localScale;

//                    // On Selection: Determine current persistent state
//                    // (Simplified: just check if the cached material/color exists, which only happens after 'T' is pressed once)
//                    Renderer targetRenderer = target.GetComponent<Renderer>();
//                    if (targetRenderer != null)
//                    {
//                        originalTargetMaterial = targetRenderer.material;
//                        // Check if the material is currently in the modified state (Optional: requires a dedicated flag on target)
//                        // For now, we rely on the persistent 'isMaterialModified' flag set by 'T'
//                        // This assumes only ONE object can be modified at a time.
//                        // If multiple objects can be modified, 'isMaterialModified' must be tied to the target itself.
//                    }

//                    Debug.Log("Target selected");
//                }
//            }
//            else
//            {
//                // --- DESELECTION ---
//                // 🎯 FIX: RE-ENABLE COLLIDER ON DESELECTION
//                if (targetCollider != null)
//                {
//                    targetCollider.enabled = true;
//                    targetCollider = null; // Clear the cached collider reference
//                }

//                Rigidbody rb = target.GetComponent<Rigidbody>();
//                if (rb != null)
//                {
//                    rb.isKinematic = false;
//                    rb.constraints = RigidbodyConstraints.None;
//                }

//                target = null;
//                Debug.Log("Target deselected");
//            }
//        }

//        // Check if target is selected and 'T' is pressed
//        if (target != null && Keyboard.current.tKey.wasPressedThisFrame)
//        {
//            Debug.Log("'T' was pressed with target selected!");

//            Rigidbody rb = target.GetComponent<Rigidbody>();
//            Renderer targetRenderer = target.GetComponent<Renderer>();

//            if (targetRenderer == null || rb == null)
//            {
//                // Handle missing components gracefully
//                Debug.LogWarning("Selected target is missing Renderer or Rigidbody.");
//                return;
//            }

//            // Perform material modification/restoration toggle
//            if (!isMaterialModified)
//            {
//                // 1. Cache ORIGINAL state (only done on the first toggle from normal to modified)
//                originalTargetMaterial = targetRenderer.material;
//                if (originalTargetMaterial.HasProperty("_BaseColor"))
//                {
//                    originalColor = originalTargetMaterial.GetColor("_BaseColor");
//                }
//                else if (originalTargetMaterial.HasProperty("_Color"))
//                {
//                    originalColor = originalTargetMaterial.GetColor("_Color");
//                }

//                // 2. APPLY Modified State
//                ModifyTargetMaterial(originalTargetMaterial, true); // Set to transparent
//                rb.useGravity = false; // Zero Gravity

//                isMaterialModified = true;
//                Debug.Log("Target state modified (Transparent/Zero-G)!");
//            }
//            else
//            {
//                // 3. RESTORE Original State
//                ModifyTargetMaterial(originalTargetMaterial, false); // Restore to original
//                rb.useGravity = true; // Restore Gravity

//                isMaterialModified = false;
//                Debug.Log("Target state restored (Opaque/Gravity On)!");
//            }
//            // The object's properties (material/gravity) are now set permanently until 'T' is pressed again.
//        }
//    }

//    // Method which resizes the selected object
//    void ResizeTarget()
//    {
//        if (target == null)
//        {
//            return;
//        }

//        RaycastHit hit;
//        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
//        {
//            float hitDistance = hit.distance;
//            float s = hitDistance / originialDistance;

//            // CLAMP THE SCALE RATIO
//            // Note: The originalScale will be multiplied by this ratio later.
//            s = Mathf.Clamp(s, minScaleLimit / originalScale, maxScaleLimit / originalScale);

//            // Use Lerp for smoother scale transitions
//            float targetS = Mathf.Lerp(targetScale.x, s, 0.5f);
//            targetScale.x = targetScale.y = targetScale.z = targetS;

//            target.transform.localScale = targetScale * originalScale;

//            // Position the target slightly in front of the hit point
//            // Note: The previous logic of adding a playerClearance offset 
//            // is now mostly redundant if the collider is disabled, but it keeps the object visually separated.
//            float halfObjectSize = target.transform.localScale.x * 0.5f;
//            float visualClearance = 0.05f; // Small buffer for visual separation

//            target.position = hit.point - transform.forward * (halfObjectSize + visualClearance);
//        }
//    }

//    // Modifies the target's material (alpha/color) or restores it
//    void ModifyTargetMaterial(Material material, bool applyModification)
//    {
//        if (material == null) { return; }

//        Color targetColor;

//        if (applyModification)
//        {
//            // Set to semi-transparent red and ensure transparency render mode is active
//            targetColor = new Color(1f, 0f, 0f, 0.5f);

//            // 💡 CRITICAL: Enable transparency on the material (for URP/HDRP/Standard)
//            // This setup is for the Standard/URP Lit shader
//            material.SetFloat("_Surface", 1); // 1 is transparent surface mode in URP
//            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//            material.SetInt("_ZWrite", 0);
//            material.DisableKeyword("_ALPHATEST_ON");
//            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // URP Keyword
//            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
//        }
//        else
//        {
//            // Restore original color and ensure Opaque render mode is active
//            targetColor = originalColor;

//            // 💡 CRITICAL: Restore Opaque mode
//            material.SetFloat("_Surface", 0); // 0 is opaque surface mode in URP
//            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
//            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
//            material.SetInt("_ZWrite", 1);
//            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
//            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
//        }

//        if (material.HasProperty("_BaseColor"))
//        {
//            material.SetColor("_BaseColor", targetColor);
//        }
//        else if (material.HasProperty("_Color"))
//        {
//            material.SetColor("_Color", targetColor);
//        }
//    }
//}







