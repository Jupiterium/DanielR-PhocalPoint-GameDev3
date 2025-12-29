using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit: MonoBehaviour
{
    public string nextSceneName = "Level2";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Load the next scene
            Debug.Log("Exiting Level...");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}