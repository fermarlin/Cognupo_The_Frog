using UnityEngine;

// Este script permite que un enemigo recupere el control despues de recibir knockback.
// Se suscribe al evento OnDamaged del Health.
public class KnockbackRecovery : MonoBehaviour
{
    [Header("Knockback Recovery")]
    [SerializeField] private Health health;                      // Health que avisa cuando recibe daño
    [SerializeField] private Rigidbody rb;                       // Rigidbody que recibe el knockback
    [SerializeField] private float knockbackControlDelay = 0.25f; // Tiempo que dejamos actuar al golpe
    [SerializeField] private float knockbackRecoveryTime = 1f;    // Tiempo total de recuperacion
    [SerializeField] private float recoveryMoveMultiplier = 2f;   // Velocidad extra al volver
    [SerializeField] private float velocityStopSpeed = 8f;   // Velocidad con la que frenamos

    private float knockbackTimer;
    private float knockbackDelayTimer;

    public bool IsRecovering => knockbackTimer > 0f;

    public float RecoveryMoveMultiplier => recoveryMoveMultiplier;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (health != null)
        {
            health.OnDamaged += StartRecovery;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= StartRecovery;
        }
    }

    private void StartRecovery(float damageReceived)
    {
        knockbackTimer = knockbackRecoveryTime;
        knockbackDelayTimer = knockbackControlDelay;
    }

    public bool UpdateRecovery()
    {
        knockbackTimer -= Time.deltaTime;

        // Durante este tiempo no recuperamos control.
        if (knockbackDelayTimer > 0f)
        {
            knockbackDelayTimer -= Time.deltaTime;
            return false;
        }

        // Frenamos poco a poco la velocidad del Rigidbody.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                velocityStopSpeed * Time.deltaTime
            );
        }

        // Si termina la recuperacion, quito la velocidad residual.
        if (knockbackTimer <= 0f)
        {
            knockbackTimer = 0f;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        return true;
    }
}