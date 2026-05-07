using UnityEngine;

// Este script es el cerebro concreto del Beetle.
public class CH_Beetle_Behavior : MonoBehaviour
{
    
    [Header("Movement")]

    [SerializeField] private float maxSpeed = 8f;                 // Velocidad cuando carga hacia el player
    [SerializeField] private float rotationSpeed = 10f;              // Velocidad a la que gira mirando al player

    [Header("Melee Attack")]

    [SerializeField] private int meleeDamage = 1;                    // Daño del ataque cuerpo a cuerpo
    [SerializeField] private float meleeAttackRange = 1.6f;          // Distancia para atacar sin cargar
    [SerializeField] private float meleeAttackCooldown = 1f;         // Tiempo entre ataques melee
    [SerializeField] private Transform attackPoint;                  // Punto desde donde hace el ataque
    [SerializeField] private float attackRadius = 1f;                // Radio del golpe melee
    [SerializeField] string playerTag = "Player";  

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

        // Si no ponemos punto de ataque, usamos el propio transform.
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
        // Si esta recuperandose de knockback, no patrulla ni ataca.
        if (knockbackRecovery != null && knockbackRecovery.IsRecovering)
        {
            HandleKnockbackState();
            return;
        }

        // Si esta muerto, no patrulla, no detecta, no carga y no ataca.
        if (isDead)
            return;

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

        // Si PlayerDetector ha encontrado player, el Beetle combate.
        if (player != null)
        {
            HandleBeetleCombat();
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

    private void HandleBeetleCombat()
    {
        if (player == null)
            return;

        // Siempre mira hacia el player.
        LookAtPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si esta a distancia cuerpo a cuerpo, deja de cargar y ataca normal.
        if (distanceToPlayer <= meleeAttackRange)
        {
            StopMovement();
            TryMeleeAttack();
            return;
        }

        // Si no esta a distancia cuerpo a cuerpo, carga hacia el player.
        ChargeToPlayer();
    }

    private void HandlePatrol()
    {
        // Paramos cualquier velocidad de carga anterior.
        StopMovement();

        if (enemyAnimator != null)
        {
            enemyAnimator.SetIdle();
        }

        if (patrol != null)
        {
            patrol.Patrol();
        }
    }

    private void HandleKnockbackState()
    {
        bool canMoveAgain = knockbackRecovery.UpdateRecovery();

        if (!canMoveAgain)
            return;

        if (player != null)
        {
            LookAtPlayer();
        }
    }

    private void ChargeToPlayer()
    {
        if (player == null)
            return;

        // Calculamos direccion hacia el player.
        Vector3 direction = player.position - transform.position;

        // Quitamos Y para que no cargue hacia arriba o hacia abajo.
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        // Movemos al Beetle con Rigidbody.
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(
                direction.x * maxSpeed,
                rb.linearVelocity.y,
                direction.z * maxSpeed
            );
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.SetFollowing();
        }
    }



    private void TryMeleeAttack()
    {
        // Si todavia esta en cooldown, no ataca.
        if (meleeAttackTimer > 0f)
            return;

        // Reiniciamos cooldown.
        meleeAttackTimer = meleeAttackCooldown;

        if (enemyAnimator != null)
        {
            enemyAnimator.SetAttacking();
        }

        // Si quieres que el daño ocurra justo en la animacion,
        // quita esta linea y llama a DealMeleeDamage desde Animation Event.
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

            // El ataque cuerpo a cuerpo hace 1 de daño.
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

        rb.linearVelocity = new Vector3(
            0f,
            rb.linearVelocity.y,
            0f
        );
    }

    private void OnDeath()
    {
        // Marcamos al Beetle como muerto.
        isDead = true;

        // Olvidamos al player para que no siga atacando.
        player = null;

        // Paramos movimiento.
        StopMovement();


    }


}