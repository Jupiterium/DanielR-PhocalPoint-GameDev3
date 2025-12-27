using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int currentScore = 0;
    public int itemsRequired = 3; // Kept for the Door logic, but hidden from UI

    [Header("References")]
    public UIManager uiManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Initialize UI at start (x0)
        if (uiManager != null) uiManager.UpdateScoreText(currentScore);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;

        // Update the UI with just the count
        if (uiManager != null)
        {
            uiManager.UpdateScoreText(currentScore);
        }
    }
}