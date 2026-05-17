using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int score;
    [SerializeField] private float signOnTime = 4f;
    [SerializeField] private float currentSignTime;

    [SerializeField] private Animator uiAnimator;

    private bool signOn = false;

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
        currentSignTime = signOnTime;
    }

    private void Update()
    {
        if (!signOn)
        {
            return;
        }
        else
        {
            currentSignTime -= Time.deltaTime;
            if (currentSignTime <= 0)
            {
                uiAnimator.SetTrigger("HideSign");
                currentSignTime = signOnTime;
                signOn = false;
            }
        }
    }

    public void AddScore(int scoreToAdd)
    {
        score += scoreToAdd;
        UpdateScoreText();
        if (!signOn)
        {
            uiAnimator.SetTrigger("ShowSign");
            signOn = true;
        }
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