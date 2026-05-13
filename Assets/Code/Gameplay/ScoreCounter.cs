using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private int score;

    private void Update()
    {
        scoreText.text = score.ToString();
    }

    private void AddScore(int scoreToAdd)
    {
        score += scoreToAdd;
    }
}
