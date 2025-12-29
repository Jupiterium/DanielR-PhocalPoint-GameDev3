using UnityEngine;

public class Collectable : MonoBehaviour
{
    public AudioClip pickupSound;
    public GameObject pickupParticles;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    void Collect()
    {
        // Tell GameManager to add score
        GameManager.Instance.AddScore(1);
        Debug.Log("Collectable picked up!");

        // Play sound and particles
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupParticles) Instantiate(pickupParticles, transform.position, pickupParticles.transform.rotation);

        // Destroy
        Destroy(gameObject);
    }
}