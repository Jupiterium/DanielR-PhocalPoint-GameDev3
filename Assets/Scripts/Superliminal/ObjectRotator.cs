using UnityEngine;
using UnityEngine.InputSystem;

/*
 * This script is used for rotating a target object in world space 
 * when the 'R' key is held down and the mouse is moved.
 * Script relies on standard Unity Input System checks.
 */
public class ObjectRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool inverted = false;

    private bool _isRotationActive = false;

    // Public method for Superliminal.cs to call to begin/end rotation
    public void SetRotationActive(bool active)
    {
        _isRotationActive = active;

        // When rotation is active, we should hide/lock the cursor to the center
        // to control the object rotation smoothly.
        //if (active)
        //{
        //    Cursor.lockState = CursorLockMode.Locked;
        //}
        //else
        //{
        //    // When rotation stops, revert cursor control to the main player logic.
        //    Cursor.lockState = CursorLockMode.Locked;
        //}
    }

    // Public method called by Superliminal.cs to perform the rotation
    public void RotateTarget(Transform target)
    {
        if (target == null || !_isRotationActive) return;

        // 1. Get Mouse Delta
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // 2. Apply Speed and Time Scaling
        mouseDelta *= rotationSpeed * Time.deltaTime;

        float invertFactor = inverted ? 1 : -1;

        // 3. Apply Rotation to the Target
        // Rotation around World Y-axis (Yaw/Horizontal Look)
        target.Rotate(Vector3.up, mouseDelta.x * invertFactor, Space.World);

        // Rotation around Target's Local X-axis (Pitch/Vertical Look)
        // Using Space.Self provides better rotation experience here, mimicking camera control.
        target.Rotate(Vector3.right, mouseDelta.y * invertFactor, Space.Self);

        Debug.Log("RotateTarget method called!");
    }
}