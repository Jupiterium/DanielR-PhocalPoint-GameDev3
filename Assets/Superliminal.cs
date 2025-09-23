using UnityEngine;
using UnityEngine.InputSystem;

public class Superliminal : MonoBehaviour
{
    [Header("Components")]
    public Transform target;

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
    }

    private void Update()
    {
        HandleInput();
        ResizeTarget();
    }

    void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) // new input system used
        {
            if(target == null)
            {
                RaycastHit hit;
                
                if(Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
                {
                    target = hit.transform;
                    target.GetComponent<Rigidbody>().isKinematic = true;
                    originialDistance = Vector3.Distance(transform.position, target.position);
                    originalScale = target.localScale.x;
                    targetScale = target.localScale;
                }
            }
            else
            {
                target.GetComponent<Rigidbody>().isKinematic = false;
                target = null;

            }
        }
    }

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
}