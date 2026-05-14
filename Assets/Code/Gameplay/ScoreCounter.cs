using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] sfx;

    [SerializeField] private int score;

    private void Update()
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

    private void AddScore(int scoreToAdd)
    {
        score += scoreToAdd;
        audioSource.PlayOneShot(sfx[0]);
    }
}
