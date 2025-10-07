using UnityEngine;
using UnityEngine.InputSystem; // New input system

/*
 * The purpose of this script is to:
 * 1. Resize the selected object (target)
 * 2. Change the objects opacity
*/
public class Superliminal : MonoBehaviour
{
    [Header("Components")]
    public Transform target; // The object to be resized
    public Renderer targetRenderer; // The object's renderer (material) so we can set it transparent and change color
    private Material targetRuntimeMat; // Cache the runtime material instance

    [Header("Parameters")]
    public LayerMask targetMask;
    public LayerMask ignoreTargetMask;
    public float offsetFactor;

    float originialDistance;
    float originalScale;
    Vector3 targetScale;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Get the target's renderer component
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRuntimeMat != null)
        {
            targetRuntimeMat = targetRenderer.material;
        }
    }

    private void Update()
    {
        HandleInput();
        ResizeTarget();
    }

    void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) // Select/deselect target
        {
            if (target == null)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    target = hit.transform;
                    target.GetComponent<Rigidbody>().isKinematic = true;
                    originialDistance = Vector3.Distance(transform.position, target.position);
                    originalScale = target.localScale.x;
                    targetScale = target.localScale;
                    Debug.Log("Target selected");
                }
            }
            else
            {
                target.GetComponent<Rigidbody>().isKinematic = false;
                target = null;
                Debug.Log("Target deselected");
            }
        }

        // Check for 'T' key press independently
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("'T' was pressed!");

            if (targetRuntimeMat != null)
            {
                ModifyTargetMaterial(targetRuntimeMat);
                Debug.Log("Target's alpha changed!");
            }
            else
            {
                Debug.LogWarning("targetRuntimeMat is null");
            }
        }
    }

    // Method which resizes the selected object
    void ResizeTarget()
    {
        if(target == null)
        {
            return;
        }

        RaycastHit hit;

        if(Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            target.position = hit.point - transform.forward * offsetFactor * targetScale.x;

            float currentDistance = Vector3.Distance(transform.position, target.position);
            float s = currentDistance / originialDistance;
            targetScale.x = targetScale.y = targetScale.z = s;

            target.transform.localScale = targetScale * originalScale;
        }
    }

    void ModifyTargetMaterial(Material material)
    {
        if (material == null) { return; }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.5f));
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", new Color(1f, 0f, 0f, 0.5f));
        }

    }
}