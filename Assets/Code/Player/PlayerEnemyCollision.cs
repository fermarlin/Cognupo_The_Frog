using UnityEngine;

// Este script controla el trigger entre el Player y los enemigos
// Si el Player esta por encima del enemigo y cae, le hace daño
// Si no, el Player recibe daño y sale empujado hacia atras
public class PlayerEnemyCollision : MonoBehaviour
{
    [Header("Enemy Detection")]
    [SerializeField] private string enemyTag = "Enemy"; // Tag que deben tener los enemigos

    [Header("Stomp Attack")]
    [SerializeField] private float stompDamage = -1f;     // Daño que recibe el enemigo
    [SerializeField] private float bounceForce = 20f;     // Fuerza del rebote al pisar enemigo
    [SerializeField] private float minFallSpeed = -0.1f;  // Velocidad minima cayendo
    [SerializeField] private float heightOffset = 0.3f;   // Margen para saber si estoy encima

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;   // Fuerza horizontal del empujon
    [SerializeField] private float knockbackUpForce = 3f; // Fuerza hacia arriba del empujon

    private Rigidbody rb;            // Rigidbody del Player
    private Health playerHealth;     // Vida del Player
    private float lastDamageTime;    // Ultima vez que el Player recibio daño

    private void Awake()
    {
        // Pillamos los componentes del Player
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<Health>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si no he entrado en el trigger de un enemigo, no hago nada
        if (!other.CompareTag(enemyTag))
            return;

        // Pillamos la vida del enemigo
        // Uso GetComponentInParent por si el collider esta en un hijo del enemigo
        Health enemyHealth = other.GetComponentInParent<Health>();

        if (enemyHealth == null)
            return;

        // Si estoy encima del enemigo, le hago daño
        if (IsPlayerAboveEnemy(other.transform))
        {
            enemyHealth.ChangeHealth(stompDamage);
            Bounce();
            return;
        }

        // Si no estoy encima, me hace daño a mi
        // Le paso la posicion del enemigo para saber hacia donde empujar al player
        ApplyKnockback(other.transform);
    }

    private bool IsPlayerAboveEnemy(Transform enemy)
    {
        if (rb == null)
            return false;

        // Si el Player no esta cayendo, no cuenta como pisoton
        if (rb.linearVelocity.y > minFallSpeed)
            return false;

        // Posicion Y del mundo del Player
        float playerY = transform.position.y;

        // Posicion Y del mundo del Enemy
        float enemyY = enemy.position.y;

        // Si la Y del Player esta por encima de la Y del Enemy,
        // entendemos que el Player ha caido encima
        bool playerIsAboveEnemy = playerY > enemyY + heightOffset;

        return playerIsAboveEnemy;
    }

    private void Bounce()
    {
        if (rb == null)
            return;

        // Quitamos la velocidad vertical para que el rebote sea limpio
        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        // Aplicamos impulso hacia arriba
        rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
    }



    private void ApplyKnockback(Transform enemy)
    {
        if (rb == null)
            return;

        // Direccion desde el enemigo hacia el player
        Vector3 knockbackDirection = transform.position - enemy.position;

        // No queremos que la direccion vertical afecte al empujon horizontal
        knockbackDirection.y = 0f;

        // Si por lo que sea estan justo en el mismo punto,
        // usamos la direccion contraria a donde mira el player
        if (knockbackDirection.sqrMagnitude < 0.001f)
        {
            knockbackDirection = -transform.forward;
        }

        knockbackDirection.Normalize();

        // Limpiamos un poco la velocidad horizontal actual
        // Asi el movimiento normal no se come el knockback
        rb.linearVelocity = Vector3.zero;

        // Empujon final: hacia atras + un poco hacia arriba
        Vector3 finalKnockback = knockbackDirection * knockbackForce;
        finalKnockback += Vector3.up * knockbackUpForce;

        // VelocityChange va bien con tu PlayerMovement porque no depende de la masa
        rb.AddForce(finalKnockback, ForceMode.VelocityChange);
    }
}