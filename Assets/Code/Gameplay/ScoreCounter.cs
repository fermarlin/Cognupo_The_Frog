using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int score;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int scoreToAdd)
    {
        score += scoreToAdd;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (score > 9)
        {
            scoreText.text = score.ToString();
        }
        else
        {
            scoreText.text = "0" + score.ToString();
        }
    }
}