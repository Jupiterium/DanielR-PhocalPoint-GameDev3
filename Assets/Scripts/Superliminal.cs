using UnityEngine;
using UnityEngine.InputSystem;

/*
 * The purpose of this script is to:
 * - Resize the selected object (target)
 * - Toggle the object's zero-gravity/material state (opacity/color) using 'T'.
 * - Shutter capture an object while it's in the ghost (transparent) state.
*/

public class Superliminal : MonoBehaviour
{
    [Header("Resizing Limits")]
    public float minScaleLimit = 0.1f;
    public float maxScaleLimit = 10f;

    private Transform target; // The object to be resized
    private Collider targetCollider; // We need target's collider to disable/enable it for the Resizing mechanic.

    // NEW: Reference to the component on the currently held object
    private SuperliminalObject targetSuperObject;

    [Header("Parameters")]
    public LayerMask targetMask; // For picking up/selecting targets
    public LayerMask ignoreTargetMask; // For resizing raycast (walls/floor)
    public LayerMask ghostCaptureMask; // Layer mask for the Shutter Capture raycast
    public float offsetFactor;

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
    // Calls the ToggleState method on the component.
    void ToggleTargetState(Transform objToToggle)
    {
        SuperliminalObject superObj = objToToggle.GetComponent<SuperliminalObject>();

        if (superObj == null)
        {
            Debug.LogWarning("Target is missing the SuperliminalObject component. Cannot toggle state.");
            return;
        }

        // Apply state change to the component
        superObj.ToggleState();

        // The single-ghost tracking variables below are now redundant but kept as placeholders 
        // to maintain the original script structure, though their function is now handled by the component.
        // if (!isMaterialModified) { ... isMaterialModified = true; ... } else { ... isMaterialModified = false; ... }
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
                    // Check for the required component
                    SuperliminalObject superObj = hit.transform.GetComponent<SuperliminalObject>();
                    if (superObj == null)
                    {
                        Debug.Log("Hit object is not an interactable SuperliminalObject.");
                        return;
                    }

                    target = hit.transform;
                    targetSuperObject = superObj; // Cache the component

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
                    // Physics are restored, but gravity/material state is maintained by the SuperliminalObject component.
                    rb.isKinematic = false;
                    rb.constraints = RigidbodyConstraints.None;
                }

                // Clear references
                targetSuperObject = null; // Clear the component reference
                target = null;
                Debug.Log("Target deselected");
            }
        }

        // T key (state toggle while selected)
        // Check for component reference and call its method.
        if (target != null && targetSuperObject != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            targetSuperObject.ToggleState();
        }

        // Shutter Capture (RMB + T)
        // This runs only when the player is NOT holding an object (target == null)
        if (target == null && Mouse.current.rightButton.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Attempting Shutter Capture...");

            // 1. Raycast to see what the player is looking at
            RaycastHit hit;
            // Use targetMask to hit objects that can be reverted
            if (Physics.Raycast(transform.position, transform.forward, out hit, 20f, targetMask))
            {
                SuperliminalObject capturedObject = hit.transform.GetComponent<SuperliminalObject>();

                // 2. Check if the hit object is a GHOST using the component's flag
                if (capturedObject != null && capturedObject.IsGhost)
                {
                    // 3. Revert the Ghost state by calling its ToggleState method
                    capturedObject.ToggleState();
                    Debug.Log("Shutter Capture successful! Object reverted to normal state.");
                }
                else
                {
                    // This now handles any object that is either non-interactable or not currently a ghost.
                    Debug.Log("Capture failed: Object is not the active ghost.");
                }
            }
            else
            {
                Debug.Log("Capture failed: No object hit.");
            }
        }
    }

    // Method which resizes the selected object (Superliminal mechanic basically)
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
}