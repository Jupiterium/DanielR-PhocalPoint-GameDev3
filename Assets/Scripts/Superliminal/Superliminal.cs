using System;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Superliminal : MonoBehaviour
{
    [Header("Cursor")]
    public RawImage cursorImage;
    public float baseScale = 0.09f; // Normal scale of the cursor
    public float shrinkFactor = 0.5f; // How much to shrink when grabbing
    public float animationSpeed = 15f;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource loopSource;

    [Header("Audio Clips")]
    public AudioClip grabSound;
    public AudioClip releaseSound;
    public AudioClip ghostOnSound;
    public AudioClip ghostOffSound;

    [Header("Cinemachine Control")]
    public CinemachineVirtualCamera playerVcam;
    private Transform originalLookAtTarget;

    [Header("Resizing Limits")]
    public float minScaleLimit = 0.1f;
    public float maxScaleLimit = 10f;

    private Transform target;
    private Collider targetCollider;
    private SuperliminalObject targetSuperObject;

    [Header("Parameters")]
    public LayerMask targetMask;
    public LayerMask ignoreTargetMask;
    public LayerMask ghostCaptureMask;
    public float offsetFactor;

    float originialDistance;
    Vector3 initialObjectScale;

    private ObjectRotator objectRotator;
    private bool isRotationMode = false;

    // Internal state
    private Vector3 targetCursorScale;
    private Color targetCursorColor;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerVcam != null) originalLookAtTarget = playerVcam.m_LookAt;

        objectRotator = GetComponent<ObjectRotator>();
        if (objectRotator == null) Debug.LogError("ObjectRotator missing!");

        // Initialize Cursor State based on your custom size
        targetCursorScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        HandleInput();
        UpdateCursorVisuals();
    }

    private void FixedUpdate()
    {
        ResizeTarget();
        if (target != null && isRotationMode) objectRotator.RotateTarget(target);
    }

    // 
    void UpdateCursorVisuals()
    {
        if (cursorImage == null) return;

        if (target != null)
        {
            // Holding something
            if (isRotationMode)
            {
                // Rotating: Keep normal size
                targetCursorScale = Vector3.one * baseScale;
            }
            else
            {
                // Grabbing: Shrink it
                targetCursorScale = Vector3.one * (baseScale * shrinkFactor);
            }
        }
        else
        {
            // Idle: Normal size
            targetCursorScale = Vector3.one * baseScale;
        }

        // Apply smooth scaling only
        cursorImage.rectTransform.localScale = Vector3.Lerp(cursorImage.rectTransform.localScale, targetCursorScale, Time.deltaTime * animationSpeed);

        // Force color to always be white (removes any weird tints)
        cursorImage.color = Color.white;
    }

    // Toggle the ghost state of the target object
    void ToggleTargetState(Transform objToToggle)
    {
        SuperliminalObject superObj = objToToggle.GetComponent<SuperliminalObject>();
        if (superObj != null) superObj.ToggleState();
    }

    // Handle all input related to grabbing, releasing, rotating, and toggling ghost state
    void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (target == null)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    SuperliminalObject superObj = hit.transform.GetComponent<SuperliminalObject>();
                    if (superObj == null) return;

                    target = hit.transform;
                    targetSuperObject = superObj;

                    Rigidbody rb = target.GetComponent<Rigidbody>();
                    targetCollider = target.GetComponent<Collider>();

                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.constraints = RigidbodyConstraints.FreezeRotation;
                    }
                    if (targetCollider != null) targetCollider.enabled = false;

                    originialDistance = Vector3.Distance(transform.position, target.position);
                    initialObjectScale = target.localScale;

                    sfxSource.PlayOneShot(grabSound);
                }
            }
            else
            {
                if (isRotationMode)
                {
                    isRotationMode = false;
                    objectRotator.SetRotationActive(false);
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
            }
        }

        if (target == null && Mouse.current.rightButton.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 20f, targetMask))
            {
                SuperliminalObject capturedObject = hit.transform.GetComponent<SuperliminalObject>();
                if (capturedObject != null && capturedObject.IsGhost)
                {
                    capturedObject.ToggleState();
                }
            }
        }

        if (target != null)
        {
            bool rKeyPressed = Keyboard.current.rKey.isPressed;
            if (rKeyPressed && !isRotationMode)
            {
                isRotationMode = true;
                FirstPersonController.RotationOverridden = true;
                objectRotator.SetRotationActive(true);
                if (playerVcam != null) playerVcam.m_LookAt = null;
            }
            else if (!rKeyPressed && isRotationMode)
            {
                isRotationMode = false;
                FirstPersonController.RotationOverridden = false;
                objectRotator.SetRotationActive(false);
                if (playerVcam != null) playerVcam.m_LookAt = originalLookAtTarget;
            }
        }

        if (target != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (!isRotationMode) ToggleTargetState(target);
        }
    }

    void ResizeTarget()
    {
        if (target == null) return;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            float hitDistance = hit.distance;
            float s = hitDistance / originialDistance;
            s = Mathf.Clamp(s, minScaleLimit / initialObjectScale.x, maxScaleLimit / initialObjectScale.x);
            Vector3 targetNewScale = initialObjectScale * s;
            target.localScale = Vector3.Lerp(target.localScale, targetNewScale, 10f * Time.deltaTime);
            float largestDimension = Mathf.Max(target.localScale.x, target.localScale.y, target.localScale.z);
            float halfObjectSize = largestDimension * 0.5f;
            float visualClearance = 0.05f;
            target.position = hit.point - transform.forward * (halfObjectSize + visualClearance);
        }
    }
}