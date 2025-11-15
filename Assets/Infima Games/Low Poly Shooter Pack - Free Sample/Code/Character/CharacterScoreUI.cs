using System;
using UnityEngine;
using TMPro;

public class CharacterScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

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
            scoreText.text = $"$ {ScoreManager.Instance.GetScore()}";
    }
}
