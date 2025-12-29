using UnityEngine;

public class FloatingCollectable : MonoBehaviour
{
    [Header("Motion Settings")]
    public Vector3 rotationAxes = new Vector3(15f, 30f, 10f); // Rotates slightly on all axes
    public float hoverAmplitude = 0.5f; // Height of the bob
    public float hoverFrequency = 1.5f; // Speed of the bob

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Rotation
        // Rotates around X, Y, and Z continuously
        transform.Rotate(rotationAxes * Time.deltaTime);

        // Hovering up & down
        // We calculate a new Y position based on time
        float newY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}
