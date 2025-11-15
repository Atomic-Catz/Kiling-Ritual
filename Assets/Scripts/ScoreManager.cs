using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    private int score = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void AddPoints(int points)
    {
        score += points;
        Debug.Log("Score: " + score);
        // Optional: update your score UI here
    }

    public int GetScore() => score;
}