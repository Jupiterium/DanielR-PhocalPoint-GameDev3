//using UnityEngine;
//using UnityEngine.InputSystem; // New input system

///*
// * The purpose of this script is to:
// * 1. Resize the selected object (target)
// * 2. Change the objects opacity
//*/
//public class Superliminal : MonoBehaviour
//{
//    [Header("Components")]
//    public Transform target; // The object to be resized
//    public Renderer targetRenderer; // The object's renderer so we can set it transparent and change color
//    public Material targetRuntimeMat; // Cache the runtime material instance

//    [Header("Parameters")]
//    public LayerMask targetMask;
//    public LayerMask ignoreTargetMask;
//    public float offsetFactor;

//    float originialDistance;
//    float originalScale;
//    Vector3 targetScale;

//    // Material caching
//    private bool isTransparent = false;
//    private bool isNotTransparent = false;
//    private Color originalColor;

//    private void Start()
//    {
//        Cursor.visible = false;
//        Cursor.lockState = CursorLockMode.Locked;

//        // Get the target's renderer component
//        if (targetRenderer == null)
//        {
//            targetRenderer = GetComponent<Renderer>();
//        }

//        // Get the target's material component
//        if (targetRuntimeMat == null)
//        {
//            targetRuntimeMat = targetRenderer.material;
//        }
//    }

//    private void Update()
//    {
//        HandleInput();
//        ResizeTarget();
//    }

//    void HandleInput()
//    {
//        if (Mouse.current.leftButton.wasPressedThisFrame) // Select/deselect target
//        {
//            if (target == null)
//            {
//                RaycastHit hit;
//                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
//                {
//                    target = hit.transform;
//                    target.GetComponent<Rigidbody>().isKinematic = true;
//                    originialDistance = Vector3.Distance(transform.position, target.position);
//                    originalScale = target.localScale.x;
//                    targetScale = target.localScale;
//                    Debug.Log("Target selected");
//                }
//            }
//            else
//            {
//                target.GetComponent<Rigidbody>().isKinematic = false;
//                target = null;
//                Debug.Log("Target deselected");
//            }
//        }

//        // Check for 'T' key press independently
//        if (Keyboard.current.tKey.wasPressedThisFrame)
//        {
//            Debug.Log("'T' was pressed!");

//            if (targetRuntimeMat != null)
//            {
//                ModifyTargetMaterial(targetRuntimeMat);
//                Debug.Log("Target's alpha changed!");
//            }
//            else
//            {
//                Debug.LogWarning("targetRuntimeMat is null");
//            }
//        }
//    }

//    // Method which resizes the selected object
//    void ResizeTarget()
//    {
//        if(target == null)
//        {
//            return;
//        }

//        RaycastHit hit;

//        if(Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
//        {
//            target.position = hit.point - transform.forward * offsetFactor * targetScale.x;

//            float currentDistance = Vector3.Distance(transform.position, target.position);
//            float s = currentDistance / originialDistance;
//            targetScale.x = targetScale.y = targetScale.z = s;

//            target.transform.localScale = targetScale * originalScale;
//        }
//    }

//    void ModifyTargetMaterial(Material material)
//    {
//        if (material == null) { return; }

//        if (material.HasProperty("_BaseColor"))
//        {
//            material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.5f));
//        }
//        else if (material.HasProperty("_Color"))
//        {
//            material.SetColor("_Color", new Color(1f, 0f, 0f, 0.5f));
//        }
//    }
//}




using UnityEngine;
using UnityEngine.InputSystem; // New input system

/*
 * The purpose of this script is to:
 * 1. Resize the selected object (target)
 * 2. Change the objects opacity (when selected by 'T')
*/
public class Superliminal : MonoBehaviour
{
    //[Header("Components")]
    private Transform target; // The object to be resized

    [Header("Parameters")]
    public LayerMask targetMask;
    public LayerMask ignoreTargetMask;
    public float offsetFactor;

    // Material state
    private Material originalTargetMaterial;
    private Color originalColor;
    private bool isMaterialModified = false;

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

    private void FixedUpdate()
    {
        ResizeTarget();
    }

    void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) // Select/deselect target
        {
            if (target == null)
            {
                // Selection
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    target = hit.transform;
                    Rigidbody rb = target.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.constraints = RigidbodyConstraints.FreezeRotation; // Freeze rotation while selected
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
                // Restore material only if it was modified by 'T'
                if (isMaterialModified && originalTargetMaterial != null)
                {
                    ModifyTargetMaterial(originalTargetMaterial, false);
                    originalTargetMaterial = null;
                    isMaterialModified = false;
                    Debug.Log("Target's alpha restored on deselection!");
                }

                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.constraints = RigidbodyConstraints.None; // Unfreeze rotation to allow normal physics
                }
                    
                target = null;
                Debug.Log("Target deselected");
            }
        }

        // Check if target is selected and 'T' is pressed
        if (target != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("'T' was pressed with target selected!");

            Renderer targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogWarning("Selected target has no Renderer component.");
                return;
            }

            // Perform material modification/restoration toggle
            if (!isMaterialModified)
            {
                // Cache the original material before modifying
                originalTargetMaterial = targetRenderer.material; // Gets the runtime instance

                // Get original color based on available property
                if (originalTargetMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = originalTargetMaterial.GetColor("_BaseColor");
                }
                else if (originalTargetMaterial.HasProperty("_Color"))
                {
                    originalColor = originalTargetMaterial.GetColor("_Color");
                }

                ModifyTargetMaterial(originalTargetMaterial, true); // Set to transparent
                isMaterialModified = true;
                Debug.Log("Target's material modified!");
            }
            else
            {
                ModifyTargetMaterial(originalTargetMaterial, false); // Restore to original
                isMaterialModified = false;
                Debug.Log("Target's material restored!");
            }
        }
    }

    // Method which resizes the selected object
    void ResizeTarget()
    {
        if (target == null)
        {
            return;
        }

        RaycastHit hit;
        //if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        //{
        //    // Position the target slightly in front of the hit point
        //    target.position = hit.point - transform.forward * offsetFactor * targetScale.x;

        //    // Calculate new scale based on distance
        //    float currentDistance = Vector3.Distance(transform.position, target.position);
        //    float s = currentDistance / originialDistance;

        //    // Apply the ratio to the original scale of the object
        //    targetScale.x = targetScale.y = targetScale.z = s;

        //    target.transform.localScale = targetScale * originalScale;

        //    Debug.Log("Target resized!");
        //}
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            // Get the distance to the collision point
            float hitDistance = hit.distance;

            // Calculate new scale based on distance to the hit point
            float s = hitDistance / originialDistance;

            // Use a small dampening factor to reduce sudden scale changes, which can reduce jitter slightly
            float targetS = Mathf.Lerp(targetScale.x, s, 0.5f); // Value like 0.1 to smooth, or 0.5 for a quick update


            // Apply the ratio to the original scale of the object
            targetScale.x = targetScale.y = targetScale.z = targetS;

            target.transform.localScale = targetScale * originalScale;

            // Position the target slightly in front of the hit point, 
            // offset by a distance proportional to its new size, using a smaller factor.
            float offset = target.transform.localScale.x * 0.5f;

            // Add a small constant bias to ensure the object is always outside the player's collision volume.
            float playerClearence = 0.05f;

            //target.position = hit.point - transform.forward * (target.transform.localScale.x * 0.5f); 
            target.position = hit.point - transform.forward * (offset + playerClearence);
        }
    }

    // Modifies the target's material (alpha/color) or restores it
    void ModifyTargetMaterial(Material material, bool applyModification)
    {
        if (material == null) { return; }

        Color targetColor;

        if (applyModification)
        {
            // Semi-transparent red: (1f, 0f, 0f, 0.5f)
            targetColor = new Color(1f, 0f, 0f, 0.5f);
            // NOTE: For transparancy to work, the Renderer needs to be set to Transparent
        }
        else
        {
            // Restore original color
            targetColor = originalColor;
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