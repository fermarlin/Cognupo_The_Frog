using UnityEngine;

public class PlayerDeathUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Animator uiAnimator;

    [Header("Animator")]
    [SerializeField] private string deathBoolName = "IsDead";

    private void Awake()
    {
        if (uiAnimator == null)
            uiAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath += OnPlayerDeath;
            playerHealth.OnRespawn += OnPlayerRespawn;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= OnPlayerDeath;
            playerHealth.OnRespawn -= OnPlayerRespawn;
        }
    }

    private void OnPlayerDeath()
    {
        if (uiAnimator == null) return;

        uiAnimator.SetBool(deathBoolName, true);
    }

    private void OnPlayerRespawn()
    {
        if (uiAnimator == null) return;

        uiAnimator.SetBool(deathBoolName, false);
    }
}