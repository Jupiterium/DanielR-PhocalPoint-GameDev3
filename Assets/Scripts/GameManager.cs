using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Transform player;
    public Transform spawnPoint;

    private void Start()
    {
        // Move player to spawn point instantly on start
        if (player != null && spawnPoint != null)
        {
            // Disable CharacterController briefly to prevent physics glitches during teleport
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.position = spawnPoint.position;
            player.rotation = spawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }
    }

    // Call where needed to restart the level
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}