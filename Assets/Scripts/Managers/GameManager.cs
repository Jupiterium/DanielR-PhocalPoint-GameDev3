using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int currentScore = 0;
    public int collectablesRequired = 4; // For hidden level

    [Header("References")]
    public UIManager uiManager;

    private void Awake()
    {
        // Singleton Pattern: There can be only one
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize UI at start
        if (uiManager != null) uiManager.UpdateScoreText(currentScore);
    }

    public void AddScore(int amount)
    {
        // Update score
        currentScore += amount;

        // Update the UI
        if (uiManager != null)
        {
            uiManager.UpdateScoreText(currentScore);
        }
    }
}