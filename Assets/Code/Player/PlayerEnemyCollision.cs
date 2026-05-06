using UnityEngine;

// Este script controla el trigger entre el Player y los enemigos.
// Si el Player esta por encima del enemigo y cae, le hace daño.
// Si no, el Player recibe daño y sale empujado hacia atras.
public class PlayerEnemyCollision : MonoBehaviour
{
    [Header("Enemy Detection")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Stomp Attack")]
    [SerializeField] private float stompDamage = -1f;
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private float minFallSpeed = -0.1f;
    [SerializeField] private float heightOffset = 0.3f;

    private Rigidbody rb;
    private Health playerHealth;

    private void Awake()
    {
        // Pillamos los componentes del Player.
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<Health>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si no he entrado en el trigger de un enemigo, no hago nada.
        if (!other.CompareTag(enemyTag))
            return;

        // Pillamos la vida del enemigo.
        // Uso GetComponentInParent por si el collider esta en un hijo.
        Health enemyHealth = other.GetComponentInParent<Health>();

        if (enemyHealth == null)
            return;

        // Si estoy por encima del enemigo, le hago daño y reboto.
        if (IsPlayerAboveEnemy(other.transform))
        {
            enemyHealth.ChangeHealth(stompDamage);

            MovementBooster.BounceUp(rb, bounceForce);

            return;
        }

        // Si no estoy por encima, el enemigo me hace daño.
        if (playerHealth != null)
        {
            playerHealth.ChangeHealth(-1,other.transform);
        }

    }

    private bool IsPlayerAboveEnemy(Transform enemy)
    {
        if (rb == null)
            return false;

        // Si el Player no esta cayendo, no cuenta como pisoton.
        if (rb.linearVelocity.y > minFallSpeed)
            return false;

        // Posicion Y del mundo del Player.
        float playerY = transform.position.y;
        // Posicion Y del mundo del Enemy.
        float enemyY = enemy.position.y;

        // Si la Y del Player esta por encima de la Y del Enemy,
        // entendemos que el Player ha caido encima.
        return playerY > enemyY + heightOffset;
    }
}