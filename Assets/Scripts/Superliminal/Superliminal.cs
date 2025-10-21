using System;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
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

    private ObjectRotator objectRotator;
    private bool isRotationMode = false;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        objectRotator = GetComponent<ObjectRotator>();
        if (objectRotator == null)
        {
            Debug.LogError("ObjectRotator component is missing from the player object!");
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate() // This update for physics related stuff
    {
        ResizeTarget();

        // Perform rotation if we are holding an object AND rotation mode is active
        if (target != null && isRotationMode)
        {
            objectRotator.RotateTarget(target);
        }
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
                // ------------------------------------
                // SELECTION LOGIC (KEEP THIS BLOCK)
                // ------------------------------------
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    SuperliminalObject superObj = hit.transform.GetComponent<SuperliminalObject>();
                    if (superObj == null)
                    {
                        Debug.Log("Hit object is not an interactable SuperliminalObject.");
                        return;
                    }

                    target = hit.transform;
                    targetSuperObject = superObj;

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
                // ------------------------------------
                // DESELECTION LOGIC (KEEP THIS BLOCK)
                // ------------------------------------
                // When deselecting, stop rotation mode if it was active
                if (isRotationMode)
                {
                    isRotationMode = false;
                    objectRotator.SetRotationActive(false);
                    Debug.Log("Rotation Mode: OFF (Deselection)");
                }

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

                targetSuperObject = null;
                target = null;
                Debug.Log("Target deselected");
            }
            // END of Mouse.current.leftButton.wasPressedThisFrame
        }

        // -----------------------------------------------------------
        // NEW LOCATION FOR E KEY (Rotation Mode Toggle)
        // -----------------------------------------------------------
        // Only check E if we are currently holding an object
        if (target != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Toggle the rotation state
            isRotationMode = !isRotationMode;

            // Tell the Rotator script to activate/deactivate
            objectRotator.SetRotationActive(isRotationMode);

            Debug.Log("Rotation Mode: " + (isRotationMode ? "ON" : "OFF"));
        }


        // -----------------------------------------------------------
        // NEW LOCATION FOR T KEY (State Toggle)
        // -----------------------------------------------------------
        // Only check T if we are holding an object
        if (target != null && targetSuperObject != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            // Prevent toggling ghost state while actively rotating
            if (isRotationMode)
            {
                Debug.Log("Cannot toggle Ghost state while in Rotation Mode.");
                return;
            }
            targetSuperObject.ToggleState();
        }


        // -----------------------------------------------------------
        // Shutter Capture (RMB + T) - Keep this outside of the main blocks
        // -----------------------------------------------------------
        if (target == null && Mouse.current.rightButton.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
        {
            // ... (existing Shutter Capture logic) ...
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