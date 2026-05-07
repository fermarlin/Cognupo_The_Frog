using UnityEngine;

// Este script es el cerebro concreto del Cormafen.

public class CH_Cormafen_Behavior : MonoBehaviour
{
    [Header("Look")]

    [SerializeField] private float rotationSpeed = 10f;              // Velocidad a la que gira mirando al player

    [Header("Melee Attack")]

    [SerializeField] private int meleeDamage = 1;                    // Daño del ataque cuerpo a cuerpo
    [SerializeField] private float meleeAttackRange = 2f;            // Distancia a la que puede atacar desde el sitio
    [SerializeField] private float meleeAttackCooldown = 1f;         // Tiempo entre ataques
    [SerializeField] private Transform attackPoint;                  // Punto desde donde hace el golpe
    [SerializeField] private float attackRadius = 1f;                // Radio del golpe
    [SerializeField] private string playerTag = "Player";            // Tag del player

    [Header("Modules")]

    [SerializeField] private FlyingHeightController heightController;
    [SerializeField] private PatrolBetweenPoints patrol;
    [SerializeField] private KnockbackRecovery knockbackRecovery;
    [SerializeField] private EnemyAnimator enemyAnimator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Health health;
    [SerializeField] private PlayerDetector playerDetector;

    private Transform player;

    private float meleeAttackTimer;

    private bool isDead = false;

    private void Awake()
    {
        // Si no estan puestos por Inspector, los buscamos en este GameObject.
        if (patrol == null)
        {
            patrol = GetComponent<PatrolBetweenPoints>();
        }

        if (playerDetector == null)
        {
            playerDetector = GetComponent<PlayerDetector>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (health != null)
        {
            health.OnDeath += OnDeath;
        }

        if (heightController == null)
        {
            heightController = GetComponent<FlyingHeightController>();
        }

        if (knockbackRecovery == null)
        {
            knockbackRecovery = GetComponent<KnockbackRecovery>();
        }

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<EnemyAnimator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (attackPoint == null)
        {
            attackPoint = transform;
        }
    }

    private void OnDestroy()
    {
        // Nos desuscribimos para evitar errores si se destruye el objeto.
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }

    private void Update()
    {
        // Si esta muerto, no patrulla, no detecta, no mira y no ataca.
        if (isDead)
            return;

        // Si esta recuperandose de knockback, no patrulla ni ataca.
        if (knockbackRecovery != null && knockbackRecovery.IsRecovering)
        {
            HandleKnockbackState();
            return;
        }

        // Bajamos cooldowns de ataques.
        UpdateTimers();

        // Mantenemos altura si usa este sistema.
        if (heightController != null)
        {
            heightController.KeepHeightFromGround();
        }

        // La deteccion la hace PlayerDetector.
        if (playerDetector != null)
        {
            player = playerDetector.UpdateDetection(player);
        }
        else
        {
            player = null;
        }

        // Si PlayerDetector ha encontrado player, Cormafen combate desde el sitio.
        if (player != null)
        {
            HandleCormafenCombat();
            return;
        }

        // Si PlayerDetector no encuentra player, patrulla.
        HandlePatrol();
    }

    private void UpdateTimers()
    {
        // Bajamos el cooldown del ataque melee.
        if (meleeAttackTimer > 0f)
        {
            meleeAttackTimer -= Time.deltaTime;
        }
    }

    private void HandleCormafenCombat()
    {
        if (player == null)
            return;


        if (patrol != null)
        {
            patrol.isPatrolling = false;
        }

        // Se queda quieto siempre que ve al player.
        StopMovement();

        // Siempre mira hacia el player.
        LookAtPlayer();

        // Calculamos distancia horizontal.

        float distanceToPlayer = GetHorizontalDistanceToPlayer();

        // Si esta a distancia cuerpo a cuerpo, ataca desde el sitio.
        if (distanceToPlayer <= meleeAttackRange)
        {
            TryMeleeAttack();
            return;
        }

        // Si ve al player pero no esta suficientemente cerca, se queda quieto mirando.
        if (enemyAnimator != null)
        {
            enemyAnimator.SetIdle();
        }
    }

    private void HandlePatrol()
    {
        // Paramos cualquier velocidad anterior.
        StopMovement();

        if (patrol == null)
        {
            if (enemyAnimator != null)
            {
                enemyAnimator.SetIdle();
            }

            return;
        }

        // Patrulla A-B.
        patrol.Patrol();
    }

    private void HandleKnockbackState()
    {
        bool canMoveAgain = knockbackRecovery.UpdateRecovery();

        if (!canMoveAgain)
            return;

        // Si sigue teniendo player, vuelve a mirarle al terminar el knockback.
        if (player != null)
        {
            LookAtPlayer();
        }
    }

    private void TryMeleeAttack()
    {
        // Si todavia esta en cooldown, no ataca.
        if (meleeAttackTimer > 0f)
            return;

        // Reiniciamos cooldown.
        meleeAttackTimer = meleeAttackCooldown;

        // Llamamos a la animacion de ataque igual que en Beetle.
        if (enemyAnimator != null)
        {
            enemyAnimator.SetAttacking();
        }


        DealMeleeDamage();
    }

    public void DealMeleeDamage()
    {
        if (attackPoint == null)
            return;

        Collider[] colliders = Physics.OverlapSphere(attackPoint.position, attackRadius);

        foreach (Collider col in colliders)
        {
            if (!col.CompareTag(playerTag))
                continue;

            // Cormafen ataca desde el sitio.
            DamagePlayer(meleeDamage);
            return;
        }
    }

    private void DamagePlayer(int damage)
    {
        if (player == null)
            return;

        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth == null)
        {
            playerHealth = player.GetComponentInParent<Health>();
        }

        if (playerHealth == null)
            return;

        playerHealth.ChangeHealth(-damage, transform);
    }

    private float GetHorizontalDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        Vector3 enemyPosition = transform.position;
        Vector3 playerPosition = player.position;


        enemyPosition.y = 0f;
        playerPosition.y = 0f;

        return Vector3.Distance(enemyPosition, playerPosition);
    }

    private void LookAtPlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;


        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void StopMovement()
    {
        if (rb == null)
            return;

        // Paramos solo el movimiento horizontal.
        rb.linearVelocity = new Vector3(
            0f,
            rb.linearVelocity.y,
            0f
        );
    }

    private void OnDeath()
    {
        // Marcamos al Cormafen como muerto.
        isDead = true;

        // Olvidamos al player para que no siga atacando.
        player = null;

        // Paramos movimiento.
        StopMovement();

        // Si muere, tampoco debe seguir contando como patrullando.
        if (patrol != null)
        {
            patrol.isPatrolling = false;
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.SetIdle();
        }
    }

}