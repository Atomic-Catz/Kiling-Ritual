using UnityEngine;
using TMPro;

public class CharacterScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int playerId = 0; // assign the player ID in inspector

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (scoreText == null)
            Debug.LogError("No TextMeshProUGUI found in ScoreUI.");
    }

    private void Update()
    {
        if (ScoreManager.Instance != null)
            scoreText.text = $"$ {ScoreManager.Instance.GetScore(playerId)}";
    }
}