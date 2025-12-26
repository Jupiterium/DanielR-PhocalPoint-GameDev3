using System;
using StarterAssets;
using Unity.Burst.Intrinsics;
using Unity.Cinemachine;
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
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource loopSource;

    [Header("Audio Clips")]
    public AudioClip grabSound;
    public AudioClip releaseSound;
    public AudioClip ghostOnSound;
    public AudioClip ghostOffSound;

    // Reference to the Cinemachine Virtual Camera
    [Header("Cinemachine Control")]
    public CinemachineVirtualCamera playerVcam;
    private Transform originalLookAtTarget;

    //[Header("Camera Control")]
    //public MonoBehaviour playerCam;

    [Header("Resizing Limits")]
    public float minScaleLimit = 0.1f;
    public float maxScaleLimit = 10f;

    private Transform target; // The object to be resized
    private Collider targetCollider; // We need target's collider to disable/enable it for the Resizing mechanic.

    // Reference to the component on the currently held object
    private SuperliminalObject targetSuperObject;

    [Header("Parameters")]
    public LayerMask targetMask; // For picking up/selecting targets
    public LayerMask ignoreTargetMask; // For resizing raycast (walls/floor)
    public LayerMask ghostCaptureMask; // Layer mask for the Shutter Capture raycast
    public float offsetFactor;

    float originialDistance;
    //float originalScale; /* Deprecated: Replaced by initialObjectScale */
    //Vector3 targetScale; /* Deprecated: Replaced by initialObjectScale */

    // Vector3 to store the initial scale of the object
    Vector3 initialObjectScale;

    private ObjectRotator objectRotator;
    private bool isRotationMode = false;

    private void Start()
    {

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;


        // Cache the original target (PlayerCameraRoot)
        if (playerVcam != null)
        {
            originalLookAtTarget = playerVcam.m_LookAt;
        }

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
                    //originalScale = target.localScale.x;
                    //targetScale = target.localScale;

                    initialObjectScale = target.localScale;

                    sfxSource.PlayOneShot(grabSound);
                    Debug.Log("Target selected");
                }
            }
            else
            {
                // When deselecting, stop rotation mode if it was active
                if (isRotationMode)
                {
                    isRotationMode = false;
                    objectRotator.SetRotationActive(false);
                    Debug.Log("Rotation Mode is OFF (Deselection)");
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

                sfxSource.PlayOneShot(releaseSound);
                Debug.Log("Target deselected");
            }
        }

        // Shutter Capture (Right Mouse Button + T)
        // This runs only when the player is NOT holding an object (target == null)
        if (target == null && Mouse.current.rightButton.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Attempting Shutter Capture...");

            // Raycast to see what the player is looking at
            RaycastHit hit;

            // Use targetMask to hit objects that can be reverted
            if (Physics.Raycast(transform.position, transform.forward, out hit, 20f, targetMask))
            {
                SuperliminalObject capturedObject = hit.transform.GetComponent<SuperliminalObject>();

                // Check if the hit object is a GHOST using the component's flag
                if (capturedObject != null && capturedObject.IsGhost)
                {
                    // Revert the Ghost state by calling its ToggleState method
                    capturedObject.ToggleState();
                    Debug.Log("Shutter Capture successful! Object reverted to normal state.");
                }
                else
                {
                    // This now handles any object that is either non-interactable or not currently a ghost.
                    Debug.Log("Capture failed: Object is not the active ghost.");
                }
            }
            else { Debug.Log("Capture failed: No object hit."); }
        }

        // R key (Rotation Mode Toggle)
        if (target != null)
        {
            bool rKeyPressed = Keyboard.current.rKey.isPressed;

            // Check if we need to START rotation mode
            if (rKeyPressed && !isRotationMode)
            {
                isRotationMode = true;
                FirstPersonController.RotationOverridden = true;
                objectRotator.SetRotationActive(true);

                if (playerVcam != null) playerVcam.m_LookAt = null;

                Debug.Log("Rotation Mode: ON (E Held)");
            }
            // Check if we need to STOP rotation mode
            else if (!rKeyPressed && isRotationMode)
            {
                isRotationMode = false;
                FirstPersonController.RotationOverridden = false;
                objectRotator.SetRotationActive(false);

                if (playerVcam != null) playerVcam.m_LookAt = originalLookAtTarget;

                Debug.Log("Rotation Mode: OFF (E Released)");
            }
        }

        // T key (state toggle while selected) - Existing
        if (target != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            // Prevent toggling ghost state while actively rotating
            if (isRotationMode)
            {
                Debug.Log("Cannot toggle Ghost state while in Rotation Mode.");
                return;
            }
            ToggleTargetState(target);
        }
    }

    // This method handles resizing the target object based on distance to surfaces
    void ResizeTarget()
    {
        if (target == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            // Calculate scale factor based on distance
            float hitDistance = hit.distance;
            float s = hitDistance / originialDistance;

            // Clamp scale factor to prevent extreme resizing
            s = Mathf.Clamp(s, minScaleLimit / initialObjectScale.x, maxScaleLimit / initialObjectScale.x);

            // Calculate new scale
            Vector3 targetNewScale = initialObjectScale * s;

            // Lerping for smooth scaling
            target.localScale = Vector3.Lerp(target.localScale, targetNewScale, 10f * Time.deltaTime);

            // Position target
            // Use the largest dimension for offset to prevent clipping
            float largestDimension = Mathf.Max(target.localScale.x, target.localScale.y, target.localScale.z);
            float halfObjectSize = largestDimension * 0.5f;
            float visualClearance = 0.05f;

            // Adjust position to account for new size
            target.position = hit.point - transform.forward * (halfObjectSize + visualClearance);
        }
    }
}