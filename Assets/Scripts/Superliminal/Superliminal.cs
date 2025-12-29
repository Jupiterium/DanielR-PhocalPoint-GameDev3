using System;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages the core interaction system for grabbing, resizing, and rotating objects.
/// Players can pick up objects and adjust their size based on distance, rotate them with R key,
/// and toggle their ghost state with T key (similar to the game "Superliminal").
/// </summary>
public class Superliminal : MonoBehaviour
{
    [Header("Cursor")]
    public RawImage cursorImage; // UI image used as the custom cursor
    public float baseScale = 0.09f; // Normal scale of the cursor
    public float shrinkFactor = 0.5f; // How much to shrink cursor when grabbing (0.5 = 50% smaller)
    public float animationSpeed = 15f; // Speed of cursor scale animation
    private Vector3 targetCursorScale; // Cursor scale target for smooth animation

    [Header("Audio Sources")]
    public AudioSource sfxSource; // Plays grab/release sound effects
    public AudioSource loopSource; // Unused looping audio source (reserved for future use)

    [Header("Audio Clips")]
    public AudioClip grabSound; // Sound played when picking up an object
    public AudioClip releaseSound; // Sound played when releasing an object
    public AudioClip ghostOnSound; // Sound played when toggling ghost state ON (unused)
    public AudioClip ghostOffSound; // Sound played when toggling ghost state OFF (unused)

    [Header("Cinemachine Control")]
    public CinemachineVirtualCamera playerVcam; // Reference to the player's camera
    private Transform originalLookAtTarget; // Stores the original camera look-at target before rotation mode

    [Header("Resizing Limits")]
    public float minScaleLimit = 0.1f; // Minimum allowed object scale
    public float maxScaleLimit = 10f; // Maximum allowed object scale

    private Transform target;
    private Collider targetCollider;
    private SuperliminalObject targetSuperObject;

    [Header("Parameters")]
    public LayerMask targetMask; // Layers that can be picked up
    public LayerMask ignoreTargetMask; // Layers that objects stop at when resizing
    public LayerMask ghostCaptureMask; // Layers used for ghost capture (unused)
    public float offsetFactor; // Unused parameter (reserved for future use)

    float originialDistance;
    Vector3 initialObjectScale;

    private ObjectRotator objectRotator;
    private bool isRotationMode = false;


    private void Start()
    {
        // Hide the system cursor and lock it to the game window
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Store the camera's original look-at target before entering rotation mode
        if (playerVcam != null) originalLookAtTarget = playerVcam.m_LookAt;

        // Get the ObjectRotator component from this GameObject
        objectRotator = GetComponent<ObjectRotator>();
        if (objectRotator == null) Debug.LogError("ObjectRotator missing!");

        // Initialize the cursor to its base scale
        targetCursorScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        HandleInput();
        UpdateCursorVisuals();
    }

    private void FixedUpdate()
    {
        // Continuously resize the target based on raycast distance
        ResizeTarget();
        // Rotate the target if in rotation mode and holding an object
        if (target != null && isRotationMode) objectRotator.RotateTarget(target);
    }

    /// <summary>
    /// Updates the cursor's visual appearance based on interaction state.
    /// Shrinks when grabbing, keeps normal size when rotating or idle.
    /// </summary>
    void UpdateCursorVisuals()
    {
        if (cursorImage == null) return;

        // Determine target cursor scale based on current interaction state
        if (target != null)
        {
            // Player is holding something
            if (isRotationMode)
            {
                // Keep normal size while rotating
                targetCursorScale = Vector3.one * baseScale;
            }
            else
            {
                // Shrink cursor while grabbing
                targetCursorScale = Vector3.one * (baseScale * shrinkFactor);
            }
        }
        else
        {
            // No object held - use normal size
            targetCursorScale = Vector3.one * baseScale;
        }

        // Smoothly animate the cursor scale towards the target scale
        cursorImage.rectTransform.localScale = Vector3.Lerp(cursorImage.rectTransform.localScale, targetCursorScale, Time.deltaTime * animationSpeed);

        // Ensure cursor is always white (no color tints)
        cursorImage.color = Color.white;
    }

    /// <summary>
    /// Toggles the ghost state of an object between Normal and Ghost forms.
    /// Delegates to the SuperliminalObject component.
    /// </summary>
    /// <param name="objToToggle">Transform of the object to toggle</param>
    void ToggleTargetState(Transform objToToggle)
    {
        SuperliminalObject superObj = objToToggle.GetComponent<SuperliminalObject>();
        if (superObj != null) superObj.ToggleState();
    }

    /// <summary>
    /// Handles all player input for grabbing, releasing, rotating, and toggling ghost state.
    /// Input Controls:
    /// - Left Click: Grab/Release objects
    /// - R Key (while holding): Enter/Exit rotation mode
    /// - T Key (while holding): Toggle ghost state
    /// - Right Click + T Key (while not holding): Capture ghost objects
    /// </summary>
    void HandleInput()
    {
        // Left Click: Grab/Release Object
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (target == null)
            {
                // Attempt to grab an object
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    // Check if the hit object has a SuperliminalObject component
                    SuperliminalObject superObj = hit.transform.GetComponent<SuperliminalObject>();
                    if (superObj == null) return; // Not a valid target

                    // Set the target and cache its initial state
                    target = hit.transform;
                    targetSuperObject = superObj;

                    // Get physics components
                    Rigidbody rb = target.GetComponent<Rigidbody>();
                    targetCollider = target.GetComponent<Collider>();

                    // Make the object kinematic while held (no physics simulation)
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.constraints = RigidbodyConstraints.FreezeRotation;
                    }
                    // Disable collider while held to prevent collision issues
                    if (targetCollider != null) targetCollider.enabled = false;

                    // Store the initial distance and scale for resize calculations
                    originialDistance = Vector3.Distance(transform.position, target.position);
                    initialObjectScale = target.localScale;

                    // Play grab sound effect
                    sfxSource.PlayOneShot(grabSound);
                }
            }
            else
            {
                // Release the currently held object
                // Exit rotation mode if active
                if (isRotationMode)
                {
                    isRotationMode = false;
                    objectRotator.SetRotationActive(false);
                }

                // Re-enable the collider
                if (targetCollider != null)
                {
                    targetCollider.enabled = true;
                    targetCollider = null;
                }

                // Restore physics simulation
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.constraints = RigidbodyConstraints.None;
                }
                targetSuperObject = null;
                target = null;

                // Play release sound effect
                sfxSource.PlayOneShot(releaseSound);
            }
        }

        // Right click + T Key: Capture Ghost Object
        // Only works when not holding an object
        if (target == null && Mouse.current.rightButton.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 20f, targetMask))
            {
                // Check if the target is a ghost object
                SuperliminalObject capturedObject = hit.transform.GetComponent<SuperliminalObject>();
                if (capturedObject != null && capturedObject.IsGhost)
                {
                    // Toggle it back to normal state
                    capturedObject.ToggleState();
                }
            }
        }

        // R Key - Rotation Mode
        // Only works while holding an object
        if (target != null)
        {
            bool rKeyPressed = Keyboard.current.rKey.isPressed;
            if (rKeyPressed && !isRotationMode)
            {
                // Enter rotation mode
                isRotationMode = true;
                FirstPersonController.RotationOverridden = true; // Disable camera rotation
                objectRotator.SetRotationActive(true); // Enable object rotation

                // Remove camera's look-at target so it doesn't interfere
                if (playerVcam != null) playerVcam.m_LookAt = null;
            }
            else if (!rKeyPressed && isRotationMode)
            {
                // Exit rotation mode
                isRotationMode = false;
                FirstPersonController.RotationOverridden = false; // Re-enable camera rotation
                objectRotator.SetRotationActive(false); // Disable object rotation

                // Restore the original camera look-at target
                if (playerVcam != null) playerVcam.m_LookAt = originalLookAtTarget;
            }
        }

        // ========== T KEY: TOGGLE GHOST STATE ==========
        // Only works while holding an object and not in rotation mode
        if (target != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (!isRotationMode) ToggleTargetState(target);
        }
    }

    /// <summary>
    /// Resizes the held object based on the distance between the player's raycast and the target surface.
    /// Uses perspective-based scaling similar to the game "Superliminal".
    /// The object moves to stay visually positioned at the raycast hit point.
    /// </summary>
    void ResizeTarget()
    {
        if (target == null) return;

        RaycastHit hit;
        // Raycast forward to find the surface the object should "sit on"
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            // Calculate the scale factor based on distance ratio
            float hitDistance = hit.distance;
            float s = hitDistance / originialDistance;

            // Clamp the scale to the allowed limits
            s = Mathf.Clamp(
                s,
                minScaleLimit / initialObjectScale.x,
                maxScaleLimit / initialObjectScale.x
            );

            // Calculate the new scale and smoothly interpolate towards it
            Vector3 targetNewScale = initialObjectScale * s;
            target.localScale = Vector3.Lerp(
                target.localScale,
                targetNewScale,
                10f * Time.deltaTime
            );

            // Position the object at the raycast hit point
            // Calculate how much space the object takes up to avoid clipping into surfaces
            float largestDimension = Mathf.Max(target.localScale.x, target.localScale.y, target.localScale.z);
            float halfObjectSize = largestDimension * 0.5f;
            float visualClearance = 0.05f; // Small offset to prevent clipping

            // Place the object so it appears to be sitting on the hit surface
            target.position = hit.point - transform.forward * (halfObjectSize + visualClearance);
        }
    }
}